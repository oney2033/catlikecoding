using Unity.Mathematics; // ���� Unity.Mathematics ��������ѧ����
using static Unity.Mathematics.math; // ��̬���� math ���е����з���

// Noise ���еľ�̬���֣�����ʵ�� Voronoi ����
public static partial class Noise
{

    // Worley ���������ʵ�֣�����ŷ����þ���
    public struct Worley : IVoronoiDistance
    {
        // 1D ���㣺��������ľ���ֵ����ʾ����
        public float4 GetDistance(float4 x) => abs(x);

        // 2D ���㣺����ŷ�����ƽ�����루������ƽ�������Ż����ܣ�
        public float4 GetDistance(float4 x, float4 y) => x * x + y * y;

        // 3D ���㣺����ŷ�����ƽ������
        public float4 GetDistance(float4 x, float4 y, float4 z) => x * x + y * y + z * z;

        // 1D ���ջ����������ı� minima
        public float4x2 Finalize1D(float4x2 minima) => minima;

        // 2D ���ջ�����������ƽ�����Ի�����յ�ŷ����þ��룬��ȷ��ֵ������� 1
        public float4x2 Finalize2D(float4x2 minima)
        {
            minima.c0 = sqrt(min(minima.c0, 1f));
            minima.c1 = sqrt(min(minima.c1, 1f));
            return minima;
        }

        // 3D ���ջ��������� 2D ���߼�
        public float4x2 Finalize3D(float4x2 minima) => Finalize2D(minima);
    }

    // Chebyshev ���������ʵ�֣������б�ѩ�����
    public struct Chebyshev : IVoronoiDistance
    {
        // 1D ���㣺���ؾ���ֵ����ʾ����
        public float4 GetDistance(float4 x) => abs(x);

        // 2D ���㣺���� x �� y ���Ͼ�������ֵ
        public float4 GetDistance(float4 x, float4 y) => max(abs(x), abs(y));

        // 3D ���㣺���� x��y �� z ���Ͼ�������ֵ
        public float4 GetDistance(float4 x, float4 y, float4 z) => max(max(abs(x), abs(y)), abs(z));

        // 1D ���ջ����������ı� minima
        public float4x2 Finalize1D(float4x2 minima) => minima;

        // 2D ���ջ����������ı� minima
        public float4x2 Finalize2D(float4x2 minima) => minima;

        // 3D ���ջ����������ı� minima
        public float4x2 Finalize3D(float4x2 minima) => minima;
    }

    // ���� Voronoi ��Сֵ�ķ���
    static float4x2 UpdateVoronoiMinima(float4x2 minima, float4 distances)
    {
        // �ж���Щ����ȵ�ǰ��Сֵ��С
        bool4 newMinimum = distances < minima.c0;

        // ���µڶ���Сֵ�������ֵ�ȵ�ǰ�ڶ���СֵС
        minima.c1 = select(
            select(minima.c1, distances, distances < minima.c1),
            minima.c0,  // �ѵ�ǰ��Сֵת�Ƹ��ڶ���Сֵ
            newMinimum  // ���������Сֵ����
        );

        // ������Сֵ
        minima.c0 = select(minima.c0, distances, newMinimum);
        return minima;
    }

    // 1D Voronoi �������ɽṹ��
    public struct Voronoi1D<L, D, F> : INoise
         where L : struct, ILattice
         where D : struct, IVoronoiDistance
         where F : struct, IVoronoiFunction
    {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);  // ʹ��Ĭ�ϵ� lattice ʵ��
            var d = default(D);  // ʹ��Ĭ�ϵ� Voronoi ����ʵ��
            LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency);  // ��ȡ x ��ĸ�㷶Χ

            // ��ʼ����Сֵ������Ϊ 2����ʾ���޴�
            float4x2 minima = 2f;
            for (int u = -1; u <= 1; u++)
            {
                // ���� x ���ƫ�ƣ����¹�ϣֵ
                SmallXXHash4 h = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));

                // ������벢������Сֵ
                minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01A + u - x.g0));
            }

            // �������յ� Voronoi ����ֵ
            return default(F).Evaluate(d.Finalize1D(minima));
        }
    }

    // 2D Voronoi �������ɽṹ��
    public struct Voronoi2D<L, D, F> : INoise
         where L : struct, ILattice
         where D : struct, IVoronoiDistance
         where F : struct, IVoronoiFunction
    {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);  // ʹ�� lattice ʵ��
            var d = default(D);  // ʹ�� Voronoi ����ʵ��
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),  // ��ȡ x �᷶Χ
                z = l.GetLatticeSpan4(positions.c2, frequency);  // ��ȡ z �᷶Χ

            float4x2 minima = 2f;  // ��ʼ����Сֵ
            for (int u = -1; u <= 1; u++)
            {
                SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));  // ���� x ƫ��
                float4 xOffset = u - x.g0;  // x ƫ����
                for (int v = -1; v <= 1; v++)
                {
                    SmallXXHash4 h = hx.Eat(l.ValidateSingleStep(z.p0 + v, frequency));  // ���� z ƫ��
                    float4 zOffset = v - z.g0;  // z ƫ����

                    // ������벢������Сֵ
                    minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01A + xOffset, h.Floats01B + zOffset));
                    minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01C + xOffset, h.Floats01D + zOffset));
                }
            }

            // �������յ� Voronoi ����ֵ
            return default(F).Evaluate(d.Finalize2D(minima));
        }
    }

    // 3D Voronoi �������ɽṹ��
    public struct Voronoi3D<L, D, F> : INoise
         where L : struct, ILattice
         where D : struct, IVoronoiDistance
         where F : struct, IVoronoiFunction
    {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
        {
            var l = default(L);  // ʹ�� lattice ʵ��
            var d = default(D);  // ʹ�� Voronoi ����ʵ��
            LatticeSpan4
                x = l.GetLatticeSpan4(positions.c0, frequency),  // ��ȡ x �᷶Χ
                y = l.GetLatticeSpan4(positions.c1, frequency),  // ��ȡ y �᷶Χ
                z = l.GetLatticeSpan4(positions.c2, frequency);  // ��ȡ z �᷶Χ

            float4x2 minima = 2f;  // ��ʼ����Сֵ
            for (int u = -1; u <= 1; u++)
            {
                SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));  // ���� x ƫ��
                float4 xOffset = u - x.g0;  // x ƫ����
                for (int v = -1; v <= 1; v++)
                {
                    SmallXXHash4 hy = hx.Eat(l.ValidateSingleStep(y.p0 + v, frequency));  // ���� y ƫ��
                    float4 yOffset = v - y.g0;  // y ƫ����
                    for (int w = -1; w <= 1; w++)
                    {
                        SmallXXHash4 h = hy.Eat(l.ValidateSingleStep(z.p0 + w, frequency));  // ���� z ƫ��
                        float4 zOffset = w - z.g0;  // z ƫ����

                        // ������벢������Сֵ
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

            // �������յ� Voronoi ����ֵ
            return default(F).Evaluate(d.Finalize3D(minima));
        }
    }
}
