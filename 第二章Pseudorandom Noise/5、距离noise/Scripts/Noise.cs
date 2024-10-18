using Unity.Burst;                // 使用 Burst 编译器来优化性能
using Unity.Collections;          // 使用 NativeArray 等原生数据结构
using Unity.Jobs;                 // 使用 Job System 进行多线程调度
using Unity.Mathematics;           // 使用 Unity Mathematics 库进行数学运算
using static Unity.Mathematics.math; // 静态导入 math 类，方便调用数学函数
using System;
using UnityEngine;

public static partial class Noise // 定义一个静态部分类 Noise，用于生成噪声
{
    // 噪声生成的设置结构体，用户可以自定义噪声生成的配置
    [Serializable] // 使该结构体可在 Unity 编辑器中序列化
    public struct Settings
    {
        public int seed; // 噪声生成的种子值，决定随机性
        [Min(1)]
        public int frequency; // 基本频率，影响噪声图的细节层次
        [Range(1, 6)]
        public int octaves; // 噪声叠加的层数，层数越高，细节越丰富
        [Range(2, 4)]
        public int lacunarity; // 倍频数，决定每次噪声倍频增大多少
        [Range(0f, 1f)]
        public float persistence; // 持久性，决定叠加的每一层噪声对最终结果的影响比例

        // 默认设置，使用 4 的频率，1 层，2 倍频和 0.5 的持久性
        public static Settings Default => new Settings
        {
            frequency = 4,
            octaves = 1,
            lacunarity = 2,
            persistence = 0.5f
        };
    }

    // 定义一个结构体 LatticeSpan4，用于存储格点信息
    public struct LatticeSpan4
    {
        public int4 p0, p1; // 存储较低和较高的格点坐标
        public float4 g0, g1; // 存储格点到输入坐标的偏移量
        public float4 t; // 存储插值因子，用于平滑噪声
    }

    // 定义一个接口 ILattice，用于获取格点范围
    public interface ILattice
    {
        LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency); // 接口方法，计算格点范围
        int4 ValidateSingleStep(int4 points, int frequency);
    }

    // LatticeNormal 结构体实现 ILattice 接口，用于生成标准格点
    public struct LatticeNormal : ILattice
    {
        // GetLatticeSpan4 计算输入坐标的格点范围
        public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
        {
            coordinates *= frequency; // 将输入坐标放大到指定频率
            float4 points = floor(coordinates); // 向下取整，获取格点坐标
            LatticeSpan4 span; // 声明 LatticeSpan4 变量
            span.p0 = (int4)points; // 将浮点数转换为整型并赋值给 p0
            span.p1 = span.p0 + 1; // p1 是 p0 + 1，即较高的格点
            span.g0 = coordinates - span.p0; // 计算格点间的偏移量
            span.g1 = span.g0 - 1f;
            span.t = coordinates - points; // 计算位置在格点之间的相对位置
            // 使用 S曲线插值函数将 t 转换到 [0, 1] 的范围内
            span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
            return span; // 返回 LatticeSpan4 结构体
        }
        public int4 ValidateSingleStep(int4 points, int frequency) => points;
    }

    // LatticeTiling 结构体实现 ILattice 接口，用于生成平铺格点
    public struct LatticeTiling : ILattice
    {
        // GetLatticeSpan4 计算输入坐标的格点范围，支持平铺
        public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
        {
            coordinates *= frequency; // 将输入坐标放大到指定频率
            float4 points = floor(coordinates); // 向下取整，获取格点坐标
            LatticeSpan4 span; // 声明 LatticeSpan4 变量
            span.p0 = (int4)points; // 将浮点数转换为整型并赋值给 p0
            span.g0 = coordinates - span.p0; // 计算偏移量
            span.g1 = span.g0 - 1f;
            span.p0 -= (int4)ceil(points / frequency) * frequency; // 确保平铺
            span.p0 = select(span.p0, span.p0 + frequency, span.p0 < 0); // 处理负数
            span.p1 = span.p0 + 1; // 较高的格点
            span.p1 = select(span.p1, 0, span.p1 == frequency); // 处理边界情况
            span.t = coordinates - points; // 计算插值
            // 使用 S曲线插值函数将 t 转换到 [0, 1] 的范围内
            span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
            return span; // 返回 LatticeSpan4 结构体
        }
        public int4 ValidateSingleStep(int4 points, int frequency) =>
            select(select(points, 0, points == frequency), frequency - 1, points == -1);
    }

    // 定义一个接口 INoise，用于获取噪声值
    public interface INoise
    {
        float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency); // 接口方法，输入位置和哈希值，返回噪声值
    }

    // Job<N> 结构体用于并行处理噪声计算，N 需要实现 INoise 接口
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise
    {
        [ReadOnly] // 只读属性，表示此数组不会被修改
        public NativeArray<float3x4> positions; // 存储位置数据的原生数组

        [WriteOnly] // 只写属性，表示此数组将只用于写入数据
        public NativeArray<float4> noise; // 存储计算出的噪声值的原生数组

        public Settings settings; // 噪声生成设置
        public float3x4 domainTRS; // 用于转换位置的变换矩阵

        // Execute 方法，计算噪声值
        public void Execute(int i)
        {
            float4x3 position = domainTRS.TransformVectors(transpose(positions[i])); // 位置变换
            var hash = SmallXXHash4.Seed(settings.seed); // 使用种子生成哈希值
            int frequency = settings.frequency; // 初始频率
            float amplitude = 1f, amplitudeSum = 0f; // 振幅和振幅总和
            float4 sum = 0f; // 用于存储噪声值的累加结果

            // 计算多个倍频的噪声值
            for (int o = 0; o < settings.octaves; o++)
            {
                sum += amplitude * default(N).GetNoise4(position, hash + o, frequency); // 叠加噪声
                frequency *= settings.lacunarity; // 频率倍增
                amplitude *= settings.persistence; // 振幅衰减
                amplitudeSum += amplitude; // 累加振幅
            }
            noise[i] = sum / amplitudeSum; // 归一化噪声值
        }

        // 静态方法 ScheduleParallel 用于调度并行计算
        public static JobHandle ScheduleParallel(
            NativeArray<float3x4> positions, NativeArray<float4> noise,
            Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
        ) => new Job<N>
        {
            positions = positions, // 赋值位置数组
            noise = noise, // 赋值噪声数组
            settings = settings,
            domainTRS = domainTRS.Matrix, // 赋值变换矩阵
        }.ScheduleParallel(positions.Length, resolution, dependency); // 调度并行计算任务
    }

    // 定义委托类型 ScheduleDelegate，用于调度噪声计算任务
    public delegate JobHandle ScheduleDelegate(
        NativeArray<float3x4> positions, NativeArray<float4> noise,
        Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
    );
}
