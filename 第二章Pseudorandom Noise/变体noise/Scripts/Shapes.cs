using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class Shapes // ����һ����̬�� Shapes�����ڹ�����״����
{
    // ����һ��ί�����ͣ����ڵ�����������
    public delegate JobHandle ScheduleDelegate(
        NativeArray<float3x4> positions, NativeArray<float3x4> normals, // λ�úͷ�������
        int resolution, float4x4 trs, JobHandle dependency // ���ɷֱ��ʡ��任�����������
    );

    // ���� Torus��Բ����״���ṹ�壬ʵ�� IShape �ӿ�
    public struct Torus : IShape
    {
        // ���������ͷֱ��ʣ����㶥��ͷ���
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution); // ���� 2D ��������

            float r1 = 0.375f; // Բ�����뾶
            float r2 = 0.125f; // Բ���ΰ뾶
            float4 s = r1 + r2 * cos(2f * PI * uv.c1); // ����Բ����״�İ뾶

            Point4 p; // �洢���ɵĶ���ͷ���
            p.positions.c0 = s * sin(2f * PI * uv.c0); // ���㶥��λ��
            p.positions.c1 = r2 * sin(2f * PI * uv.c1);
            p.positions.c2 = s * cos(2f * PI * uv.c0);
            p.normals = p.positions; // ��������Ϊ����λ��
            p.normals.c0 -= r1 * sin(2f * PI * uv.c0); // �������ߣ�ʹ�䴹ֱ�ڱ���
            p.normals.c2 -= r1 * cos(2f * PI * uv.c0);
            return p;
        }
    }

    // ���� Sphere��������״���ṹ�壬ʵ�� IShape �ӿ�
    public struct Sphere : IShape
    {
        // ���������ϵĶ���ͷ���
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution); // ���� 2D ��������

            float r = 0.5f; // ����뾶
            float4 s = r * sin(PI * uv.c1); // ����������״�İ뾶

            Point4 p; // �洢���ɵĶ���ͷ���
            p.positions.c0 = uv.c0 - 0.5f; // ���㶥��λ��
            p.positions.c1 = uv.c1 - 0.5f;
            p.positions.c2 = 0.5f - abs(p.positions.c0) - abs(p.positions.c1);
            float4 offset = max(-p.positions.c2, 0f);
            p.positions.c0 += select(-offset, offset, p.positions.c0 < 0f);
            p.positions.c1 += select(-offset, offset, p.positions.c1 < 0f);
            float4 scale = 0.5f * rsqrt(
                p.positions.c0 * p.positions.c0 +
                p.positions.c1 * p.positions.c1 +
                p.positions.c2 * p.positions.c2
            );
            p.positions.c0 *= scale; // ��һ������λ��
            p.positions.c1 *= scale;
            p.positions.c2 *= scale;
            p.normals = p.positions; // ��������Ϊ��һ����Ķ���λ��
            return p;
        }
    }

    // ������ת��Ϊ UV ���꣬���ڼ��㶥��λ��
    public static float4x2 IndexTo4UV(int i, float resolution, float invResolution)
    {
        float4x2 uv;
        float4 i4 = 4f * i + float4(0f, 1f, 2f, 3f);
        uv.c1 = floor(invResolution * i4 + 0.00001f); // ������������
        uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f); // ����С������
        uv.c1 = invResolution * (uv.c1 + 0.5f); // ���� y ����
        return uv;
    }

    // ����һ���洢����ͷ��ߵĽṹ��
    public struct Point4
    {
        public float4x3 positions, normals;
    }

    // ����һ���ӿ� IShape��Ҫ��ʵ����״�Ķ���ͷ�������
    public interface IShape
    {
        Point4 GetPoint4(int i, float resolution, float invResolution);
    }

    // ���� Plane��ƽ����״���ṹ�壬ʵ�� IShape �ӿ�
    public struct Plane : IShape
    {
        // ����ƽ���ϵĶ���ͷ���
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution); // ���� UV ����
            return new Point4
            {
                positions = float4x3(uv.c0 - 0.5f, 0f, uv.c1 - 0.5f), // ����ƽ��Ķ���
                normals = float4x3(0f, 1f, 0f) // ����ָ�� y ��������
            };
        }
    }

    // ����һ������ Job�����ڲ��м�����״�Ķ���ͷ��ߣ�S ����ʵ�� IShape �ӿ�
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<S> : IJobFor where S : struct, IShape
    {
        [WriteOnly]
        NativeArray<float3x4> positions, normals; // ԭ�����飬���ڴ洢����ͷ���

        public float resolution, invResolution; // �ֱ��ʺͷֱ��ʵĵ���
        public float3x4 positionTRS, normalTRS; // λ�úͷ��ߵı任����

        // ִ�����񣬼���ÿ�������ʵ��λ�úͷ���
        public void Execute(int i)
        {
            Point4 p = default(S).GetPoint4(i, resolution, invResolution); // ��ȡ����ͷ���

            positions[i] = transpose(positionTRS.TransformVectors(p.positions)); // �任����λ��
            float3x4 n = transpose(normalTRS.TransformVectors(p.normals, 0f)); // �任����
            normals[i] = float3x4(
                normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3) // ��һ������
            );
        }

        // ���Ȳ�������������״�Ķ���ͷ���
        public static JobHandle ScheduleParallel(
           NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution,
            float4x4 trs, JobHandle dependency
       ) => new Job<S>
       {
           positions = positions,
           normals = normals,
           resolution = resolution,
           invResolution = 1f / resolution,
           positionTRS = trs.Get3x4(),
           normalTRS = transpose(inverse(trs)).Get3x4() // ���㷨�ߵı任����
       }.ScheduleParallel(positions.Length, resolution, dependency); // ���е�������
    }
}
