using Unity.Mathematics; // ʹ�� Unity Mathematics �������ѧ����
using static Unity.Mathematics.math; // ��̬���� math �࣬���������ѧ����

public static partial class Noise // ����һ����̬������ Noise
{
    // ���������ṹ�壬ʹ�÷��� G ʵ�֣�����ʹ�ò�ͬ�����������㷨
    public struct Turbulence<G> : IGradient where G : struct, IGradient
    {
        // һά���������������ϣֵ��Ӧ���ݶ�����
        public float4 Evaluate(SmallXXHash4 hash, float4 x) =>
            default(G).Evaluate(hash, x);

        // ��ά��������
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
            default(G).Evaluate(hash, x, y);

        // ��ά��������
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
            default(G).Evaluate(hash, x, y, z);

        // �Բ�ֵ�������ֵȡ����ֵ��ģ������Ч��
        public float4 EvaluateCombined(float4 value) =>
            abs(default(G).EvaluateCombined(value));
    }

    // һά����������ɽṹ�壬ʵ�� INoise �ӿ�
    public struct Lattice1D<L, G> : INoise
        where L : struct, ILattice where G : struct, IGradient
    {
        // ��ȡ����ֵ�ķ���������Ϊλ�ú͹�ϣֵ
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            // ��ȡλ�õĸ�㷶Χ
            LatticeSpan4 x = default(L).GetLatticeSpan4(positions.c0, frequency);
            var g = default(G);
            // ͨ����ֵ��������ֵ��������Χ�� [0, 1] ת���� [-1, 1]
            return g.EvaluateCombined(lerp(
                g.Evaluate(hash.Eat(x.p0), x.g0), g.Evaluate(hash.Eat(x.p1), x.g1), x.t
            )); // ��������ֵ
        }
    }

    // ��ά����������ɽṹ�壬ʵ�� INoise �ӿ�
    public struct Lattice2D<L, G> : INoise
        where L : struct, ILattice where G : struct, IGradient
    {
        // ��ȡ����ֵ�ķ���������Ϊλ�ú͹�ϣֵ
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);
            // ��ȡ x �� z �᷽��ĸ�㷶Χ
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),
                z = l.GetLatticeSpan4(positions.c2, frequency);
            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);
            var g = default(G);
            // ͨ����ֵ��������ֵ��ʹ��˫���Բ�ֵ
            return g.EvaluateCombined(lerp(
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
             )); // ��������ֵ
        }
    }

    // ��ά����������ɽṹ�壬ʵ�� INoise �ӿ�
    public struct Lattice3D<L, G> : INoise
         where L : struct, ILattice where G : struct, IGradient
    {
        // ��ȡ����ֵ�ķ���������Ϊλ�ú͹�ϣֵ
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);
            // ��ȡ x��y �� z �᷽��ĸ�㷶Χ
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),
                y = l.GetLatticeSpan4(positions.c1, frequency),
                z = l.GetLatticeSpan4(positions.c2, frequency);

            // �ӹ�ϣ�л�ȡ��صĹ�ϣֵ
            SmallXXHash4
                h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1),
                h00 = h0.Eat(y.p0), h01 = h0.Eat(y.p1),
                h10 = h1.Eat(y.p0), h11 = h1.Eat(y.p1);

            var g = default(G);
            // ͨ����ֵ��������ֵ��ʹ�������Բ�ֵ
            return g.EvaluateCombined(lerp(
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
            )); // ��������ֵ
        }
    }
}
