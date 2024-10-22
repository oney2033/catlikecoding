using Unity.Mathematics; // 引用 Unity 数学库，用于处理数学计算
using UnityEngine; // 引用 Unity 引擎的核心库

namespace ProceduralMeshes // 定义一个命名空间，组织与程序化网格相关的代码
{

    // 定义了一个 IMeshStreams 接口，用于处理网格数据流
    public interface IMeshStreams
    {
        // 用于设置网格的基本数据结构
        // meshData: 需要初始化的网格数据
        // bounds: 网格的边界框，用于定义网格的包围范围
        // vertexCount: 顶点的数量
        // indexCount: 三角形索引的数量
        void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount);

        // 设置指定索引处的顶点数据
        // index: 顶点的索引
        // data: 顶点的具体数据
        void SetVertex(int index, Vertex data);

        // 设置三角形数据
        // index: 三角形的索引
        // triangle: 由三个顶点组成的三角形 (int3 表示3个整数顶点的索引)
        void SetTriangle(int index, int3 triangle);
    }

}
