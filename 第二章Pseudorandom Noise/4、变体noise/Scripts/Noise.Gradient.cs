using Unity.Mathematics;
using static Unity.Mathematics.math;

public static partial class Noise
{
    // �����ݶȽӿڣ������˸���ά�ȵ��������ɷ���
    public interface IGradient
    {
        // һά�����ļ���
        float4 Evaluate(SmallXXHash4 hash, float4 x);
        // ��ά�����ļ���
        float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y);
        // ��ά�����ļ���
        float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z);
        // ��ֵ�������ֵ�Ĵ���
        float4 EvaluateAfterInterpolation(float4 value);
    }

    // ֵ����ʵ��
    public struct Value : IGradient
    {
        // һάֵ���������ع�ϣֵ���ɵ�α�����
        public float4 Evaluate(SmallXXHash4 hash, float4 x) => hash.Floats01A * 2f - 1f;
        // ��άֵ������������һά
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
            hash.Floats01A * 2f - 1f;
        // ��άֵ������������һά
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
            hash.Floats01A * 2f - 1f;
        // ���Բ�ֵ�������ֵ���н�һ������
        public float4 EvaluateAfterInterpolation(float4 value) => value;
    }

    // Perlin����ʵ��
    public struct Perlin : IGradient
    {
        // һάPerlin������ͨ����ϣֵ��x���������ݶ�
        public float4 Evaluate(SmallXXHash4 hash, float4 x) =>
            (1f + hash.Floats01A) * select(-x, x, ((uint4)hash & 1 << 8) == 0);

        // ��άPerlin����
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y)
        {
            // ����x��y������ݶ�
            float4 gx = hash.Floats01A * 2f - 1f;
            float4 gy = 0.5f - abs(gx);
            gx -= floor(gx + 0.5f);
            // ����x��y�ļ�Ȩ��
            return (gx * x + gy * y) * (2f / 0.53528f);
        }

        // ��άPerlin����
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z)
        {
            // ����x��y��z������ݶ�
            float4 gx = hash.Floats01A * 2f - 1f, gy = hash.Floats01D * 2f - 1f;
            float4 gz = 1f - abs(gx) - abs(gy);
            float4 offset = max(-gz, 0f);
            gx += select(-offset, offset, gx < 0f);
            gy += select(-offset, offset, gy < 0f);
            // ����x��y��z�ļ�Ȩ��
            return (gx * x + gy * y + gz * z) * (1f / 0.56290f);
        }

        // ���Բ�ֵ�������ֵ���н�һ������
        public float4 EvaluateAfterInterpolation(float4 value) => value;
    }
}
