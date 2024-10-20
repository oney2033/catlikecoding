using Unity.Mathematics;
using static Unity.Mathematics.math;

public static partial class Noise
{
    // Simplex 1D 噪声结构，泛型 G 表示梯度类型，需要实现 IGradient 接口
    public struct Simplex1D<G> : INoise where G : struct, IGradient
    {
        // 获取 1D Simplex 噪声
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            // 缩放输入位置
            positions *= frequency;
            // 计算整数网格坐标
            int4 x0 = (int4)floor(positions.c0), x1 = x0 + 1;

            // 调用 Kernel 方法，生成噪声值并组合结果
            return default(G).EvaluateCombined(
                 Kernel(hash.Eat(x0), x0, positions) + Kernel(hash.Eat(x1), x1, positions)
             );
        }

        // Kernel 方法，用于计算噪声
        static float4 Kernel(SmallXXHash4 hash, float4 lx, float4x3 positions)
        {
            // 计算相对于网格节点的偏移
            float4 x = positions.c0 - lx;
            // 基于偏移量计算衰减函数
            float4 f = 1f - x * x;
            f = f * f * f;
            // 使用梯度 Evaluate 方法计算噪声值
            return f * default(G).Evaluate(hash, x);
        }
    }

    // Simplex 2D 噪声结构，泛型 G 表示梯度类型
    public struct Simplex2D<G> : INoise where G : struct, IGradient
    {
        // 获取 2D Simplex 噪声
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            // 缩放位置，并应用 Simplex 特有的缩放因子
            positions *= frequency * (1f / sqrt(3f));
            float4 skew = (positions.c0 + positions.c2) * ((sqrt(3f) - 1f) / 2f);
            float4 sx = positions.c0 + skew, sz = positions.c2 + skew;

            // 计算网格坐标
            int4 x0 = (int4)floor(sx), x1 = x0 + 1, z0 = (int4)floor(sz), z1 = z0 + 1;

            // 比较 X 和 Z 的相对大小，以确定 Simplex 噪声网格的形状
            bool4 xGz = sx - x0 > sz - z0;
            int4 xC = select(x0, x1, xGz), zC = select(z1, z0, xGz);

            // 使用 SmallXXHash4 生成哈希值
            SmallXXHash4 h0 = hash.Eat(x0), h1 = hash.Eat(x1), hC = SmallXXHash4.Select(h0, h1, xGz);

            // 计算噪声值并组合
            return default(G).EvaluateCombined(
                 Kernel(h0.Eat(z0), x0, z0, positions) +
                 Kernel(h1.Eat(z1), x1, z1, positions) +
                 Kernel(hC.Eat(zC), xC, zC, positions)
             );
        }

        // Kernel 方法，用于计算 2D 噪声
        static float4 Kernel(
            SmallXXHash4 hash, float4 lx, float4 lz, float4x3 positions
        )
        {
            // 取消偏移，恢复原始网格位置
            float4 unskew = (lx + lz) * ((3f - sqrt(3f)) / 6f);
            float4 x = positions.c0 - lx + unskew, z = positions.c2 - lz + unskew;
            // 计算衰减函数
            float4 f = 0.5f - x * x - z * z;
            f = f * f * f * 8f;
            // 使用梯度 Evaluate 方法计算噪声值
            return max(0f, f) * default(G).Evaluate(hash, x, z);
        }
    }

    // Simplex 3D 噪声结构，泛型 G 表示梯度类型
    public struct Simplex3D<G> : INoise where G : struct, IGradient
    {
        // 获取 3D Simplex 噪声
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            // 缩放位置，并应用 Simplex 特有的缩放因子
            positions *= frequency * 0.6f;
            float4 skew = (positions.c0 + positions.c1 + positions.c2) * (1f / 3f);
            float4 sx = positions.c0 + skew, sy = positions.c1 + skew, sz = positions.c2 + skew;

            // 计算网格坐标
            int4 x0 = (int4)floor(sx), x1 = x0 + 1, y0 = (int4)floor(sy), y1 = y0 + 1, z0 = (int4)floor(sz), z1 = z0 + 1;

            // 比较 X、Y、Z 的相对大小，确定 Simplex 噪声网格形状
            bool4 xGy = sx - x0 > sy - y0, xGz = sx - x0 > sz - z0, yGz = sy - y0 > sz - z0;
            bool4 xA = xGy & xGz, xB = xGy | (xGz & yGz), yA = !xGy & yGz, yB = !xGy | (xGz & yGz), zA = (xGy & !xGz) | (!xGy & !yGz), zB = !(xGz & yGz);

            // 选择对应的网格坐标
            int4 xCA = select(x0, x1, xA), xCB = select(x0, x1, xB), yCA = select(y0, y1, yA), yCB = select(y0, y1, yB), zCA = select(z0, z1, zA), zCB = select(z0, z1, zB);

            // 生成哈希值
            SmallXXHash4 h0 = hash.Eat(x0), h1 = hash.Eat(x1), hA = SmallXXHash4.Select(h0, h1, xA), hB = SmallXXHash4.Select(h0, h1, xB);

            // 计算噪声值并组合
            return default(G).EvaluateCombined(
                Kernel(h0.Eat(y0).Eat(z0), x0, y0, z0, positions) +
                Kernel(h1.Eat(y1).Eat(z1), x1, y1, z1, positions) +
                Kernel(hA.Eat(yCA).Eat(zCA), xCA, yCA, zCA, positions) +
                Kernel(hB.Eat(yCB).Eat(zCB), xCB, yCB, zCB, positions)
            );
        }

        // Kernel 方法，用于计算 3D 噪声
        static float4 Kernel(
            SmallXXHash4 hash, float4 lx, float4 ly, float4 lz, float4x3 positions
        )
        {
            // 取消偏移，恢复原始网格位置
            float4 unskew = (lx + ly + lz) * (1f / 6f);
            float4 x = positions.c0 - lx + unskew, y = positions.c1 - ly + unskew, z = positions.c2 - lz + unskew;
            // 计算衰减函数
            float4 f = 0.5f - x * x - y * y - z * z;
            f = f * f * f * 8f;
            // 使用梯度 Evaluate 方法计算噪声值
            return max(0f, f) * default(G).Evaluate(hash, x, y, z);
        }
    }
}
