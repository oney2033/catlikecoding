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

    Mesh mesh; // ���ڴ洢��������

    void Awake()
    {
        // �����µ� Mesh ʵ������������
        mesh = new Mesh
        {
            name = "Procedural Mesh"
        };
        // �������� Mesh ��ֵ�� MeshFilter ���
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // �� Inspector ���޸ĸýű�ʱ���ã����øýű�
    void OnValidate() => enabled = true;

    void Update()
    {
        // ÿ֡��������
        GenerateMesh();
        enabled = false; // ���ɺ���øýű��Է�ֹ�������
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
    }

    static MeshJobScheduleDelegate[] jobs = {
        MeshJob<SquareGrid, SingleStream>.ScheduleParallel,
        MeshJob<SharedSquareGrid, SingleStream>.ScheduleParallel
    };

    public enum MeshType
    {
        SquareGrid, SharedSquareGrid
    };

    [SerializeField]
    MeshType meshType;
}
