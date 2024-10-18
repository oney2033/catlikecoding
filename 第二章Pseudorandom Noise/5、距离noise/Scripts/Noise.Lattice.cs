using Unity.Mathematics; // 使用 Unity Mathematics 库进行数学运算
using static Unity.Mathematics.math; // 静态导入 math 类，方便调用数学函数

public static partial class Noise // 定义一个静态部分类 Noise
{
    // 湍流噪声结构体，使用泛型 G 实现，可以使用不同的噪声生成算法
    public struct Turbulence<G> : IGradient where G : struct, IGradient
    {
        // 一维湍流噪声，计算哈希值对应的梯度噪声
        public float4 Evaluate(SmallXXHash4 hash, float4 x) =>
            default(G).Evaluate(hash, x);

        // 二维湍流噪声
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
            default(G).Evaluate(hash, x, y);

        // 三维湍流噪声
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
            default(G).Evaluate(hash, x, y, z);

        // 对插值后的噪声值取绝对值，模拟湍流效果
        public float4 EvaluateAfterInterpolation(float4 value) =>
            abs(default(G).EvaluateAfterInterpolation(value));
    }

    // 一维格点噪声生成结构体，实现 INoise 接口
    public struct Lattice1D<L, G> : INoise
        where L : struct, ILattice where G : struct, IGradient
    {
        // 获取噪声值的方法，输入为位置和哈希值
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            // 获取位置的格点范围
            LatticeSpan4 x = default(L).GetLatticeSpan4(positions.c0, frequency);
            var g = default(G);
            // 通过插值计算噪声值，并将范围从 [0, 1] 转换到 [-1, 1]
            return g.EvaluateAfterInterpolation(lerp(
                g.Evaluate(hash.Eat(x.p0), x.g0), g.Evaluate(hash.Eat(x.p1), x.g1), x.t
            )); // 返回噪声值
        }
    }

    // 二维格点噪声生成结构体，实现 INoise 接口
    public struct Lattice2D<L, G> : INoise
        where L : struct, ILattice where G : struct, IGradient
    {
        // 获取噪声值的方法，输入为位置和哈希值
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);
            // 获取 x 和 z 轴方向的格点范围
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),
                z = l.GetLatticeSpan4(positions.c2, frequency);
            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);
            var g = default(G);
            // 通过插值计算噪声值，使用双线性插值
            return g.EvaluateAfterInterpolation(lerp(
                 lerp(
                     g.Evaluate(h0.Eat(z.p0), x.g0, z.g0),
                     g.Evaluate(h0.Eat(z.p1), x.g0, z.g1),
                     z.t
                 ),
                 lerp(
                     g.Evaluate(h1.Eat(z.p0), x.g1, z.g0),
                     g.Evaluate(h1.Eat(z.p1), x.g1, z.g1),
                     z.t
                 ),
                 x.t
             )); // 返回噪声值
        }
    }

    // 三维格点噪声生成结构体，实现 INoise 接口
    public struct Lattice3D<L, G> : INoise
         where L : struct, ILattice where G : struct, IGradient
    {
        // 获取噪声值的方法，输入为位置和哈希值
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);
            // 获取 x、y 和 z 轴方向的格点范围
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),
                y = l.GetLatticeSpan4(positions.c1, frequency),
                z = l.GetLatticeSpan4(positions.c2, frequency);

            // 从哈希中获取相关的哈希值
            SmallXXHash4
                h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1),
                h00 = h0.Eat(y.p0), h01 = h0.Eat(y.p1),
                h10 = h1.Eat(y.p0), h11 = h1.Eat(y.p1);

            var g = default(G);
            // 通过插值计算噪声值，使用三线性插值
            return g.EvaluateAfterInterpolation(lerp(
                lerp(
                    lerp(
                        g.Evaluate(h00.Eat(z.p0), x.g0, y.g0, z.g0),
                        g.Evaluate(h00.Eat(z.p1), x.g0, y.g0, z.g1),
                        z.t
                    ),
                    lerp(
                        g.Evaluate(h01.Eat(z.p0), x.g0, y.g1, z.g0),
                        g.Evaluate(h01.Eat(z.p1), x.g0, y.g1, z.g1),
                        z.t
                    ),
                    y.t
                ),
                lerp(
                    lerp(
                        g.Evaluate(h10.Eat(z.p0), x.g1, y.g0, z.g0),
                        g.Evaluate(h10.Eat(z.p1), x.g1, y.g0, z.g1),
                        z.t
                    ),
                    lerp(
                        g.Evaluate(h11.Eat(z.p0), x.g1, y.g1, z.g0),
                        g.Evaluate(h11.Eat(z.p1), x.g1, y.g1, z.g1),
                        z.t
                    ),
                    y.t
                ),
                x.t
            )); // 返回噪声值
        }
    }
}
