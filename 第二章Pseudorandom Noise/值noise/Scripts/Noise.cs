using Unity.Burst;                // 使用 Burst 编译器来优化性能
using Unity.Collections;          // 使用 NativeArray 等原生数据结构
using Unity.Jobs;                 // 使用 Job System 进行多线程调度
using Unity.Mathematics;           // 使用 Unity Mathematics 库进行数学运算
using static Unity.Mathematics.math; // 静态导入 math 类，方便调用数学函数

public static partial class Noise // 定义一个静态部分类 Noise
{
    struct LatticeSpan4 // 定义一个结构体 LatticeSpan4
    {
        public int4 p0, p1; // 存储两个整型 4 元组，分别代表较低和较高的格点
        public float4 t; // 存储插值因子
    }

    // 函数 GetLatticeSpan4 计算输入坐标的格子范围
    static LatticeSpan4 GetLatticeSpan4(float4 coordinates)
    {
        float4 points = floor(coordinates); // 向下取整，获取格点坐标
        LatticeSpan4 span; // 声明 LatticeSpan4 变量
        span.p0 = (int4)points; // 将浮点数转换为整型并赋值给 p0
        span.p1 = span.p0 + 1; // p1 是 p0 + 1，即较高的格点
        span.t = coordinates - points; // 计算位置在格点之间的相对位置
        // 使用 S曲线插值函数将 t 转换到 [0, 1] 的范围内
        span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
        return span; // 返回 LatticeSpan4 结构体
    }

    // 定义一个接口 INoise，用于获取噪声值
    public interface INoise
    {
        float4 GetNoise4(float4x3 positions, SmallXXHash4 hash); // 接口方法，输入位置和哈希值，返回噪声值
    }

    // Job<N> 结构体用于并行处理噪声计算，N 需要实现 INoise 接口
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise
    {
        [ReadOnly] // 只读属性，表示此数组不会被修改
        public NativeArray<float3x4> positions; // 存储位置数据的原生数组

        [WriteOnly] // 只写属性，表示此数组将只用于写入数据
        public NativeArray<float4> noise; // 存储计算出的噪声值的原生数组

        public SmallXXHash4 hash; // 用于哈希计算的哈希值

        public float3x4 domainTRS; // 用于转换位置的变换矩阵

        // 执行方法，计算噪声
        public void Execute(int i)
        {
            // 在变换后调用噪声生成接口，存储结果
            noise[i] = default(N).GetNoise4(
                domainTRS.TransformVectors(transpose(positions[i])), hash
            );
        }

        // 静态方法 ScheduleParallel 用于调度并行计算
        public static JobHandle ScheduleParallel(
            NativeArray<float3x4> positions, NativeArray<float4> noise,
            int seed, SpaceTRS domainTRS, int resolution, JobHandle dependency
        ) => new Job<N>
        {
            positions = positions, // 赋值位置数组
            noise = noise, // 赋值噪声数组
            hash = SmallXXHash.Seed(seed), // 生成哈希值
            domainTRS = domainTRS.Matrix, // 赋值变换矩阵
        }.ScheduleParallel(positions.Length, resolution, dependency); // 调度并行计算任务
    }

    // 定义委托类型 ScheduleDelegate，用于调度噪声计算任务
    public delegate JobHandle ScheduleDelegate(
        NativeArray<float3x4> positions, NativeArray<float4> noise,
        int seed, SpaceTRS domainTRS, int resolution, JobHandle dependency
    );
}
