using Unity.Burst;                // ʹ�� Burst ���������Ż�����
using Unity.Collections;          // ʹ�� NativeArray ��ԭ�����ݽṹ
using Unity.Jobs;                 // ʹ�� Job System ���ж��̵߳���
using Unity.Mathematics;           // ʹ�� Unity Mathematics �������ѧ����
using static Unity.Mathematics.math; // ��̬���� math �࣬���������ѧ����

public static partial class Noise // ����һ����̬������ Noise
{
    public struct Perlin : IGradient
    {
        public float4 Evaluate(SmallXXHash4 hash, float4 x) => (1f + hash.Floats01A) * select(-x, x, ((uint4)hash & 1 << 8) == 0);
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y)
        {
            float4 gx = hash.Floats01A * 2f - 1f;
            float4 gy = 0.5f - abs(gx);
            gx -= floor(gx + 0.5f);
            return (gx * x + gy * y) * (2f / 0.53528f);
        }
        public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z)
        {
            float4 gx = hash.Floats01A * 2f - 1f, gy = hash.Floats01D * 2f - 1f;
            float4 gz = 1f - abs(gx) - abs(gy);
            float4 offset = max(-gz, 0f);
            gx += select(-offset, offset, gx < 0f);
            gy += select(-offset, offset, gy < 0f);
            return (gx * x + gy * y + gz * z) * (1f / 0.56290f);
        }
    }

    struct LatticeSpan4 // ����һ���ṹ�� LatticeSpan4
    {
        public int4 p0, p1; // �洢�������� 4 Ԫ�飬�ֱ����ϵͺͽϸߵĸ��
        public float4 g0, g1;
        public float4 t; // �洢��ֵ����
    }

    // ���� GetLatticeSpan4 ������������ĸ��ӷ�Χ
    static LatticeSpan4 GetLatticeSpan4(float4 coordinates)
    {
        float4 points = floor(coordinates); // ����ȡ������ȡ�������
        LatticeSpan4 span; // ���� LatticeSpan4 ����
        span.p0 = (int4)points; // ��������ת��Ϊ���Ͳ���ֵ�� p0
        span.p1 = span.p0 + 1; // p1 �� p0 + 1�����ϸߵĸ��
        span.g0 = coordinates - span.p0;
        span.g1 = span.g0 - 1f;
        span.t = coordinates - points; // ����λ���ڸ��֮������λ��
        // ʹ�� S���߲�ֵ������ t ת���� [0, 1] �ķ�Χ��
        span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
        return span; // ���� LatticeSpan4 �ṹ��
    }

    // ����һ���ӿ� INoise�����ڻ�ȡ����ֵ
    public interface INoise
    {
        float4 GetNoise4(float4x3 positions, SmallXXHash4 hash); // �ӿڷ���������λ�ú͹�ϣֵ����������ֵ
    }

    // Job<N> �ṹ�����ڲ��д����������㣬N ��Ҫʵ�� INoise �ӿ�
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise
    {
        [ReadOnly] // ֻ�����ԣ���ʾ�����鲻�ᱻ�޸�
        public NativeArray<float3x4> positions; // �洢λ�����ݵ�ԭ������

        [WriteOnly] // ֻд���ԣ���ʾ�����齫ֻ����д������
        public NativeArray<float4> noise; // �洢�����������ֵ��ԭ������

        public SmallXXHash4 hash; // ���ڹ�ϣ����Ĺ�ϣֵ

        public float3x4 domainTRS; // ����ת��λ�õı任����

        // ִ�з�������������
        public void Execute(int i)
        {
            // �ڱ任������������ɽӿڣ��洢���
            noise[i] = default(N).GetNoise4(
                domainTRS.TransformVectors(transpose(positions[i])), hash
            );
        }

        // ��̬���� ScheduleParallel ���ڵ��Ȳ��м���
        public static JobHandle ScheduleParallel(
            NativeArray<float3x4> positions, NativeArray<float4> noise,
            int seed, SpaceTRS domainTRS, int resolution, JobHandle dependency
        ) => new Job<N>
        {
            positions = positions, // ��ֵλ������
            noise = noise, // ��ֵ��������
            hash = SmallXXHash.Seed(seed), // ���ɹ�ϣֵ
            domainTRS = domainTRS.Matrix, // ��ֵ�任����
        }.ScheduleParallel(positions.Length, resolution, dependency); // ���Ȳ��м�������
    }

    // ����ί������ ScheduleDelegate�����ڵ���������������
    public delegate JobHandle ScheduleDelegate(
        NativeArray<float3x4> positions, NativeArray<float4> noise,
        int seed, SpaceTRS domainTRS, int resolution, JobHandle dependency
    );
}
