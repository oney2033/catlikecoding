using Unity.Burst;                // ʹ�� Burst ���������Ż�����
using Unity.Collections;          // ʹ�� NativeArray ��ԭ�����ݽṹ
using Unity.Jobs;                 // ʹ�� Job System ���ж��̵߳���
using Unity.Mathematics;           // ʹ�� Unity Mathematics ����������ѧ����
using UnityEngine;                // Unity �ĺ��Ŀ�
using static Unity.Mathematics.math; // ��̬���� math �࣬������� math ����

public class HashVisualization : Visualization
{

    // BurstCompile ������������ Burst ����������� Job ��ִ������
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor  // IJobFor �� Unity Job System �Ľӿڣ�֧�ֲ��е���
    {
        [ReadOnly]
        public NativeArray<float3x4> positions;

        [WriteOnly]  // ֻд�������ȡ�������Ż�����
        public NativeArray<uint4> hashes; // ���ڴ洢������Ĺ�ϣֵ
        public SmallXXHash4 hash; // ʹ�� SmallXXHash ��Ϊ��ϣ����
        public float3x4 domainTRS;
     
        public void Execute(int i)
        {
            float4x3 p = domainTRS.TransformVectors(transpose(positions[i]));
            int4 u = (int4)floor(p.c0);
            int4 v = (int4)floor(p.c1);
            int4 w = (int4)floor(p.c2);

            hashes[i] = hash.Eat(u).Eat(v).Eat(w);
        }
    }


    // Shader ���Ե� ID����������ɫ�������ö�Ӧ�Ļ�����������
    static int
        hashesId = Shader.PropertyToID("_Hashes");  // ��ϣֵ�������� Shader ���� ID
      

    [SerializeField]
    int seed; // ��ϣ���������ӣ��������ɲ�ͬ�����ֵ

    NativeArray<uint4> hashes; // ���ڴ洢ÿ��ʵ���Ĺ�ϣֵ

    ComputeBuffer hashesBuffer;

    [SerializeField]
    SpaceTRS domain = new SpaceTRS
    {
        scale = 8f
    };

    // ���ű�����ʱ����
    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
    {
        hashes = new NativeArray<uint4>(dataLength, Allocator.Persistent);
       // positions = new NativeArray<float3x4>(length, Allocator.Persistent);
      //  normals = new NativeArray<float3x4>(length, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(dataLength * 4, 4);
       
        // ����ϣ�������󶨵�����������
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
    }

    // ���ű�����ʱ����
    protected override void DisableVisualization()
    {
        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null;
    }

    // ÿ֡����ʱ����
    protected override void UpdateVisualization(
         NativeArray<float3x4> positions, int resolution, JobHandle handle
     )
    {
        new HashJob
        {
            positions = positions,
            hashes = hashes,
            hash = SmallXXHash.Seed(seed),
            domainTRS = domain.Matrix
        }.ScheduleParallel(hashes.Length, resolution, handle).Complete();
        
        hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
    }
}
