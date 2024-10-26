using ProceduralMeshes; // ���� ProceduralMeshes �����ռ�
using ProceduralMeshes.Generators; // ������������������ռ�
using ProceduralMeshes.Streams; // ������������������ռ�
using UnityEngine; // ���� Unity ���������ռ�
using UnityEngine.Rendering; // ������Ⱦ��������ռ�

// ȷ������������� MeshFilter �� MeshRenderer ���
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour
{
    // ���� Inspector �е����ķֱ��ʣ���ΧΪ 1 �� 10
    [SerializeField, Range(1, 50)]
    int resolution = 1;

    Mesh mesh; // ���ڴ洢�������ݵ� Mesh ����

    [System.NonSerialized]
    Vector3[] vertices, normals; // �ֱ�洢����Ķ���ͷ�������
    [System.NonSerialized]
    Vector4[] tangents; // ���ڴ洢�������������

    // ����һ��ö������ GizmoMode��������������Щ Gizmos Ҫ��ʾ
    [System.Flags]
    public enum GizmoMode
    {
        Nothing = 0, // ����ʾ�κ� Gizmo
        Vertices = 1, // ��ʾ����
        Normals = 0b10, // ��ʾ����
        Tangents = 0b100, // ��ʾ����
        Triangles = 0b1000
    }

    [SerializeField] // ʹ gizmos �� Unity Inspector �пɱ༭
    GizmoMode gizmos;

    public enum MaterialMode { Flat, Ripple, LatLonMap, CubeMap }

    [SerializeField]
    MaterialMode material;

    [SerializeField]
    Material[] materials;

    [System.NonSerialized]
    int[] triangles;

    [System.Flags]
    public enum MeshOptimizationMode
    {
        Nothing = 0, ReorderIndices = 1, ReorderVertices = 0b10
    }

    [SerializeField]
    MeshOptimizationMode meshOptimization;

    void Awake()
    {
        // ����һ���µ� Mesh ���󣬲�����һ������ "Procedural Mesh"
        mesh = new Mesh
        {
            name = "Procedural Mesh"
        };
        // �������� Mesh ��ֵ����ǰ GameObject �� MeshFilter ����������ڳ�������ʾ����
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // ���� Inspector �жԽű������޸�ʱ���ø÷�����ȷ���ű�������
    void OnValidate() => enabled = true;

    void Update()
    {
        // ÿ֡������������ĺ���
        GenerateMesh();
        // �������������ýű����Ա����ظ�����
        enabled = false;
        // �ͷ�������ص����ݣ�����ռ���ڴ�
        vertices = null;
        normals = null;
        tangents = null;
        triangles = null;
        GetComponent<MeshRenderer>().material = materials[(int)material];
    }

    // ʹ�� Gizmos ��������Ķ��㡢���ߺ�����
    void OnDrawGizmos()
    {
        // ���û������Ҫ���Ƶ����ݻ�û���������ݣ�ֱ�ӷ���
        if (gizmos == GizmoMode.Nothing || mesh == null)
        {
            return;
        }

        // ���� gizmos ö��ֵ�ж��Ƿ���ƶ��㡢���ߺ�����
        bool drawVertices = (gizmos & GizmoMode.Vertices) != 0; // �ж��Ƿ���ƶ���
        bool drawNormals = (gizmos & GizmoMode.Normals) != 0;   // �ж��Ƿ���Ʒ���
        bool drawTangents = (gizmos & GizmoMode.Tangents) != 0; // �ж��Ƿ��������
        bool drawTriangles = (gizmos & GizmoMode.Triangles) != 0;

        // �����������Ϊ null����������л�ȡ��������
        if (vertices == null)
        {
            vertices = mesh.vertices;
        }
        // �����Ҫ���Ʒ��߲��ҷ�������Ϊ null����������л�ȡ��������
        if (drawNormals && normals == null)
        {
            drawNormals = mesh.HasVertexAttribute(VertexAttribute.Normal);
            if (drawNormals)
            {
                normals = mesh.normals;
            }
        }
        // �����Ҫ�������߲�����������Ϊ null����������л�ȡ��������
        if (drawTangents && tangents == null)
        {
            drawTangents = mesh.HasVertexAttribute(VertexAttribute.Tangent);
            if (drawTangents)
            {
                tangents = mesh.tangents;
            }
        }
        if (drawTriangles && triangles == null)
        {
            triangles = mesh.triangles;
        }

        // ��ȡ��ǰ GameObject �ı任���
        Transform t = transform;
        // �������ж���
        for (int i = 0; i < vertices.Length; i++)
        {
            // ����������ת������������ϵ��
            Vector3 position = t.TransformPoint(vertices[i]);

            // �����Ҫ���ƶ��㣬��ʹ����ɫ�����ʾ����λ��
            if (drawVertices)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(position, 0.02f); // ��һ���뾶Ϊ 0.02 ��С��
            }
            // �����Ҫ���Ʒ��ߣ���ʹ����ɫ��ͷ��ʾ���߷���
            if (drawNormals)
            {
                Gizmos.color = Color.green;
                // �Ӷ���λ�û�����������������Ϊ 0.2
                Gizmos.DrawRay(position, t.TransformDirection(normals[i]) * 0.2f);
            }
            // �����Ҫ�������ߣ���ʹ�ú�ɫ��ͷ��ʾ���߷���
            if (drawTangents)
            {
                Gizmos.color = Color.red;
                // �Ӷ���λ�û�����������������Ϊ 0.2
                Gizmos.DrawRay(position, t.TransformDirection(tangents[i]) * 0.2f);
            }
        }

        if (drawTriangles)
        {
            float colorStep = 1f / (triangles.Length - 3);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                float c = i * colorStep;
                Gizmos.color = new Color(c, 0f, c);
                Gizmos.DrawSphere(
                    t.TransformPoint((
                        vertices[triangles[i]] +
                        vertices[triangles[i + 1]] +
                        vertices[triangles[i + 2]]
                    ) * (1f / 3f)),
                    0.02f
                );
            }
        }
    }


    void GenerateMesh()
    {
        // ����һ����д��������������
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0]; // ��ȡ��һ�� MeshData

        // ���� MeshJob �����е�����������
        jobs[(int)meshType](mesh, meshData, resolution, default).Complete(); // �ȴ���ҵ���

        // Ӧ�ò��ͷſ�д�������ݣ����� Mesh
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        if (meshOptimization == MeshOptimizationMode.ReorderIndices)
        {
            mesh.OptimizeIndexBuffers();
        }
        else if (meshOptimization == MeshOptimizationMode.ReorderVertices)
        {
            mesh.OptimizeReorderVertexBuffer();
        }
        else if (meshOptimization != MeshOptimizationMode.Nothing)
        {
            mesh.Optimize();
        }
    }

    static MeshJobScheduleDelegate[] jobs = {
        MeshJob<SquareGrid, SingleStream>.ScheduleParallel,
        MeshJob<SharedSquareGrid, SingleStream>.ScheduleParallel,
        MeshJob<SharedTriangleGrid, SingleStream>.ScheduleParallel,
        MeshJob<PointyHexagonGrid, SingleStream>.ScheduleParallel,
        MeshJob<FlatHexagonGrid, SingleStream>.ScheduleParallel,
        MeshJob<CubeSphere, SingleStream>.ScheduleParallel,
        MeshJob<SharedCubeSphere, PositionStream>.ScheduleParallel,
        MeshJob<UVSphere, SingleStream>.ScheduleParallel
    };

    public enum MeshType
    {
        SquareGrid, SharedSquareGrid, SharedTriangleGrid, 
        PointyHexagonGrid, FlatHexagonGrid, CubeSphere, SharedCubeSphere, UVSphere
    };

    [SerializeField]
    MeshType meshType;
}
