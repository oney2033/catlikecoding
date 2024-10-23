using Unity.Mathematics; // 引入 Unity 的数学库
using UnityEngine; // 引入 Unity 引擎的库

using static Unity.Mathematics.math; // 使用数学库中的静态函数

namespace ProceduralMeshes.Generators // 定义程序化网格生成的命名空间
{
    // 定义一个共享顶点的三角形网格生成器，实现 IMeshGenerator 接口
    public struct SharedTriangleGrid : IMeshGenerator
    {
        // 计算顶点总数，网格的顶点数量为 (Resolution + 1) * (Resolution + 1)
        public int VertexCount => (Resolution + 1) * (Resolution + 1);

        // 计算索引总数，索引数量为 6 * Resolution * Resolution，每个网格有 6 个索引（2 个三角形）
        public int IndexCount => 6 * Resolution * Resolution;

        // 计算作业长度，表示行数，由于使用共享顶点，每行的顶点数量为 Resolution + 1
        public int JobLength => Resolution + 1;

        // 定义网格的边界（Bound），中心在 (0, 0, 0)，宽度为 1 + 0.5 / Resolution，深度为等边三角形的高度
        public Bounds Bounds => new Bounds(
            Vector3.zero, new Vector3(1f + 0.5f / Resolution, 0f, sqrt(3f) / 2f)
        );

        public int Resolution { get; set; } // 网格的分辨率

        // 执行网格生成的逻辑，传入 z 代表当前处理的行，S 是 IMeshStreams 的结构体实现
        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            // vi 是顶点索引，ti 是三角形索引，初始时为当前行的第一个顶点索引和第一个三角形索引
            int vi = (Resolution + 1) * z, ti = 2 * Resolution * (z - 1);
            float xOffset = -0.25f; // 偏移量，初始设为 -0.25f
            float uOffset = 0f; // U 方向的纹理坐标偏移量

            // 定义四个顶点的索引偏移量，iA、iB、iC、iD 代表构成三角形的顶点
            int iA = -Resolution - 2, iB = -Resolution - 1, iC = -1, iD = 0;
            // 定义两个三角形的顶点索引三元组，分别代表三角形 A 和三角形 B
            var tA = int3(iA, iC, iD);
            var tB = int3(iA, iD, iB);

            // 如果当前行为奇数行，调整偏移量和三角形的顶点索引顺序
            if ((z & 1) == 1)
            {
                xOffset = 0.25f; // 偏移量设为 0.25f
                uOffset = 0.5f / (Resolution + 0.5f); // 计算 U 方向的纹理坐标偏移
                tA = int3(iA, iC, iB); // 调整第一个三角形的顶点顺序
                tB = int3(iB, iC, iD); // 调整第二个三角形的顶点顺序
            }

            xOffset = xOffset / Resolution - 0.5f; // 将 X 方向偏移缩放到正确的比例范围
            // 初始化一个顶点对象，设置法线方向为 Y 轴，设置切线向量
            var vertex = new Vertex();
            vertex.normal.y = 1f;
            vertex.tangent.xw = float2(1f, -1f);

            // 设置第一个顶点的位置和纹理坐标，X 坐标设为 xOffset，Z 坐标基于当前行数计算
            vertex.position.x = xOffset;
            vertex.position.z = ((float)z / Resolution - 0.5f) * sqrt(3f) / 2f; // 计算 Z 坐标（等边三角形的高度）
            vertex.texCoord0.x = uOffset; // 设置 U 方向的纹理坐标偏移
            vertex.texCoord0.y = vertex.position.z / (1f + 0.5f / Resolution) + 0.5f; // 计算 V 方向纹理坐标
            streams.SetVertex(vi, vertex); // 设置顶点信息
            vi += 1; // 顶点索引自增

            // 循环生成每一列的顶点
            for (int x = 1; x <= Resolution; x++, vi++, ti += 2)
            {
                vertex.position.x = (float)x / Resolution + xOffset; // 计算 X 方向的顶点位置
                vertex.texCoord0.x = x / (Resolution + 0.5f) + uOffset; // 设置 U 方向的纹理坐标
                streams.SetVertex(vi, vertex); // 设置当前顶点信息

                // 如果不是第一行，则设置两个三角形
                if (z > 0)
                {
                    // 设置第一个三角形的顶点索引
                    streams.SetTriangle(ti + 0, vi + tA);
                    // 设置第二个三角形的顶点索引
                    streams.SetTriangle(ti + 1, vi + tB);
                }
            }
        }
    }
}
