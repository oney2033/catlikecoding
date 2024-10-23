using Unity.Mathematics; // 引入数学相关的库
using UnityEngine; // 引入Unity引擎的库

using static Unity.Mathematics.math; // 导入数学库中的静态函数

namespace ProceduralMeshes.Generators // 命名空间，表示生成程序网格的模块
{
    // 定义六边形网格生成器，实现IMeshGenerator接口
    public struct PointyHexagonGrid : IMeshGenerator
    {
        // 计算顶点总数：每个六边形有7个顶点，共有 Resolution * Resolution 个六边形
        public int VertexCount => 7 * Resolution * Resolution;

        // 计算索引总数：每个六边形有6个三角形，每个三角形需要3个索引
        public int IndexCount => 18 * Resolution * Resolution;

        // 作业长度，表示需要处理的行数，每一行有 Resolution 个六边形
        public int JobLength => Resolution;

        // 网格的边界，定义一个中心在 (0, 0, 0)，宽度和深度为 1 的边界框
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(
            (Resolution > 1 ? 0.5f + 0.25f / Resolution : 0.5f) * sqrt(3f), // X轴方向大小
            0f, // Y轴方向大小
            0.75f + 0.25f / Resolution // Z轴方向大小
        ));

        public int Resolution { get; set; } // 网格的分辨率

        // 执行网格生成逻辑的函数，传入 z 表示当前处理的行，S 是实现 IMeshStreams 的结构体
        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            // vi 是顶点索引，ti 是三角形索引，当前行的第一个顶点和第一个三角形索引
            int vi = 7 * Resolution * z, ti = 6 * Resolution * z;

            // 计算六边形的高度
            float h = sqrt(3f) / 4f;
            // 初始化中心偏移量
            float2 centerOffset = 0f;

            // 如果分辨率大于1，计算中心的偏移
            if (Resolution > 1)
            {
                centerOffset.x = (((z & 1) == 0 ? 0.5f : 1.5f) - Resolution) * h;
                centerOffset.y = -0.375f * (Resolution - 1);
            }

            // 循环为每列生成顶点
            for (int x = 0; x < Resolution; x++, vi += 7, ti += 6)
            {
                // 计算六边形的中心点位置，并将其除以分辨率进行归一化
                var center = (float2(2f * h * x, 0.75f * z) + centerOffset) / Resolution;

                // 计算六边形的X方向和Z方向的坐标
                var xCoordinates = center.x + float2(-h, h) / Resolution;
                var zCoordinates = center.y + float4(-0.5f, -0.25f, 0.25f, 0.5f) / Resolution;

                var vertex = new Vertex(); // 初始化一个顶点对象
                vertex.normal.y = 1f; // 设定法线方向（Y轴向上）
                vertex.tangent.xw = float2(1f, -1f); // 设置顶点切线

                // 设置中心顶点
                vertex.position.xz = center; // XZ平面位置
                vertex.texCoord0 = 0.5f; // 中心顶点纹理坐标为 (0.5, 0.5)
                streams.SetVertex(vi + 0, vertex); // 设置第一个中心顶点

                // 设置六边形的其他顶点
                vertex.position.z = zCoordinates.x;
                vertex.texCoord0.y = 0f; // 顶点的纹理Y坐标为0
                streams.SetVertex(vi + 1, vertex); // 设置第二个顶点

                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.y;
                vertex.texCoord0 = float2(0.5f - h, 0.25f); // 顶点的纹理坐标
                streams.SetVertex(vi + 2, vertex); // 设置第三个顶点

                vertex.position.z = zCoordinates.z;
                vertex.texCoord0.y = 0.75f; // 顶点的纹理Y坐标为0.75
                streams.SetVertex(vi + 3, vertex); // 设置第四个顶点

                vertex.position.x = center.x;
                vertex.position.z = zCoordinates.w;
                vertex.texCoord0 = float2(0.5f, 1f); // 顶点的纹理坐标为 (0.5, 1)
                streams.SetVertex(vi + 4, vertex); // 设置第五个顶点

                vertex.position.x = xCoordinates.y;
                vertex.position.z = zCoordinates.z;
                vertex.texCoord0 = float2(0.5f + h, 0.75f); // 顶点的纹理坐标
                streams.SetVertex(vi + 5, vertex); // 设置第六个顶点

                vertex.position.z = zCoordinates.y;
                vertex.texCoord0.y = 0.25f; // 顶点的纹理Y坐标为0.25
                streams.SetVertex(vi + 6, vertex); // 设置第七个顶点

                // 设置每个六边形的六个三角形索引，使用之前设置的顶点
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
