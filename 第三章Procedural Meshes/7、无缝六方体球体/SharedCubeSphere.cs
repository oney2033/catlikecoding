using Unity.Mathematics; // 引入数学相关的库
using UnityEngine; // 引入Unity引擎的库

using static Unity.Mathematics.math; // 导入数学库中的静态函数

namespace ProceduralMeshes.Generators // 命名空间，表示生成程序网格的模块
{
    // 定义方形网格生成器，实现IMeshGenerator接口
    public struct SharedCubeSphere : IMeshGenerator
    {
        // 计算顶点总数：每个网格有4个顶点，分辨率为Resolution
        public int VertexCount => 6 * Resolution * Resolution +2;
        // 计算索引总数：每个网格有6个索引（两个三角形），分辨率为Resolution
        public int IndexCount => 6 * 6 * Resolution * Resolution;
        // 作业长度，表示要处理的行数
        public int JobLength => 6 * Resolution;
        // 网格的边界，中心在(0,0,0)，宽度和深度为1
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        public int Resolution { get; set; } // 网格的分辨率

        // 定义一个结构体，用于表示立方体的每一面
        struct Side
        {
            public int id; // 立方体面的ID
            public float3 uvOrigin, uVector, vVector; // UV原点、U向量和V向量
            public int seamStep;
            public bool TouchesMinimumPole => (id & 1) == 0;

        }

        // 根据ID获取立方体的一面
        static Side GetSide(int id) => id switch
        {
            0 => new Side
            {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * right(),
                vVector = 2f * up(),
                seamStep = 4
            },
            1 => new Side
            {
                id = id,
                uvOrigin = float3(1f, -1f, -1f),
                uVector = 2f * forward(),
                vVector = 2f * up(),
                seamStep = 4
            },
            2 => new Side
            {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * forward(),
                vVector = 2f * right(),
                seamStep = -2
            },
            3 => new Side
            {
                id = id,
                uvOrigin = float3(-1f, -1f, 1f),
                uVector = 2f * up(),
                vVector = 2f * right(),
                seamStep = -2
            },
            4 => new Side
            {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * up(),
                vVector = 2f * forward(),
                seamStep = -2
            },
            _ => new Side
            {
                id = id,
                uvOrigin = float3(-1f, 1f, -1f),
                uVector = 2f * right(),
                vVector = 2f * forward(),
                seamStep = -2
            }
        };

        // 将立方体坐标转换为球面坐标的方法
        static float3 CubeToSphere(float3 p) => p * sqrt(
            1f - ((p * p).yxx + (p * p).zzy) / 2f + (p * p).yxx * (p * p).zzy / 3f
        );

        // 执行网格生成的逻辑
        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {
            int u = i / 6; // 当前处理的U坐标
            Side side = GetSide(i - 6 * u); // 获取当前处理的立方体面

            int vi = Resolution * (Resolution * side.id + u) + 2; // 计算当前顶点索引
            int ti = 2 * Resolution * (Resolution * side.id + u); // 计算当前三角形索引

            bool firstColumn = u == 0;
            u += 1;

            // 计算两个U方向上的顶点位置
            float3 pStart = side.uvOrigin + side.uVector * u / Resolution;
           

            var vertex = new Vertex(); // 创建一个顶点实例

            if (i == 0)
            {
                vertex.position = -sqrt(1f / 3f);
                streams.SetVertex(0, vertex);
                vertex.position = sqrt(1f / 3f);
                streams.SetVertex(1, vertex);
            }

            vertex.position = CubeToSphere(pStart);
            streams.SetVertex(vi, vertex);


            var triangle = int3(
                 vi,
                 firstColumn && side.TouchesMinimumPole ? 0 : vi - Resolution,
                 vi + (firstColumn ?
                     side.TouchesMinimumPole ?
                         side.seamStep * Resolution * Resolution :
                         Resolution == 1 ? side.seamStep : -Resolution + 1 :
                     -Resolution + 1
                 )
             );
            streams.SetTriangle(ti, triangle);
            
            vi += 1;
            ti += 1;

            int zAdd = firstColumn && side.TouchesMinimumPole ? Resolution : 1;
            int zAddLast = firstColumn && side.TouchesMinimumPole ?
                Resolution :
                !firstColumn && !side.TouchesMinimumPole ?
                    Resolution * ((side.seamStep + 1) * Resolution - u) + u :
                    (side.seamStep + 1) * Resolution * Resolution - Resolution + 1;

            // 遍历当前行中的每一列
            for (int v = 1; v < Resolution; v++, vi++, ti += 2)
            {
                // 计算当前列的两个顶点位置
                vertex.position = CubeToSphere(pStart + side.vVector * v / Resolution);
                streams.SetVertex(vi, vertex);
                // 设置两个三角形的索引
                triangle.x += 1;
                triangle.y = triangle.z;
                triangle.z += v == Resolution - 1 ? zAddLast : zAdd;
                streams.SetTriangle(ti + 0, int3(triangle.x - 1, triangle.y, triangle.x));
                streams.SetTriangle(ti + 1, triangle);
            }
            streams.SetTriangle(ti, int3(
                 triangle.x,
                 triangle.z,
                 side.TouchesMinimumPole ?
                    triangle.z + Resolution :
                    u == Resolution ? 1 : triangle.z + 1));
        }
    }
}
