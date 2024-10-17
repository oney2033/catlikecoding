using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class Shapes // 定义一个静态类 Shapes，用于管理形状生成
{
    // 定义一个委托类型，用于调度生成任务
    public delegate JobHandle ScheduleDelegate(
        NativeArray<float3x4> positions, NativeArray<float3x4> normals, // 位置和法线数组
        int resolution, float4x4 trs, JobHandle dependency // 生成分辨率、变换矩阵和依赖项
    );

    // 定义 Torus（圆环形状）结构体，实现 IShape 接口
    public struct Torus : IShape
    {
        // 根据索引和分辨率，计算顶点和法线
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution); // 计算 2D 纹理坐标

            float r1 = 0.375f; // 圆环主半径
            float r2 = 0.125f; // 圆环次半径
            float4 s = r1 + r2 * cos(2f * PI * uv.c1); // 计算圆环形状的半径

            Point4 p; // 存储生成的顶点和法线
            p.positions.c0 = s * sin(2f * PI * uv.c0); // 计算顶点位置
            p.positions.c1 = r2 * sin(2f * PI * uv.c1);
            p.positions.c2 = s * cos(2f * PI * uv.c0);
            p.normals = p.positions; // 将法线设为顶点位置
            p.normals.c0 -= r1 * sin(2f * PI * uv.c0); // 调整法线，使其垂直于表面
            p.normals.c2 -= r1 * cos(2f * PI * uv.c0);
            return p;
        }
    }

    // 定义 Sphere（球体形状）结构体，实现 IShape 接口
    public struct Sphere : IShape
    {
        // 计算球体上的顶点和法线
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution); // 计算 2D 纹理坐标

            float r = 0.5f; // 球体半径
            float4 s = r * sin(PI * uv.c1); // 计算球体形状的半径

            Point4 p; // 存储生成的顶点和法线
            p.positions.c0 = uv.c0 - 0.5f; // 计算顶点位置
            p.positions.c1 = uv.c1 - 0.5f;
            p.positions.c2 = 0.5f - abs(p.positions.c0) - abs(p.positions.c1);
            float4 offset = max(-p.positions.c2, 0f);
            p.positions.c0 += select(-offset, offset, p.positions.c0 < 0f);
            p.positions.c1 += select(-offset, offset, p.positions.c1 < 0f);
            float4 scale = 0.5f * rsqrt(
                p.positions.c0 * p.positions.c0 +
                p.positions.c1 * p.positions.c1 +
                p.positions.c2 * p.positions.c2
            );
            p.positions.c0 *= scale; // 归一化顶点位置
            p.positions.c1 *= scale;
            p.positions.c2 *= scale;
            p.normals = p.positions; // 将法线设为归一化后的顶点位置
            return p;
        }
    }

    // 将索引转换为 UV 坐标，用于计算顶点位置
    public static float4x2 IndexTo4UV(int i, float resolution, float invResolution)
    {
        float4x2 uv;
        float4 i4 = 4f * i + float4(0f, 1f, 2f, 3f);
        uv.c1 = floor(invResolution * i4 + 0.00001f); // 计算整数部分
        uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f); // 计算小数部分
        uv.c1 = invResolution * (uv.c1 + 0.5f); // 计算 y 坐标
        return uv;
    }

    // 定义一个存储顶点和法线的结构体
    public struct Point4
    {
        public float4x3 positions, normals;
    }

    // 定义一个接口 IShape，要求实现形状的顶点和法线生成
    public interface IShape
    {
        Point4 GetPoint4(int i, float resolution, float invResolution);
    }

    // 定义 Plane（平面形状）结构体，实现 IShape 接口
    public struct Plane : IShape
    {
        // 计算平面上的顶点和法线
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution); // 计算 UV 坐标
            return new Point4
            {
                positions = float4x3(uv.c0 - 0.5f, 0f, uv.c1 - 0.5f), // 计算平面的顶点
                normals = float4x3(0f, 1f, 0f) // 法线指向 y 轴正方向
            };
        }
    }

    // 定义一个泛型 Job，用于并行计算形状的顶点和法线，S 必须实现 IShape 接口
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<S> : IJobFor where S : struct, IShape
    {
        [WriteOnly]
        NativeArray<float3x4> positions, normals; // 原生数组，用于存储顶点和法线

        public float resolution, invResolution; // 分辨率和分辨率的倒数
        public float3x4 positionTRS, normalTRS; // 位置和法线的变换矩阵

        // 执行任务，计算每个顶点的实际位置和法线
        public void Execute(int i)
        {
            Point4 p = default(S).GetPoint4(i, resolution, invResolution); // 获取顶点和法线

            positions[i] = transpose(positionTRS.TransformVectors(p.positions)); // 变换顶点位置
            float3x4 n = transpose(normalTRS.TransformVectors(p.normals, 0f)); // 变换法线
            normals[i] = float3x4(
                normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3) // 归一化法线
            );
        }

        // 调度并行任务，生成形状的顶点和法线
        public static JobHandle ScheduleParallel(
           NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution,
            float4x4 trs, JobHandle dependency
       ) => new Job<S>
       {
           positions = positions,
           normals = normals,
           resolution = resolution,
           invResolution = 1f / resolution,
           positionTRS = trs.Get3x4(),
           normalTRS = transpose(inverse(trs)).Get3x4() // 计算法线的变换矩阵
       }.ScheduleParallel(positions.Length, resolution, dependency); // 并行调度任务
    }
}
