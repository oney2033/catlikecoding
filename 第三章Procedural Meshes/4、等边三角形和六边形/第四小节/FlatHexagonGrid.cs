using Unity.Mathematics; // 引入 Unity 的数学库，用于数学运算
using UnityEngine; // 引入 Unity 引擎库
using static Unity.Mathematics.math; // 静态导入 math 库中的函数，使其可以直接调用

namespace ProceduralMeshes.Generators // 命名空间，用于区分不同的代码模块
{
    // 定义一个用于生成平面六边形网格的结构体，继承自 IMeshGenerator 接口
    public struct FlatHexagonGrid : IMeshGenerator
    {
        // 顶点数量：每个六边形有7个顶点，顶点总数为 7 * Resolution * Resolution
        public int VertexCount => 7 * Resolution * Resolution;

        // 索引数量：每个六边形有6个三角形（每个三角形有3个索引），总索引数为 18 * Resolution * Resolution
        public int IndexCount => 18 * Resolution * Resolution;

        // 作业长度，表示每一行的任务数量，即 Resolution 行
        public int JobLength => Resolution;

        // 定义网格的边界框，网格位于 (0, 0, 0) 处，尺寸由分辨率决定
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(
                0.75f + 0.25f / Resolution,  // 网格宽度
                0f,                          // 网格的高度
                (Resolution > 1 ? 0.5f + 0.25f / Resolution : 0.5f) * sqrt(3f)  // 网格深度
            ));

        // 网格分辨率属性，表示生成的六边形的数量和密度
        public int Resolution { get; set; }

        // 核心函数，执行生成网格的逻辑，参数 x 表示当前处理的列，S 是实现 IMeshStreams 接口的结构体
        public void Execute<S>(int x, S streams) where S : struct, IMeshStreams
        {
            // vi 表示当前顶点的起始索引，ti 表示当前三角形的起始索引
            int vi = 7 * Resolution * x, ti = 6 * Resolution * x;

            // h 是六边形的高度偏移量，等于 sqrt(3) / 4，用于计算顶点位置
            float h = sqrt(3f) / 4f;

            // 用于偏移六边形中心的偏移量
            float2 centerOffset = 0f;

            // 如果分辨率大于 1，计算中心的偏移量，使六边形呈现交错排列
            if (Resolution > 1)
            {
                centerOffset.x = -0.375f * (Resolution - 1);
                centerOffset.y = (((x & 1) == 0 ? 0.5f : 1.5f) - Resolution) * h;
            }

            // 循环遍历当前列中的所有六边形，z 表示行索引
            for (int z = 0; z < Resolution; z++, vi += 7, ti += 6)
            {
                // 计算当前六边形中心位置，并考虑中心偏移
                var center = (float2(0.75f * x, 2f * h * z) + centerOffset) / Resolution;

                // 计算六边形顶点的 x 和 z 坐标
                var xCoordinates =
                    center.x + float4(-0.5f, -0.25f, 0.25f, 0.5f) / Resolution;
                var zCoordinates = center.y + float2(h, -h) / Resolution;

                // 定义一个顶点结构体，用于存储每个顶点的相关数据
                var vertex = new Vertex();
                vertex.normal.y = 1f; // 设置顶点法线方向
                vertex.tangent.xw = float2(1f, -1f); // 设置顶点切线方向

                // 设置六边形中心顶点的数据
                vertex.position.xz = center;
                vertex.texCoord0 = 0.5f; // 中心点的纹理坐标
                streams.SetVertex(vi + 0, vertex);

                // 设置其他六个顶点的位置信息和纹理坐标
                vertex.position.x = xCoordinates.x;
                vertex.texCoord0.x = 0f;
                streams.SetVertex(vi + 1, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.position.z = zCoordinates.x;
                vertex.texCoord0 = float2(0.25f, 0.5f + h);
                streams.SetVertex(vi + 2, vertex);

                vertex.position.x = xCoordinates.z;
                vertex.texCoord0.x = 0.75f;
                streams.SetVertex(vi + 3, vertex);

                vertex.position.x = xCoordinates.w;
                vertex.position.z = center.y;
                vertex.texCoord0 = float2(1f, 0.5f);
                streams.SetVertex(vi + 4, vertex);

                vertex.position.x = xCoordinates.z;
                vertex.position.z = zCoordinates.y;
                vertex.texCoord0 = float2(0.75f, 0.5f - h);
                streams.SetVertex(vi + 5, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.texCoord0.x = 0.25f;
                streams.SetVertex(vi + 6, vertex);

                // 生成六边形的六个三角形，定义每个三角形的顶点索引
                streams.SetTriangle(ti + 0, vi + int3(0, 1, 2));
                streams.SetTriangle(ti + 1, vi + int3(0, 2, 3));
                streams.SetTriangle(ti + 2, vi + int3(0, 3, 4));
                streams.SetTriangle(ti + 3, vi + int3(0, 4, 5));
                streams.SetTriangle(ti + 4, vi + int3(0, 5, 6));
                streams.SetTriangle(ti + 5, vi + int3(0, 6, 1));
            }
        }
    }
}
