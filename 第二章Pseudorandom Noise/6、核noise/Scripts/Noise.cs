using Unity.Burst;                // ʹ�� Burst ���������Ż�����
using Unity.Collections;          // ʹ�� NativeArray ��ԭ�����ݽṹ
using Unity.Jobs;                 // ʹ�� Job System ���ж��̵߳���
using Unity.Mathematics;           // ʹ�� Unity Mathematics �������ѧ����
using static Unity.Mathematics.math; // ��̬���� math �࣬���������ѧ����
using System;
using UnityEngine;

public static partial class Noise // ����һ����̬������ Noise��������������
{
    // �������ɵ����ýṹ�壬�û������Զ����������ɵ�����
    [Serializable] // ʹ�ýṹ����� Unity �༭�������л�
    public struct Settings
    {
        public int seed; // �������ɵ�����ֵ�����������
        [Min(1)]
        public int frequency; // ����Ƶ�ʣ�Ӱ������ͼ��ϸ�ڲ��
        [Range(1, 6)]
        public int octaves; // �������ӵĲ���������Խ�ߣ�ϸ��Խ�ḻ
        [Range(2, 4)]
        public int lacunarity; // ��Ƶ��������ÿ��������Ƶ�������
        [Range(0f, 1f)]
        public float persistence; // �־��ԣ��������ӵ�ÿһ�����������ս����Ӱ�����

        // Ĭ�����ã�ʹ�� 4 ��Ƶ�ʣ�1 �㣬2 ��Ƶ�� 0.5 �ĳ־���
        public static Settings Default => new Settings
        {
            frequency = 4,
            octaves = 1,
            lacunarity = 2,
            persistence = 0.5f
        };
    }

    // ����һ���ṹ�� LatticeSpan4�����ڴ洢�����Ϣ
    public struct LatticeSpan4
    {
        public int4 p0, p1; // �洢�ϵͺͽϸߵĸ������
        public float4 g0, g1; // �洢��㵽���������ƫ����
        public float4 t; // �洢��ֵ���ӣ�����ƽ������
    }

    // ����һ���ӿ� ILattice�����ڻ�ȡ��㷶Χ
    public interface ILattice
    {
        LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency); // �ӿڷ����������㷶Χ
        int4 ValidateSingleStep(int4 points, int frequency);
    }

    // LatticeNormal �ṹ��ʵ�� ILattice �ӿڣ��������ɱ�׼���
    public struct LatticeNormal : ILattice
    {
        // GetLatticeSpan4 ������������ĸ�㷶Χ
        public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
        {
            coordinates *= frequency; // ����������Ŵ�ָ��Ƶ��
            float4 points = floor(coordinates); // ����ȡ������ȡ�������
            LatticeSpan4 span; // ���� LatticeSpan4 ����
            span.p0 = (int4)points; // ��������ת��Ϊ���Ͳ���ֵ�� p0
            span.p1 = span.p0 + 1; // p1 �� p0 + 1�����ϸߵĸ��
            span.g0 = coordinates - span.p0; // ��������ƫ����
            span.g1 = span.g0 - 1f;
            span.t = coordinates - points; // ����λ���ڸ��֮������λ��
            // ʹ�� S���߲�ֵ������ t ת���� [0, 1] �ķ�Χ��
            span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
            return span; // ���� LatticeSpan4 �ṹ��
        }
        public int4 ValidateSingleStep(int4 points, int frequency) => points;
    }

    // LatticeTiling �ṹ��ʵ�� ILattice �ӿڣ���������ƽ�̸��
    public struct LatticeTiling : ILattice
    {
        // GetLatticeSpan4 ������������ĸ�㷶Χ��֧��ƽ��
        public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
        {
            coordinates *= frequency; // ����������Ŵ�ָ��Ƶ��
            float4 points = floor(coordinates); // ����ȡ������ȡ�������
            LatticeSpan4 span; // ���� LatticeSpan4 ����
            span.p0 = (int4)points; // ��������ת��Ϊ���Ͳ���ֵ�� p0
            span.g0 = coordinates - span.p0; // ����ƫ����
            span.g1 = span.g0 - 1f;
            span.p0 -= (int4)ceil(points / frequency) * frequency; // ȷ��ƽ��
            span.p0 = select(span.p0, span.p0 + frequency, span.p0 < 0); // ������
            span.p1 = span.p0 + 1; // �ϸߵĸ��
            span.p1 = select(span.p1, 0, span.p1 == frequency); // ����߽����
            span.t = coordinates - points; // �����ֵ
            // ʹ�� S���߲�ֵ������ t ת���� [0, 1] �ķ�Χ��
            span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
            return span; // ���� LatticeSpan4 �ṹ��
        }
        public int4 ValidateSingleStep(int4 points, int frequency) =>
            select(select(points, 0, points == frequency), frequency - 1, points == -1);
    }

    // ����һ���ӿ� INoise�����ڻ�ȡ����ֵ
    public interface INoise
    {
        float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency); // �ӿڷ���������λ�ú͹�ϣֵ����������ֵ
    }

    // Job<N> �ṹ�����ڲ��д����������㣬N ��Ҫʵ�� INoise �ӿ�
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise
    {
        [ReadOnly] // ֻ�����ԣ���ʾ�����鲻�ᱻ�޸�
        public NativeArray<float3x4> positions; // �洢λ�����ݵ�ԭ������

        [WriteOnly] // ֻд���ԣ���ʾ�����齫ֻ����д������
        public NativeArray<float4> noise; // �洢�����������ֵ��ԭ������

        public Settings settings; // ������������
        public float3x4 domainTRS; // ����ת��λ�õı任����

        // Execute ��������������ֵ
        public void Execute(int i)
        {
            float4x3 position = domainTRS.TransformVectors(transpose(positions[i])); // λ�ñ任
            var hash = SmallXXHash4.Seed(settings.seed); // ʹ���������ɹ�ϣֵ
            int frequency = settings.frequency; // ��ʼƵ��
            float amplitude = 1f, amplitudeSum = 0f; // ���������ܺ�
            float4 sum = 0f; // ���ڴ洢����ֵ���ۼӽ��

            // ��������Ƶ������ֵ
            for (int o = 0; o < settings.octaves; o++)
            {
                sum += amplitude * default(N).GetNoise4(position, hash + o, frequency); // ��������
                frequency *= settings.lacunarity; // Ƶ�ʱ���
                amplitude *= settings.persistence; // ���˥��
                amplitudeSum += amplitude; // �ۼ����
            }
            noise[i] = sum / amplitudeSum; // ��һ������ֵ
        }

        // ��̬���� ScheduleParallel ���ڵ��Ȳ��м���
        public static JobHandle ScheduleParallel(
            NativeArray<float3x4> positions, NativeArray<float4> noise,
            Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
        ) => new Job<N>
        {
            positions = positions, // ��ֵλ������
            noise = noise, // ��ֵ��������
            settings = settings,
            domainTRS = domainTRS.Matrix, // ��ֵ�任����
        }.ScheduleParallel(positions.Length, resolution, dependency); // ���Ȳ��м�������
    }

    // ����ί������ ScheduleDelegate�����ڵ���������������
    public delegate JobHandle ScheduleDelegate(
        NativeArray<float3x4> positions, NativeArray<float4> noise,
        Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
    );
}
