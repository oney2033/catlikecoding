using Unity.Mathematics; // 引入 Unity.Mathematics 库，用于数学计算

// Noise 命名空间中的静态类，用来处理噪声生成相关的内容
public static partial class Noise
{

    // 定义一个接口 IVoronoiFunction，用于计算 Voronoi 函数
    public interface IVoronoiFunction
    {
        // 评估 Voronoi 最小值对 (minima)，并返回一个 float4 值
        float4 Evaluate(float4x2 minima);
    }

    // F1 结构体实现了 IVoronoiFunction 接口，用于获取最近的距离
    public struct F1 : IVoronoiFunction
    {
        // Evaluate 方法返回最近的距离，即 minima 的第一列 (c0)
        public float4 Evaluate(float4x2 distances) => distances.c0;
    }

    // F2 结构体实现了 IVoronoiFunction 接口，用于获取第二近的距离
    public struct F2 : IVoronoiFunction
    {
        // Evaluate 方法返回第二近的距离，即 minima 的第二列 (c1)
        public float4 Evaluate(float4x2 distances) => distances.c1;
    }

    // F2MinusF1 结构体实现了 IVoronoiFunction 接口，用于获取第二近和最近距离的差值
    public struct F2MinusF1 : IVoronoiFunction
    {
        // Evaluate 方法返回第二近的距离减去最近的距离 (c1 - c0)
        public float4 Evaluate(float4x2 distances) => distances.c1 - distances.c0;
    }
}
