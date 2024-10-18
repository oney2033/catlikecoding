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
        float4 EvaluateAfterInterpolation(float4 value);
    }

    // 值噪声实现
    public struct Value : IGradient
    {
        // 一维值噪声，返回哈希值生成的伪随机数
        public float4 Evaluate(SmallXXHash4 hash, float4 x) => hash.Floats01A * 2f - 1f;
        // 二维值噪声，类似于一维
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
            hash.Floats01A * 2f - 1f;
        // 三维值噪声，类似于一维
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
            hash.Floats01A * 2f - 1f;
        // 不对插值后的噪声值进行进一步处理
        public float4 EvaluateAfterInterpolation(float4 value) => value;
    }

    // Perlin噪声实现
    public struct Perlin : IGradient
    {
        // 一维Perlin噪声，通过哈希值和x方向生成梯度
        public float4 Evaluate(SmallXXHash4 hash, float4 x) =>
            (1f + hash.Floats01A) * select(-x, x, ((uint4)hash & 1 << 8) == 0);

        // 二维Perlin噪声
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y)
        {
            // 计算x和y方向的梯度
            float4 gx = hash.Floats01A * 2f - 1f;
            float4 gy = 0.5f - abs(gx);
            gx -= floor(gx + 0.5f);
            // 返回x和y的加权和
            return (gx * x + gy * y) * (2f / 0.53528f);
        }

        // 三维Perlin噪声
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z)
        {
            // 计算x、y和z方向的梯度
            float4 gx = hash.Floats01A * 2f - 1f, gy = hash.Floats01D * 2f - 1f;
            float4 gz = 1f - abs(gx) - abs(gy);
            float4 offset = max(-gz, 0f);
            gx += select(-offset, offset, gx < 0f);
            gy += select(-offset, offset, gy < 0f);
            // 返回x、y和z的加权和
            return (gx * x + gy * y + gz * z) * (1f / 0.56290f);
        }

        // 不对插值后的噪声值进行进一步处理
        public float4 EvaluateAfterInterpolation(float4 value) => value;
    }
}
