using Unity.Mathematics;
using static Unity.Mathematics.math;

public static partial class Noise
{
    // 噪声梯度接口，定义了各种维度的噪声生成方法
    public interface IGradient
    {
        // 一维噪声的计算
        float4 Evaluate(SmallXXHash4 hash, float4 x);
        // 二维噪声的计算
        float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y);
        // 三维噪声的计算
        float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z);
        // 插值后对噪声值的处理
        float4 EvaluateCombined(float4 value);
    }

    // 值噪声的实现，返回哈希值生成的伪随机数作为噪声值
    public struct Value : IGradient
    {
        // 一维值噪声的计算，直接使用哈希值生成的伪随机数
        public float4 Evaluate(SmallXXHash4 hash, float4 x) => hash.Floats01A * 2f - 1f;

        // 二维值噪声的计算，和一维类似，返回哈希值生成的伪随机数
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
            hash.Floats01A * 2f - 1f;

        // 三维值噪声的计算，和一维类似
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
            hash.Floats01A * 2f - 1f;

        // 对插值后的噪声值不进行进一步处理，直接返回
        public float4 EvaluateCombined(float4 value) => value;
    }

    // BaseGradients 类提供了一些基本的梯度计算方法，适用于不同类型的噪声
    public static class BaseGradients
    {
        // 线性梯度计算，基于输入的 x 生成梯度
        public static float4 Line(SmallXXHash4 hash, float4 x) =>
            (1f + hash.Floats01A) * select(-x, x, ((uint4)hash & 1 << 8) == 0);

        // 生成一个正方形向量，用于二维噪声
        static float4x2 SquareVectors(SmallXXHash4 hash)
        {
            float4x2 v;
            v.c0 = hash.Floats01A * 2f - 1f;
            v.c1 = 0.5f - abs(v.c0);
            v.c0 -= floor(v.c0 + 0.5f);
            return v;
        }

        // 生成一个八面体向量，用于三维噪声
        static float4x3 OctahedronVectors(SmallXXHash4 hash)
        {
            float4x3 g;
            g.c0 = hash.Floats01A * 2f - 1f;
            g.c1 = hash.Floats01D * 2f - 1f;
            g.c2 = 1f - abs(g.c0) - abs(g.c1);
            float4 offset = max(-g.c2, 0f);
            g.c0 += select(-offset, offset, g.c0 < 0f);
            g.c1 += select(-offset, offset, g.c1 < 0f);
            return g;
        }

        // 二维正方形梯度计算
        public static float4 Square(SmallXXHash4 hash, float4 x, float4 y)
        {
            float4x2 v = SquareVectors(hash);
            return v.c0 * x + v.c1 * y;
        }

        // 二维圆形梯度计算，结果归一化
        public static float4 Circle(SmallXXHash4 hash, float4 x, float4 y)
        {
            float4x2 v = SquareVectors(hash);
            return (v.c0 * x + v.c1 * y) * rsqrt(v.c0 * v.c0 + v.c1 * v.c1);
        }

        // 三维八面体梯度计算
        public static float4 Octahedron(
            SmallXXHash4 hash, float4 x, float4 y, float4 z
        )
        {
            float4x3 v = OctahedronVectors(hash);
            return v.c0 * x + v.c1 * y + v.c2 * z;
        }

        // 三维球体梯度计算，结果归一化
        public static float4 Sphere(SmallXXHash4 hash, float4 x, float4 y, float4 z)
        {
            float4x3 v = OctahedronVectors(hash);
            return
                (v.c0 * x + v.c1 * y + v.c2 * z) *
                rsqrt(v.c0 * v.c0 + v.c1 * v.c1 + v.c2 * v.c2);
        }
    }

    // Perlin噪声的实现
    public struct Perlin : IGradient
    {
        // 一维Perlin噪声的计算，使用线性梯度
        public float4 Evaluate(SmallXXHash4 hash, float4 x) =>
           BaseGradients.Line(hash, x);

        // 二维Perlin噪声的计算，使用正方形梯度，并进行放缩
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
            BaseGradients.Square(hash, x, y) * (2f / 0.53528f);

        // 三维Perlin噪声的计算，使用八面体梯度，并进行放缩
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
            BaseGradients.Octahedron(hash, x, y, z) * (1f / 0.56290f);

        // 对插值后的噪声值不进行进一步处理，直接返回
        public float4 EvaluateCombined(float4 value) => value;
    }

    // Simplex噪声的实现
    public struct Simplex : IGradient
    {
        // 一维Simplex噪声的计算，使用线性梯度，并进行放缩
        public float4 Evaluate(SmallXXHash4 hash, float4 x) =>
            BaseGradients.Line(hash, x) * (32f / 27f);

        // 二维Simplex噪声的计算，使用圆形梯度，并进行放缩
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
            BaseGradients.Circle(hash, x, y) * (5.832f / sqrt(2f));

        // 三维Simplex噪声的计算，使用球体梯度，并进行放缩
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
            BaseGradients.Sphere(hash, x, y, z) * (1024f / (125f * sqrt(3f)));

        // 对插值后的噪声值不进行进一步处理，直接返回
        public float4 EvaluateCombined(float4 value) => value;
    }
}
