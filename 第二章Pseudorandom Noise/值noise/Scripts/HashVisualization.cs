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
        [ReadOnly]
        public NativeArray<float3x4> positions;

        [WriteOnly]  // ֻд�������ȡ�������Ż�����
        public NativeArray<uint4> hashes; // ���ڴ洢������Ĺ�ϣֵ
        public SmallXXHash4 hash; // ʹ�� SmallXXHash ��Ϊ��ϣ����
        public float3x4 domainTRS;
        float4x3 TransformPositions(float3x4 trs, float4x3 p) => float4x3(
            trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x,
            trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y,
            trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z
        );

        public void Execute(int i)
        {
            float4x3 p = TransformPositions(domainTRS, transpose(positions[i]));

            int4 u = (int4)floor(p.c0);
            int4 v = (int4)floor(p.c1);
            int4 w = (int4)floor(p.c2);

            hashes[i] = hash.Eat(u).Eat(v).Eat(w);
        }
    }

    public enum Shape { Plane, Sphere, Torus }

    static Shapes.ScheduleDelegate[] shapeJobs = {
        Shapes.Job<Shapes.Plane>.ScheduleParallel,
        Shapes.Job<Shapes.Sphere>.ScheduleParallel,
        Shapes.Job<Shapes.Torus>.ScheduleParallel
    };

    [SerializeField]
    Shape shape;

    // Shader ���Ե� ID����������ɫ�������ö�Ӧ�Ļ�����������
    static int
        hashesId = Shader.PropertyToID("_Hashes"),  // ��ϣֵ�������� Shader ���� ID
        positionsId = Shader.PropertyToID("_Positions"),
        normalsId = Shader.PropertyToID("_Normals"),
        configId = Shader.PropertyToID("_Config");  // ���������� Shader ���� ID

    [SerializeField]  // ʹ�� [SerializeField] ʹ����Щ�ֶο����� Unity Inspector ������
    Mesh instanceMesh; // ����ʵ������ Mesh

    [SerializeField]
    Material material; // ʹ�õĲ���

    [SerializeField, Range(1, 512)] // ���Ʒֱ��ʵķ�ΧΪ 1 �� 512
    int resolution = 16; // ʵ����������ֱ���

    [SerializeField]
    int seed; // ��ϣ���������ӣ��������ɲ�ͬ�����ֵ

    [SerializeField, Range(-0.5f, 0.5f)]
    float displacement = 0.1f;

    NativeArray<uint4> hashes; // ���ڴ洢ÿ��ʵ���Ĺ�ϣֵ
    NativeArray<float3x4> positions, normals;

    ComputeBuffer hashesBuffer, positionsBuffer, normalsBuffer; // ComputeBuffer ���ڴ������ݵ� GPU

    MaterialPropertyBlock propertyBlock; // �����ڻ���ʱ����ʵ������������

    [SerializeField]
    SpaceTRS domain = new SpaceTRS
    {
        scale = 8f
    };
    Bounds bounds;
    bool isDirty;

    [SerializeField, Range(0.1f, 10f)]
    float instanceScale = 2f;

    // ���ű�����ʱ����
    void OnEnable()
    {
        isDirty = true;
        // ������ʵ�������ֱ��� * �ֱ��� = ʵ����
        int length = resolution * resolution;

        // ���� NativeArray�����ڴ洢��ϣֵ�������ڳ־û��ڴ���
        length = length / 4 + (length & 1);
        hashes = new NativeArray<uint4>(length, Allocator.Persistent);
        positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        normals = new NativeArray<float3x4>(length, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(length * 4, 4);
        positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);
        normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

        // ��� propertyBlock Ϊ null ���ʼ��
        propertyBlock ??= new MaterialPropertyBlock();

        // ����ϣ�������󶨵�����������
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
        propertyBlock.SetBuffer(positionsId, positionsBuffer);
        propertyBlock.SetBuffer(normalsId, normalsBuffer);

        // �������������������ֱ��ʡ���ֱƫ�ƵȲ���
        propertyBlock.SetVector(configId, new Vector4(
            resolution, instanceScale / resolution, displacement
        ));
    }

    // ���ű�����ʱ����
    void OnDisable()
    {
        // �ͷ� NativeArray �� ComputeBuffer ռ�õ��ڴ�
        hashes.Dispose();
        positions.Dispose();
        normals.Dispose();
        hashesBuffer.Release();
        positionsBuffer.Release();
        normalsBuffer.Release();
        hashesBuffer = null;
        positionsBuffer = null;
        normalsBuffer = null;
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
        if (isDirty || transform.hasChanged)
        {
            isDirty = false;
            transform.hasChanged = false;

            JobHandle handle = shapeJobs[(int)shape](
                positions, normals, resolution, transform.localToWorldMatrix, default
            );

            new HashJob
            {
                positions = positions,
                hashes = hashes,
                hash = SmallXXHash.Seed(seed),
                domainTRS = domain.Matrix
            }.ScheduleParallel(hashes.Length, resolution, handle).Complete();

            hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
            positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
            normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));
            bounds = new Bounds(
                transform.position,
                float3(2f * cmax(abs(transform.lossyScale)) + displacement)
            );
        }
        // ʹ��ʵ������������ƣ��������Կ��е����ݴ��ݸ� Shader
        Graphics.DrawMeshInstancedProcedural(
            instanceMesh, 0, material, bounds,
            resolution * resolution, propertyBlock
        );
    }
}
