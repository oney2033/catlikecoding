using Unity.Mathematics; // ʹ�� Unity Mathematics �������ѧ����
using static Unity.Mathematics.math; // ��̬���� math �࣬���������ѧ����

public static partial class Noise // ����һ����̬������ Noise
{
    // һά����������ɽṹ�壬ʵ�� INoise �ӿ�
    public struct Lattice1D<G> : INoise where G : struct, IGradient
    {
        // ��ȡ����ֵ�ķ���������Ϊλ�ú͹�ϣֵ
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
        {
            // ��ȡλ�õĸ�㷶Χ
            LatticeSpan4 x = GetLatticeSpan4(positions.c0);
            var g = default(G);
            // ͨ����ֵ��������ֵ��������Χ�� [0, 1] ת���� [-1, 1]
            return lerp(
                g.Evaluate(hash.Eat(x.p0), x.g0), g.Evaluate(hash.Eat(x.p1), x.g1), x.t
            ); // ��������ֵ
        }
    }

    // ��ά����������ɽṹ�壬ʵ�� INoise �ӿ�
    public struct Lattice2D<G> : INoise where G : struct, IGradient
    {
        // ��ȡ����ֵ�ķ���������Ϊλ�ú͹�ϣֵ
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
        {
            // ��ȡλ�õĸ�㷶Χ
            LatticeSpan4
                x = GetLatticeSpan4(positions.c0), z = GetLatticeSpan4(positions.c2);
            // �ӹ�ϣ�л�ȡ��صĹ�ϣֵ
            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);
            // ͨ����ֵ��������ֵ
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
             ); // ��������ֵ
        }
    }

    // ��ά����������ɽṹ�壬ʵ�� INoise �ӿ�
    public struct Lattice3D<G> : INoise where G : struct, IGradient
    {
        // ��ȡ����ֵ�ķ���������Ϊλ�ú͹�ϣֵ
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
        {
            // ��ȡλ�õĸ�㷶Χ
            LatticeSpan4
                x = GetLatticeSpan4(positions.c0),
                y = GetLatticeSpan4(positions.c1),
                z = GetLatticeSpan4(positions.c2);

            // �ӹ�ϣ�л�ȡ��صĹ�ϣֵ
            SmallXXHash4
                h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1),
                h00 = h0.Eat(y.p0), h01 = h0.Eat(y.p1),
                h10 = h1.Eat(y.p0), h11 = h1.Eat(y.p1);

            var g = default(G);
            // ͨ����ֵ��������ֵ
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
            ); // ��������ֵ
        }
    }
}
