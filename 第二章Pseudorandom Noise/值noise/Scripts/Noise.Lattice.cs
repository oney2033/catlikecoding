using Unity.Mathematics; // ʹ�� Unity Mathematics �������ѧ����
using static Unity.Mathematics.math; // ��̬���� math �࣬���������ѧ����

public static partial class Noise // ����һ����̬������ Noise
{
    // һά����������ɽṹ�壬ʵ�� INoise �ӿ�
    public struct Lattice1D : INoise
    {
        // ��ȡ����ֵ�ķ���������Ϊλ�ú͹�ϣֵ
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
        {
            // ��ȡλ�õĸ�㷶Χ
            LatticeSpan4 x = GetLatticeSpan4(positions.c0);
            // ͨ����ֵ��������ֵ��������Χ�� [0, 1] ת���� [-1, 1]
            return lerp(
                hash.Eat(x.p0).Floats01A, hash.Eat(x.p1).Floats01A, x.t
            ) * 2f - 1f; // ��������ֵ
        }
    }

    // ��ά����������ɽṹ�壬ʵ�� INoise �ӿ�
    public struct Lattice2D : INoise
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
            return lerp(
                lerp(h0.Eat(z.p0).Floats01A, h0.Eat(z.p1).Floats01A, z.t),
                lerp(h1.Eat(z.p0).Floats01A, h1.Eat(z.p1).Floats01A, z.t),
                x.t
            ) * 2f - 1f; // ��������ֵ
        }
    }

    // ��ά����������ɽṹ�壬ʵ�� INoise �ӿ�
    public struct Lattice3D : INoise
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

            // ͨ����ֵ��������ֵ
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
            ) * 2f - 1f; // ��������ֵ
        }
    }
}
