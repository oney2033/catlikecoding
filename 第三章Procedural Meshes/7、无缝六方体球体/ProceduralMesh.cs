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

    Mesh mesh; // 用于存储网格数据的 Mesh 对象

    [System.NonSerialized]
    Vector3[] vertices, normals; // 分别存储网格的顶点和法线数据
    [System.NonSerialized]
    Vector4[] tangents; // 用于存储网格的切线数据

    // 定义一个枚举类型 GizmoMode，它用来控制哪些 Gizmos 要显示
    [System.Flags]
    public enum GizmoMode
    {
        Nothing = 0, // 不显示任何 Gizmo
        Vertices = 1, // 显示顶点
        Normals = 0b10, // 显示法线
        Tangents = 0b100, // 显示切线
        Triangles = 0b1000
    }

    [SerializeField] // 使 gizmos 在 Unity Inspector 中可编辑
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
        // 创建一个新的 Mesh 对象，并给它一个名称 "Procedural Mesh"
        mesh = new Mesh
        {
            name = "Procedural Mesh"
        };
        // 将创建的 Mesh 赋值给当前 GameObject 的 MeshFilter 组件，用于在场景中显示网格
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // 当在 Inspector 中对脚本进行修改时调用该方法，确保脚本被启用
    void OnValidate() => enabled = true;

    void Update()
    {
        // 每帧调用生成网格的函数
        GenerateMesh();
        // 生成完网格后禁用脚本，以避免重复生成
        enabled = false;
        // 释放网格相关的数据，避免占用内存
        vertices = null;
        normals = null;
        tangents = null;
        triangles = null;
        GetComponent<MeshRenderer>().material = materials[(int)material];
    }

    // 使用 Gizmos 绘制网格的顶点、法线和切线
    void OnDrawGizmos()
    {
        // 如果没有设置要绘制的内容或没有网格数据，直接返回
        if (gizmos == GizmoMode.Nothing || mesh == null)
        {
            return;
        }

        // 根据 gizmos 枚举值判断是否绘制顶点、法线和切线
        bool drawVertices = (gizmos & GizmoMode.Vertices) != 0; // 判断是否绘制顶点
        bool drawNormals = (gizmos & GizmoMode.Normals) != 0;   // 判断是否绘制法线
        bool drawTangents = (gizmos & GizmoMode.Tangents) != 0; // 判断是否绘制切线
        bool drawTriangles = (gizmos & GizmoMode.Triangles) != 0;

        // 如果顶点数据为 null，则从网格中获取顶点数据
        if (vertices == null)
        {
            vertices = mesh.vertices;
        }
        // 如果需要绘制法线并且法线数据为 null，则从网格中获取法线数据
        if (drawNormals && normals == null)
        {
            drawNormals = mesh.HasVertexAttribute(VertexAttribute.Normal);
            if (drawNormals)
            {
                normals = mesh.normals;
            }
        }
        // 如果需要绘制切线并且切线数据为 null，则从网格中获取切线数据
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

        // 获取当前 GameObject 的变换组件
        Transform t = transform;
        // 遍历所有顶点
        for (int i = 0; i < vertices.Length; i++)
        {
            // 将顶点坐标转换到世界坐标系中
            Vector3 position = t.TransformPoint(vertices[i]);

            // 如果需要绘制顶点，则使用青色球体表示顶点位置
            if (drawVertices)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(position, 0.02f); // 画一个半径为 0.02 的小球
            }
            // 如果需要绘制法线，则使用绿色箭头表示法线方向
            if (drawNormals)
            {
                Gizmos.color = Color.green;
                // 从顶点位置画出法线向量，长度为 0.2
                Gizmos.DrawRay(position, t.TransformDirection(normals[i]) * 0.2f);
            }
            // 如果需要绘制切线，则使用红色箭头表示切线方向
            if (drawTangents)
            {
                Gizmos.color = Color.red;
                // 从顶点位置画出切线向量，长度为 0.2
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
        // 分配一个可写的网格数据数组
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0]; // 获取第一个 MeshData

        // 调用 MeshJob 来并行调度生成网格
        jobs[(int)meshType](mesh, meshData, resolution, default).Complete(); // 等待作业完成

        // 应用并释放可写网格数据，更新 Mesh
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
