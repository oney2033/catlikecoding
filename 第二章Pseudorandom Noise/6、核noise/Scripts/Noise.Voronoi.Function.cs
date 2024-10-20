using Unity.Mathematics; // ���� Unity.Mathematics �⣬������ѧ����

// Noise �����ռ��еľ�̬�࣬������������������ص�����
public static partial class Noise
{

    // ����һ���ӿ� IVoronoiFunction�����ڼ��� Voronoi ����
    public interface IVoronoiFunction
    {
        // ���� Voronoi ��Сֵ�� (minima)��������һ�� float4 ֵ
        float4 Evaluate(float4x2 minima);
    }

    // F1 �ṹ��ʵ���� IVoronoiFunction �ӿڣ����ڻ�ȡ����ľ���
    public struct F1 : IVoronoiFunction
    {
        // Evaluate ������������ľ��룬�� minima �ĵ�һ�� (c0)
        public float4 Evaluate(float4x2 distances) => distances.c0;
    }

    // F2 �ṹ��ʵ���� IVoronoiFunction �ӿڣ����ڻ�ȡ�ڶ����ľ���
    public struct F2 : IVoronoiFunction
    {
        // Evaluate �������صڶ����ľ��룬�� minima �ĵڶ��� (c1)
        public float4 Evaluate(float4x2 distances) => distances.c1;
    }

    // F2MinusF1 �ṹ��ʵ���� IVoronoiFunction �ӿڣ����ڻ�ȡ�ڶ������������Ĳ�ֵ
    public struct F2MinusF1 : IVoronoiFunction
    {
        // Evaluate �������صڶ����ľ����ȥ����ľ��� (c1 - c0)
        public float4 Evaluate(float4x2 distances) => distances.c1 - distances.c0;
    }
}
