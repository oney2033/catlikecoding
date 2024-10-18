using Unity.Collections;          // ʹ�� NativeArray ��ԭ�����ݽṹ
using Unity.Jobs;                 // ʹ�� Job System ���ж��̵߳���
using Unity.Mathematics;           // ʹ�� Unity Mathematics ����������ѧ����
using UnityEngine;                // Unity �ĺ��Ŀ�
using static Unity.Mathematics.math; // ��̬���� math �࣬������� math ����

public abstract class Visualization : MonoBehaviour
{
    // ���󷽷������ÿ��ӻ������������ݳ��ȺͲ������Կ�
    protected abstract void EnableVisualization(
         int dataLength, MaterialPropertyBlock propertyBlock
     );

    // ���󷽷������ÿ��ӻ�
    protected abstract void DisableVisualization();

    // ���󷽷������¿��ӻ���������λ�����顢�ֱ��ʺ���ҵ���
    protected abstract void UpdateVisualization(
    NativeArray<float3x4> positions, int resolution, JobHandle handle
);

    // ö�٣�����������״
    public enum Shape { Plane, Sphere, Torus }

    // ������ÿ����״����ҵ���ȷ���
    static Shapes.ScheduleDelegate[] shapeJobs = {
        Shapes.Job<Shapes.Plane>.ScheduleParallel,
        Shapes.Job<Shapes.Sphere>.ScheduleParallel,
        Shapes.Job<Shapes.Torus>.ScheduleParallel
    };

    [SerializeField]
    Shape shape;  // ����ѡ����ӻ���״

    // Shader ���� ID�������� Shader �����û����������ò���
    static int
        positionsId = Shader.PropertyToID("_Positions"), // λ������ ID
        normalsId = Shader.PropertyToID("_Normals"),     // �������� ID
        configId = Shader.PropertyToID("_Config");       // �������� ID

    [SerializeField]  // ʹ����Щ�ֶ��� Unity Inspector �пɼ�
    Mesh instanceMesh; // ����ʵ����������mesh��

    [SerializeField]
    Material material; // ʹ�õĲ���

    [SerializeField, Range(1, 512)] // ���Ʒֱ��ʵķ�ΧΪ 1 �� 512
    int resolution = 16;  // ʵ����������ֱ���

    [SerializeField, Range(-0.5f, 0.5f)]
    float displacement = 0.1f; // ��ֱƫ����

    NativeArray<float3x4> positions, normals;  // �洢λ����Ϣ�ͷ��ߵ�ԭ������

    ComputeBuffer positionsBuffer, normalsBuffer; // ���� GPU ����Ļ�����

    MaterialPropertyBlock propertyBlock; // �洢ʵ�������ʵ�����

    Bounds bounds;  // ȷ����������ı߽��
    bool isDirty;   // ����Ƿ���Ҫ����

    [SerializeField, Range(0.1f, 10f)]
    float instanceScale = 2f;  // ʵ������������ű���

    // ���ű�����ʱ����
    void OnEnable()
    {
        isDirty = true;  // �����Ҫ����
        int length = resolution * resolution;  // ������ʵ����
        length = length / 4 + (length & 1); // ȷ������Ϊ 4 �ı������Ż�����

        // ���� NativeArray ���ڴ洢λ����Ϣ�ͷ�����Ϣ
        positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        normals = new NativeArray<float3x4>(length, Allocator.Persistent);

        // ���� GPU ����� ComputeBuffer�����ڴ洢λ�úͷ�������
        positionsBuffer = new ComputeBuffer(length * 4, 3 * 4); // ÿ��λ���� 3 �� float4
        normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

        // ��ʼ�� MaterialPropertyBlock
        propertyBlock ??= new MaterialPropertyBlock();
        EnableVisualization(length, propertyBlock);  // ���ÿ��ӻ�

        // �����������ݰ󶨵� Shader ��������
        propertyBlock.SetBuffer(positionsId, positionsBuffer);
        propertyBlock.SetBuffer(normalsId, normalsBuffer);

        // ���� Shader �е����������������ֱ��ʡ����š�λ�ƵȲ���
        propertyBlock.SetVector(configId, new Vector4(
            resolution, instanceScale / resolution, displacement
        ));
    }

    // ���ű�����ʱ����
    void OnDisable()
    {
        // �ͷ� NativeArray �� ComputeBuffer ռ�õ��ڴ�
        positions.Dispose();
        normals.Dispose();
        positionsBuffer.Release();
        normalsBuffer.Release();
        positionsBuffer = null;
        normalsBuffer = null;
        DisableVisualization();  // ���ÿ��ӻ�
    }

    // �� Inspector �еĲ��������仯ʱ����
    void OnValidate()
    {
        // ����������Ѵ����ҽű������ã����³�ʼ��������
        if (positionsBuffer != null && enabled)
        {
            OnDisable();  // ���õ�ǰ��Դ
            OnEnable();   // ��������
        }
    }

    // ÿ֡����ʱ����
    void Update()
    {
        // �����Ҫ���»��߱任�����˱仯
        if (isDirty || transform.hasChanged)
        {
            isDirty = false;  // ���ø��±��
            transform.hasChanged = false;  // ���ñ任���

            // ������״��ҵ���ȷ��������¿��ӻ�����
            UpdateVisualization(
                positions, resolution,
                shapeJobs[(int)shape]( // ����ѡ�����״������ҵ����
                    positions, normals, resolution, transform.localToWorldMatrix, default
                )
            );

            // ������õ���λ����Ϣ�ͷ������ݴ��ݸ� ComputeBuffer
            positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
            normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));

            // ������Ƶı߽��
            bounds = new Bounds(
                transform.position,
                float3(2f * cmax(abs(transform.lossyScale)) + displacement)
            );
        }

        // ʹ�� Graphics.DrawMeshInstancedProcedural ������ʵ����������
        Graphics.DrawMeshInstancedProcedural(
            instanceMesh, 0, material, bounds,
            resolution * resolution, propertyBlock
        );
    }
}
