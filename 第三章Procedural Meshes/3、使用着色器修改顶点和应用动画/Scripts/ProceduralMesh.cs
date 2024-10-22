using ProceduralMeshes; // 引入 ProceduralMeshes 命名空间
using ProceduralMeshes.Generators; // 引入生成器相关命名空间
using ProceduralMeshes.Streams; // 引入数据流相关命名空间
using UnityEngine; // 引入 Unity 引擎命名空间
using UnityEngine.Rendering; // 引入渲染相关命名空间

// 确保该组件附加了 MeshFilter 和 MeshRenderer 组件
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour
{
    // 可在 Inspector 中调整的分辨率，范围为 1 到 10
    [SerializeField, Range(1, 50)]
    int resolution = 1;

    Mesh mesh; // 用于存储网格数据

    void Awake()
    {
        // 创建新的 Mesh 实例并设置名称
        mesh = new Mesh
        {
            name = "Procedural Mesh"
        };
        // 将创建的 Mesh 赋值给 MeshFilter 组件
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // 在 Inspector 中修改该脚本时调用，启用该脚本
    void OnValidate() => enabled = true;

    void Update()
    {
        // 每帧生成网格
        GenerateMesh();
        enabled = false; // 生成后禁用该脚本以防止多次生成
    }

    void GenerateMesh()
    {
        // 分配一个可写的网格数据数组
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0]; // 获取第一个 MeshData

        // 调用 MeshJob 来并行调度生成网格
        jobs[(int)meshType](mesh, meshData, resolution, default).Complete(); // 等待作业完成

        // 应用并释放可写网格数据，更新 Mesh
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
