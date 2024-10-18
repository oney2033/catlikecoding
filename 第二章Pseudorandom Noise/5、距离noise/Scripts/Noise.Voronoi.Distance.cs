using Unity.Mathematics;           // 引入 Unity.Mathematics 库，用于数学计算
using static Unity.Mathematics.math; // 引入 math 静态方法，使得可以直接使用数学函数

// 定义一个 Noise 命名空间中的静态类
public static partial class Noise
{
    // 定义一个接口，用于 Voronoi 距离计算
    public interface IVoronoiDistance
    {
        // 在 1D 空间中计算 Voronoi 距离，参数 x 是采样点到 Voronoi 点的距离
        float4 GetDistance(float4 x);

        // 在 2D 空间中计算 Voronoi 距离，参数 x 和 y 是两个维度的距离
        float4 GetDistance(float4 x, float4 y);

        // 在 3D 空间中计算 Voronoi 距离，参数 x, y 和 z 是三个维度的距离
        float4 GetDistance(float4 x, float4 y, float4 z);

        // 最终处理 1D 空间中的 Voronoi 噪声最小值。参数 minima 是包含两个最小值的 float4x2 结构
        float4x2 Finalize1D(float4x2 minima);

        // 最终处理 2D 空间中的 Voronoi 噪声最小值
        float4x2 Finalize2D(float4x2 minima);

        // 最终处理 3D 空间中的 Voronoi 噪声最小值
        float4x2 Finalize3D(float4x2 minima);
    }
}
