using Unity.Mathematics; // 使用 Unity Mathematics 库进行数学运算
using static Unity.Mathematics.math; // 静态导入 math 类，方便调用数学函数

public static partial class Noise // 定义一个静态部分类 Noise
{
    // 一维格点噪声生成结构体，实现 INoise 接口
    public struct Lattice1D : INoise
    {
        // 获取噪声值的方法，输入为位置和哈希值
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
        {
            // 获取位置的格点范围
            LatticeSpan4 x = GetLatticeSpan4(positions.c0);
            // 通过插值计算噪声值，并将范围从 [0, 1] 转换到 [-1, 1]
            return lerp(
                hash.Eat(x.p0).Floats01A, hash.Eat(x.p1).Floats01A, x.t
            ) * 2f - 1f; // 返回噪声值
        }
    }

    // 二维格点噪声生成结构体，实现 INoise 接口
    public struct Lattice2D : INoise
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
            return lerp(
                lerp(h0.Eat(z.p0).Floats01A, h0.Eat(z.p1).Floats01A, z.t),
                lerp(h1.Eat(z.p0).Floats01A, h1.Eat(z.p1).Floats01A, z.t),
                x.t
            ) * 2f - 1f; // 返回噪声值
        }
    }

    // 三维格点噪声生成结构体，实现 INoise 接口
    public struct Lattice3D : INoise
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

            // 通过插值计算噪声值
            return lerp(
                lerp(
                    lerp(h00.Eat(z.p0).Floats01A, h00.Eat(z.p1).Floats01A, z.t),
                    lerp(h01.Eat(z.p0).Floats01A, h01.Eat(z.p1).Floats01A, z.t),
                    y.t
                ),
                lerp(
                    lerp(h10.Eat(z.p0).Floats01A, h10.Eat(z.p1).Floats01A, z.t),
                    lerp(h11.Eat(z.p0).Floats01A, h11.Eat(z.p1).Floats01A, z.t),
                    y.t
                ),
                x.t
            ) * 2f - 1f; // 返回噪声值
        }
    }
}
