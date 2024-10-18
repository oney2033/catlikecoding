using Unity.Mathematics; // 引入 Unity.Mathematics 库用于数学运算
using static Unity.Mathematics.math; // 静态导入 math 类中的所有方法

// Noise 类中的静态部分，用于实现 Voronoi 噪声
public static partial class Noise
{

    // Worley 距离度量的实现，基于欧几里得距离
    public struct Worley : IVoronoiDistance
    {
        // 1D 计算：返回输入的绝对值，表示距离
        public float4 GetDistance(float4 x) => abs(x);

        // 2D 计算：返回欧几里得平方距离（不计算平方根以优化性能）
        public float4 GetDistance(float4 x, float4 y) => x * x + y * y;

        // 3D 计算：返回欧几里得平方距离
        public float4 GetDistance(float4 x, float4 y, float4 z) => x * x + y * y + z * z;

        // 1D 最终化方法，不改变 minima
        public float4x2 Finalize1D(float4x2 minima) => minima;

        // 2D 最终化方法，计算平方根以获得最终的欧几里得距离，并确保值不会大于 1
        public float4x2 Finalize2D(float4x2 minima)
        {
            minima.c0 = sqrt(min(minima.c0, 1f));
            minima.c1 = sqrt(min(minima.c1, 1f));
            return minima;
        }

        // 3D 最终化方法复用 2D 的逻辑
        public float4x2 Finalize3D(float4x2 minima) => Finalize2D(minima);
    }

    // Chebyshev 距离度量的实现，基于切比雪夫距离
    public struct Chebyshev : IVoronoiDistance
    {
        // 1D 计算：返回绝对值，表示距离
        public float4 GetDistance(float4 x) => abs(x);

        // 2D 计算：返回 x 和 y 轴上距离的最大值
        public float4 GetDistance(float4 x, float4 y) => max(abs(x), abs(y));

        // 3D 计算：返回 x、y 和 z 轴上距离的最大值
        public float4 GetDistance(float4 x, float4 y, float4 z) => max(max(abs(x), abs(y)), abs(z));

        // 1D 最终化方法，不改变 minima
        public float4x2 Finalize1D(float4x2 minima) => minima;

        // 2D 最终化方法，不改变 minima
        public float4x2 Finalize2D(float4x2 minima) => minima;

        // 3D 最终化方法，不改变 minima
        public float4x2 Finalize3D(float4x2 minima) => minima;
    }

    // 更新 Voronoi 最小值的方法
    static float4x2 UpdateVoronoiMinima(float4x2 minima, float4 distances)
    {
        // 判断哪些距离比当前最小值更小
        bool4 newMinimum = distances < minima.c0;

        // 更新第二最小值，如果新值比当前第二最小值小
        minima.c1 = select(
            select(minima.c1, distances, distances < minima.c1),
            minima.c0,  // 把当前最小值转移给第二最小值
            newMinimum  // 如果有新最小值出现
        );

        // 更新最小值
        minima.c0 = select(minima.c0, distances, newMinimum);
        return minima;
    }

    // 1D Voronoi 噪声生成结构体
    public struct Voronoi1D<L, D, F> : INoise
         where L : struct, ILattice
         where D : struct, IVoronoiDistance
         where F : struct, IVoronoiFunction
    {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);  // 使用默认的 lattice 实现
            var d = default(D);  // 使用默认的 Voronoi 距离实现
            LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency);  // 获取 x 轴的格点范围

            // 初始化最小值，设置为 2（表示无限大）
            float4x2 minima = 2f;
            for (int u = -1; u <= 1; u++)
            {
                // 处理 x 轴的偏移，更新哈希值
                SmallXXHash4 h = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));

                // 计算距离并更新最小值
                minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01A + u - x.g0));
            }

            // 返回最终的 Voronoi 噪声值
            return default(F).Evaluate(d.Finalize1D(minima));
        }
    }

    // 2D Voronoi 噪声生成结构体
    public struct Voronoi2D<L, D, F> : INoise
         where L : struct, ILattice
         where D : struct, IVoronoiDistance
         where F : struct, IVoronoiFunction
    {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);  // 使用 lattice 实现
            var d = default(D);  // 使用 Voronoi 距离实现
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),  // 获取 x 轴范围
                z = l.GetLatticeSpan4(positions.c2, frequency);  // 获取 z 轴范围

            float4x2 minima = 2f;  // 初始化最小值
            for (int u = -1; u <= 1; u++)
            {
                SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));  // 处理 x 偏移
                float4 xOffset = u - x.g0;  // x 偏移量
                for (int v = -1; v <= 1; v++)
                {
                    SmallXXHash4 h = hx.Eat(l.ValidateSingleStep(z.p0 + v, frequency));  // 处理 z 偏移
                    float4 zOffset = v - z.g0;  // z 偏移量

                    // 计算距离并更新最小值
                    minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01A + xOffset, h.Floats01B + zOffset));
                    minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01C + xOffset, h.Floats01D + zOffset));
                }
            }

            // 返回最终的 Voronoi 噪声值
            return default(F).Evaluate(d.Finalize2D(minima));
        }
    }

    // 3D Voronoi 噪声生成结构体
    public struct Voronoi3D<L, D, F> : INoise
         where L : struct, ILattice
         where D : struct, IVoronoiDistance
         where F : struct, IVoronoiFunction
    {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);  // 使用 lattice 实现
            var d = default(D);  // 使用 Voronoi 距离实现
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),  // 获取 x 轴范围
                y = l.GetLatticeSpan4(positions.c1, frequency),  // 获取 y 轴范围
                z = l.GetLatticeSpan4(positions.c2, frequency);  // 获取 z 轴范围

            float4x2 minima = 2f;  // 初始化最小值
            for (int u = -1; u <= 1; u++)
            {
                SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));  // 处理 x 偏移
                float4 xOffset = u - x.g0;  // x 偏移量
                for (int v = -1; v <= 1; v++)
                {
                    SmallXXHash4 hy = hx.Eat(l.ValidateSingleStep(y.p0 + v, frequency));  // 处理 y 偏移
                    float4 yOffset = v - y.g0;  // y 偏移量
                    for (int w = -1; w <= 1; w++)
                    {
                        SmallXXHash4 h = hy.Eat(l.ValidateSingleStep(z.p0 + w, frequency));  // 处理 z 偏移
                        float4 zOffset = w - z.g0;  // z 偏移量

                        // 计算距离并更新最小值
                        minima = UpdateVoronoiMinima(minima, d.GetDistance(
                            h.GetBitsAsFloats01(5, 0) + xOffset,
                            h.GetBitsAsFloats01(5, 5) + yOffset,
                            h.GetBitsAsFloats01(5, 10) + zOffset
                        ));
                        minima = UpdateVoronoiMinima(minima, d.GetDistance(
                            h.GetBitsAsFloats01(5, 15) + xOffset,
                            h.GetBitsAsFloats01(5, 20) + yOffset,
                            h.GetBitsAsFloats01(5, 25) + zOffset
                        ));
                    }
                }
            }

            // 返回最终的 Voronoi 噪声值
            return default(F).Evaluate(d.Finalize3D(minima));
        }
    }
}
