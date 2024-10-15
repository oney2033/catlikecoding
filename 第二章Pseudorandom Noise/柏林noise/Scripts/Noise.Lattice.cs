using Unity.Mathematics; // 使用 Unity Mathematics 库进行数学运算
using static Unity.Mathematics.math; // 静态导入 math 类，方便调用数学函数

public static partial class Noise // 定义一个静态部分类 Noise
{
    // 一维格点噪声生成结构体，实现 INoise 接口
    public struct Lattice1D<G> : INoise where G : struct, IGradient
    {
        // 获取噪声值的方法，输入为位置和哈希值
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
        {
            // 获取位置的格点范围
            LatticeSpan4 x = GetLatticeSpan4(positions.c0);
            var g = default(G);
            // 通过插值计算噪声值，并将范围从 [0, 1] 转换到 [-1, 1]
            return lerp(
                g.Evaluate(hash.Eat(x.p0), x.g0), g.Evaluate(hash.Eat(x.p1), x.g1), x.t
            ); // 返回噪声值
        }
    }

    // 二维格点噪声生成结构体，实现 INoise 接口
    public struct Lattice2D<G> : INoise where G : struct, IGradient
    {
        // 获取噪声值的方法，输入为位置和哈希值
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
        {
            // 获取位置的格点范围
            LatticeSpan4
                x = GetLatticeSpan4(positions.c0), z = GetLatticeSpan4(positions.c2);
            // 从哈希中获取相关的哈希值
            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);
            // 通过插值计算噪声值
            var g = default(G);
            return lerp(
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
             ); // 返回噪声值
        }
    }

    // 三维格点噪声生成结构体，实现 INoise 接口
    public struct Lattice3D<G> : INoise where G : struct, IGradient
    {
        // 获取噪声值的方法，输入为位置和哈希值
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
        {
            // 获取位置的格点范围
            LatticeSpan4
                x = GetLatticeSpan4(positions.c0),
                y = GetLatticeSpan4(positions.c1),
                z = GetLatticeSpan4(positions.c2);

            // 从哈希中获取相关的哈希值
            SmallXXHash4
                h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1),
                h00 = h0.Eat(y.p0), h01 = h0.Eat(y.p1),
                h10 = h1.Eat(y.p0), h11 = h1.Eat(y.p1);

            var g = default(G);
            // 通过插值计算噪声值
            return lerp(
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
            ); // 返回噪声值
        }
    }
}
