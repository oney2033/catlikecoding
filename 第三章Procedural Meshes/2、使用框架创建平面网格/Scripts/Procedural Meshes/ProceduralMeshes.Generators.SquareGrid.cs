using Unity.Mathematics; // 引入数学相关的库
using UnityEngine; // 引入Unity引擎的库

using static Unity.Mathematics.math; // 导入数学库中的静态函数

namespace ProceduralMeshes.Generators // 命名空间，表示生成程序网格的模块
{
    // 定义方形网格生成器，实现IMeshGenerator接口
    public struct SquareGrid : IMeshGenerator
    {
        // 计算顶点总数：每个网格有4个顶点，分辨率为Resolution
        public int VertexCount => 4 * Resolution * Resolution;
        // 计算索引总数：每个网格有6个索引（两个三角形），分辨率为Resolution
        public int IndexCount => 6 * Resolution * Resolution;
        // 作业长度，表示要处理的行数
        public int JobLength => Resolution;
        // 网格的边界，中心在(0,0,0)，宽度和深度为1
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));
        public int Resolution { get; set; } // 网格的分辨率

        // 执行网格生成的逻辑
        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            // 计算当前行的顶点索引和三角形索引
            int vi = 4 * Resolution * z; // 顶点索引
            int ti = 2 * Resolution * z; // 三角形索引

            // 遍历当前行中的每一列
            for (int x = 0; x < Resolution; x++, vi += 4, ti += 2)
            {
                // 计算当前网格的x和z坐标
                var xCoordinates = float2(x, x + 1f) / Resolution - 0.5f; // x坐标
                var zCoordinates = float2(z, z + 1f) / Resolution - 0.5f; // z坐标

                // 创建一个顶点实例
                var vertex = new Vertex();
                vertex.normal.y = 1f; // 法线向上
                vertex.tangent.xw = float2(1f, -1f); // 切线的x和w分量
                vertex.position.x = xCoordinates.x; // 设置顶点位置x
                vertex.position.z = zCoordinates.x; // 设置顶点位置z
                streams.SetVertex(vi + 0, vertex); // 存储第一个顶点

                vertex.position.x = xCoordinates.y; // 设置下一个顶点位置x
                vertex.texCoord0 = float2(1f, 0f); // 设置纹理坐标
                streams.SetVertex(vi + 1, vertex); // 存储第二个顶点

                vertex.position.x = xCoordinates.x; // 设置下一个顶点位置x
                vertex.position.z = zCoordinates.y; // 设置顶点位置z
                vertex.texCoord0 = float2(0f, 1f); // 设置纹理坐标
                streams.SetVertex(vi + 2, vertex); // 存储第三个顶点

                vertex.position.x = xCoordinates.y; // 设置下一个顶点位置x
                vertex.texCoord0 = 1f; // 设置纹理坐标
                streams.SetVertex(vi + 3, vertex); // 存储第四个顶点

                // 设置两个三角形的索引
                streams.SetTriangle(ti + 0, vi + int3(0, 2, 1)); // 第一个三角形
                streams.SetTriangle(ti + 1, vi + int3(1, 2, 3)); // 第二个三角形
            }
        }
    }
}
