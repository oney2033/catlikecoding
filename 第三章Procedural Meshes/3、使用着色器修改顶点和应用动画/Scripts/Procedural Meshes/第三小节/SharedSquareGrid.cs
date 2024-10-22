using Unity.Mathematics; // 引入数学相关的库
using UnityEngine; // 引入Unity引擎的库

using static Unity.Mathematics.math; // 导入数学库中的静态函数

namespace ProceduralMeshes.Generators // 命名空间，表示生成程序网格的模块
{
    // 定义方形网格生成器，实现IMeshGenerator接口
    public struct SharedSquareGrid : IMeshGenerator
    {
        // 计算顶点总数：共享网格的顶点数量为 (Resolution + 1) * (Resolution + 1)
        public int VertexCount => (Resolution + 1) * (Resolution + 1);

        // 计算索引总数：每个网格有6个索引（两个三角形），索引数量为 6 * Resolution * Resolution
        public int IndexCount => 6 * Resolution * Resolution;

        // 作业长度，表示需要处理的行数，由于使用共享顶点，每行的顶点数为 Resolution + 1
        public int JobLength => Resolution + 1;

        // 网格的边界，定义一个中心在 (0, 0, 0)，宽度和深度为 1 的边界框
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));

        public int Resolution { get; set; } // 网格的分辨率

        // 执行网格生成逻辑的函数，传入 z 表示当前处理的行，S 是实现 IMeshStreams 的结构体
        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            // vi 是顶点索引，ti 是三角形索引，当前行的第一个顶点和第一个三角形索引
            int vi = (Resolution + 1) * z, ti = 2 * Resolution * (z - 1);

            // 初始化一个顶点对象，设定法线方向（Y轴）和切线
            var vertex = new Vertex();
            vertex.normal.y = 1f;
            vertex.tangent.xw = float2(1f, -1f);

            // 设置第一个顶点的位置和纹理坐标，X 坐标设为 -0.5f
            vertex.position.x = -0.5f;
            vertex.position.z = (float)z / Resolution - 0.5f; // Z 方向位置
            vertex.texCoord0.y = (float)z / Resolution; // 纹理坐标Y轴
            streams.SetVertex(vi, vertex); // 设定顶点
            vi += 1; // 索引递增

            // 循环为每列生成顶点
            for (int x = 1; x <= Resolution; x++, vi++, ti += 2)
            {
                vertex.position.x = (float)x / Resolution - 0.5f; // X方向位置
                vertex.texCoord0.x = (float)x / Resolution; // 纹理坐标X轴
                streams.SetVertex(vi, vertex); // 设定顶点

                // 如果不是第一行，则设置三角形
                if (z > 0)
                {
                    // 设置第一个三角形，使用共享的顶点索引
                    streams.SetTriangle(
                        ti + 0, vi + int3(-Resolution - 2, -1, -Resolution - 1)
                    );
                    // 设置第二个三角形
                    streams.SetTriangle(
                        ti + 1, vi + int3(-Resolution - 1, -1, 0)
                    );
                }
            }
        }
    }
}
