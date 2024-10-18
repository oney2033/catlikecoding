using Unity.Burst;                // ʹ�� Burst ���������Ż�����
using Unity.Collections;          // ʹ�� NativeArray ��ԭ�����ݽṹ
using Unity.Jobs;                 // ʹ�� Job System ���ж��̵߳���
using Unity.Mathematics;           // ʹ�� Unity Mathematics ����������ѧ����
using UnityEngine;                // Unity �ĺ��Ŀ�
using static Unity.Mathematics.math; // ��̬���� math �࣬������� math ����

public class HashVisualization : MonoBehaviour
{
    // BurstCompile ������������ Burst ����������� Job ��ִ������
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor  // IJobFor �� Unity Job System �Ľӿڣ�֧�ֲ��е���
    {
        [WriteOnly]  // ֻд�������ȡ�������Ż�����
        public NativeArray<uint> hashes; // ���ڴ洢������Ĺ�ϣֵ
        public int resolution; // �ֱ���
        public float invResolution; // �ֱ��ʵĵ��������ڼ���λ��
        public SmallXXHash hash; // ʹ�� SmallXXHash ��Ϊ��ϣ����

        public void Execute(int i)
        {
            // �������������ά�����е� u �� v ����
            int v = (int)floor(invResolution * i + 0.00001f);
            int u = i - resolution * v - resolution / 2;
            v -= resolution / 2;

            // ʹ�ù�ϣ������ u �� v ���� "����" (Eat)������Ψһ�Ĺ�ϣֵ
            hashes[i] = hash.Eat(u).Eat(v);
        }
    }

    // Shader ���Ե� ID����������ɫ�������ö�Ӧ�Ļ�����������
    static int
        hashesId = Shader.PropertyToID("_Hashes"),  // ��ϣֵ�������� Shader ���� ID
        configId = Shader.PropertyToID("_Config");  // ���������� Shader ���� ID

    [SerializeField]  // ʹ�� [SerializeField] ʹ����Щ�ֶο����� Unity Inspector ������
    Mesh instanceMesh; // ����ʵ������ Mesh

    [SerializeField]
    Material material; // ʹ�õĲ���

    [SerializeField, Range(1, 512)] // ���Ʒֱ��ʵķ�ΧΪ 1 �� 512
    int resolution = 16; // ʵ����������ֱ���

    [SerializeField]
    int seed; // ��ϣ���������ӣ��������ɲ�ͬ�����ֵ

    [SerializeField, Range(-2f, 2f)] // ���ƴ�ֱƫ�Ƶķ�ΧΪ -2 �� 2
    float verticalOffset = 1f; // ʵ���Ĵ�ֱƫ����

    NativeArray<uint> hashes; // ���ڴ洢ÿ��ʵ���Ĺ�ϣֵ

    ComputeBuffer hashesBuffer; // ComputeBuffer ���ڴ������ݵ� GPU

    MaterialPropertyBlock propertyBlock; // �����ڻ���ʱ����ʵ������������

    // ���ű�����ʱ����
    void OnEnable()
    {
        // ������ʵ�������ֱ��� * �ֱ��� = ʵ����
        int length = resolution * resolution;

        // ���� NativeArray�����ڴ洢��ϣֵ�������ڳ־û��ڴ���
        hashes = new NativeArray<uint>(length, Allocator.Persistent);

        // ����һ�� ComputeBuffer����СΪ length��ÿ��Ԫ��Ϊ 4 �ֽڣ�uint��
        hashesBuffer = new ComputeBuffer(length, 4);

        // ���� HashJob �����������
        new HashJob
        {
            hashes = hashes, // ����ϣ���鴫�ݸ� Job
            resolution = resolution, // ���÷ֱ���
            invResolution = 1f / resolution, // ����ֱ��ʵĵ���������λ�ü���
            hash = SmallXXHash.Seed(seed) // ���ɹ�ϣ����
        }.ScheduleParallel(hashes.Length, resolution, default).Complete();

        // ��������Ĺ�ϣֵ���ݴ��ݵ� ComputeBuffer
        hashesBuffer.SetData(hashes);

        // ��� propertyBlock Ϊ null ���ʼ��
        propertyBlock ??= new MaterialPropertyBlock();

        // ����ϣ�������󶨵�����������
        propertyBlock.SetBuffer(hashesId, hashesBuffer);

        // �������������������ֱ��ʡ���ֱƫ�ƵȲ���
        propertyBlock.SetVector(configId, new Vector4(
            resolution, 1f / resolution, verticalOffset / resolution
        ));
    }

    // ���ű�����ʱ����
    void OnDisable()
    {
        // �ͷ� NativeArray �� ComputeBuffer ռ�õ��ڴ�
        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null;
    }

    // �� Inspector �еĲ��������仯ʱ����
    void OnValidate()
    {
        // ��������������ҽű����ã������³�ʼ��
        if (hashesBuffer != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    // ÿ֡����ʱ����
    void Update()
    {
        // ʹ��ʵ������������ƣ��������Կ��е����ݴ��ݸ� Shader
        Graphics.DrawMeshInstancedProcedural(
            instanceMesh, 0, material, new Bounds(Vector3.zero, Vector3.one),
            hashes.Length, propertyBlock
        );
    }
}
