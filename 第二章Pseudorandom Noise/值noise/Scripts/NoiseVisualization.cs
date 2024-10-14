using Unity.Collections;          // ʹ�� NativeArray ��ԭ�����ݽṹ
using Unity.Jobs;                 // ʹ�� Job System ���ж��̵߳���
using Unity.Mathematics;           // ʹ�� Unity Mathematics ����������ѧ����
using UnityEngine;                // Unity �ĺ��Ŀ�
using static Noise;

public class NoiseVisualization : Visualization
{
    // Shader ���Ե� ID����������ɫ�������ö�Ӧ�Ļ�����������
    static int noiseId = Shader.PropertyToID("_Noise"); 

    [SerializeField]
    int seed; // ��ϣ���������ӣ��������ɲ�ͬ�����ֵ

    NativeArray<float4> noise;
    ComputeBuffer noiseBuffer;

    [SerializeField]
    SpaceTRS domain = new SpaceTRS
    {
        scale = 8f
    };

    // ���ű�����ʱ����
    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
    {
        noise = new NativeArray<float4>(dataLength, Allocator.Persistent);
        noiseBuffer = new ComputeBuffer(dataLength * 4, 4);
        propertyBlock.SetBuffer(noiseId, noiseBuffer);
    }

    // ���ű�����ʱ����
    protected override void DisableVisualization()
    {
        noise.Dispose();
        noiseBuffer.Release();
        noiseBuffer = null;
    }

    static ScheduleDelegate[] noiseJobs = {
        Job<Lattice1D>.ScheduleParallel,
        Job<Lattice2D>.ScheduleParallel,
        Job<Lattice3D>.ScheduleParallel
    };

    [SerializeField, Range(1, 3)]
    int dimensions = 3;

    // ÿ֡����ʱ����
    protected override void UpdateVisualization(
        NativeArray<float3x4> positions, int resolution, JobHandle handle
    )
    {

        noiseJobs[dimensions - 1](
                positions, noise, seed, domain, resolution, handle
            ).Complete();
        noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
    }
}
