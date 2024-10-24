using Unity.Mathematics; // 引入数学相关的库
using UnityEngine; // 引入Unity引擎的库

using static Unity.Mathematics.math; // 导入数学库中的静态函数

namespace ProceduralMeshes.Generators // 命名空间，表示生成程序网格的模块
{
    // 定义UV球体生成器，实现IMeshGenerator接口
    public struct UVSphere : IMeshGenerator
    {
        // 计算顶点总数：顶点数量为 (Resolution + 1) * (Resolution + 1) - 2
        public int VertexCount => (ResolutionU + 1) * (ResolutionV + 1) - 2;

        // 计算索引总数：每个网格有6个索引（两个三角形），索引数量为 6 * Resolution * Resolution
        public int IndexCount => 6 * ResolutionU * (ResolutionV - 1);

        // 作业长度，表示需要处理的列数
        public int JobLength => ResolutionU + 1;

        // 网格的边界，定义一个中心在 (0, 0, 0)，宽度和深度为 1 的边界框
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));

        // 网格的分辨率，Resolution 定义了球体网格的精细度
        public int Resolution { get; set; }

        // ResolutionV 是沿纬度方向的分辨率，等于 Resolution 的两倍
        int ResolutionV => 2 * Resolution;

        // ResolutionU 是沿经度方向的分辨率，等于 Resolution 的四倍
        int ResolutionU => 4 * Resolution;

        // 执行常规的网格生成逻辑函数，传入 u 表示当前处理的列，S 是实现 IMeshStreams 的结构体
        public void ExecuteRegular<S>(int u, S streams) where S : struct, IMeshStreams
        {
            // vi 是顶点索引，ti 是三角形索引，当前列的第一个顶点和第一个三角形索引
            int vi = (ResolutionV + 1) * u - 2, ti = 2 * (ResolutionV - 1) * (u - 1);

            // 初始化一个顶点对象，设定法线方向（Y轴）和切线
            var vertex = new Vertex();
            vertex.position.y = vertex.normal.y = -1f;

            // 使用 sincos 函数计算切线的 x 和 z 分量，并设置 w 为 -1
            sincos(
                2f * PI * (u - 0.5f) / ResolutionU,
                out vertex.tangent.z, out vertex.tangent.x
            );
            vertex.tangent.w = -1f;

            // 计算纹理坐标
            vertex.texCoord0.x = (u - 0.5f) / ResolutionU;
            streams.SetVertex(vi, vertex); // 设置当前顶点信息

            // 设置顶部的顶点
            vertex.position.y = vertex.normal.y = 1f;
            vertex.texCoord0.y = 1f;
            streams.SetVertex(vi + ResolutionV, vertex);
            vi += 1; // 索引递增

            // 计算圆周上各点的正弦和余弦值，得到单位圆的坐标
            float2 circle;
            sincos(2f * PI * u / ResolutionU, out circle.x, out circle.y);
            vertex.tangent.xz = circle.yx; // 交换 y 和 x
            circle.y = -circle.y; // 反转 y 方向

            vertex.texCoord0.x = (float)u / ResolutionU;

            // 判断是否需要偏移三角形的顶点索引
            int shiftLeft = (u == 1 ? 0 : -1) - ResolutionV;

            // 设置三角形的顶点索引
            streams.SetTriangle(ti, vi + int3(-1, shiftLeft, 0));
            ti += 1;

            // 循环为每行生成顶点
            for (int v = 1; v < ResolutionV; v++, vi++) //, ti += 2)
            {
                // 计算纬度方向上的圆形半径
                sincos(
                     PI + PI * v / ResolutionV,
                     out float circleRadius, out vertex.position.y
                 );

                // 计算当前顶点的位置
                vertex.position.xz = circle * -circleRadius;
                vertex.normal = vertex.position; // 法线和顶点位置相同
                vertex.texCoord0.y = (float)v / ResolutionV; // 计算纹理坐标
                streams.SetVertex(vi, vertex); // 设置顶点信息

                // 如果不是第一行，则设置三角形的顶点索引
                if (v > 1)
                {
                    streams.SetTriangle(ti + 0, vi + int3(shiftLeft - 1, shiftLeft, -1));
                    streams.SetTriangle(ti + 1, vi + int3(-1, shiftLeft, 0));
                    ti += 2;
                }
            }
            // 最后一个三角形
            streams.SetTriangle(ti, vi + int3(shiftLeft - 1, 0, -1));
        }

        // 处理球体接缝的函数，生成接缝部分的顶点
        public void ExecuteSeam<S>(S streams) where S : struct, IMeshStreams
        {
            // 初始化一个顶点对象
            var vertex = new Vertex();

            // 设置切线方向
            vertex.tangent.x = 1f;
            vertex.tangent.w = -1f;

            // 循环为接缝生成顶点
            for (int v = 1; v < ResolutionV; v++) //, ti += 2)
            {
                // 计算纬度方向上的位置和法线
                sincos(
                     PI + PI * v / ResolutionV,
                     out vertex.position.z, out vertex.position.y
                 );
                vertex.normal = vertex.position; // 法线和顶点位置相同
                vertex.texCoord0.y = (float)v / ResolutionV; // 设置纹理坐标
                streams.SetVertex(v - 1, vertex); // 设置顶点信息
            }
        }

        // 主执行函数，根据列数调用对应的处理函数
        public void Execute<S>(int u, S streams) where S : struct, IMeshStreams
        {
            if (u == 0)
            {
                ExecuteSeam(streams); // 处理接缝部分
            }
            else
            {
                ExecuteRegular(u, streams); // 常规生成逻辑
            }
        }
    }
}
