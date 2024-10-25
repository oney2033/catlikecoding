using Unity.Mathematics; // 引入数学相关的库
using UnityEngine; // 引入Unity引擎的库

using static Unity.Mathematics.math; // 导入数学库中的静态函数

namespace ProceduralMeshes.Generators // 命名空间，表示生成程序网格的模块
{
    // 定义方形网格生成器，实现IMeshGenerator接口
    public struct CubeSphere : IMeshGenerator
    {
        // 计算顶点总数：每个网格有4个顶点，分辨率为Resolution
        public int VertexCount => 6 * 4 * Resolution * Resolution;
        // 计算索引总数：每个网格有6个索引（两个三角形），分辨率为Resolution
        public int IndexCount =>6 * 6 * Resolution * Resolution;
        // 作业长度，表示要处理的行数
        public int JobLength => 6 * Resolution;
        // 网格的边界，中心在(0,0,0)，宽度和深度为1
       public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        public int Resolution { get; set; } // 网格的分辨率

        struct Side
        {
            public int id;
            public float3 uvOrigin, uVector, vVector;
           
        }

        static Side GetSide(int id) => id switch
        {
            0 => new Side
            {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * right(),
                vVector = 2f * up()
            },
            1 => new Side 
            {
                id = id,
                uvOrigin = float3(1f, -1f, -1f),
                uVector = 2f * forward(),
                vVector = 2f * up()
            },
            2 => new Side
            {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * forward(),
                vVector = 2f * right()
            },
            3 => new Side
            {
                id = id,
                uvOrigin = float3(-1f, -1f, 1f),
                uVector = 2f * up(),
                vVector = 2f * right()
            },
            4 => new Side
            {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * up(),
                vVector = 2f * forward()
            },
            _ => new Side
            {
                id = id,
                uvOrigin = float3(-1f, 1f, -1f),
                uVector = 2f * right(),
                vVector = 2f * forward()
            }
        };

        static float3 CubeToSphere(float3 p) => p * sqrt(
            1f - ((p * p).yxx + (p * p).zzy) / 2f + (p * p).yxx * (p * p).zzy / 3f
        );

        // 执行网格生成的逻辑
        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {
            int u = i / 6;
            Side side = GetSide(i - 6 * u);

            int vi = 4 * Resolution * (Resolution * side.id + u);
            int ti = 2 * Resolution * (Resolution * side.id + u);

            float3 uA = side.uvOrigin + side.uVector * u / Resolution;
            float3 uB = side.uvOrigin + side.uVector * (u + 1) / Resolution;

            float3 pA = CubeToSphere(uA), pB = CubeToSphere(uB);

            var vertex = new Vertex();
            vertex.tangent = float4(normalize(pB - pA), -1f);

            // 遍历当前行中的每一列
            for (int v = 1; v <= Resolution; v++, vi += 4, ti += 2)
            {
                float3 pC = CubeToSphere(uA + side.vVector * v / Resolution);
                float3 pD = CubeToSphere(uB + side.vVector * v / Resolution);

                // 创建一个顶点实例
               
                vertex.position = pA;
                vertex.normal = normalize(cross(pC - pA, vertex.tangent.xyz));
                vertex.texCoord0 = 0f;
                streams.SetVertex(vi + 0, vertex); // 存储第一个顶点

                vertex.position = pB;
                vertex.normal = normalize(cross(pD - pB, vertex.tangent.xyz));
                vertex.texCoord0 = float2(1f, 0f); // 设置纹理坐标
                streams.SetVertex(vi + 1, vertex); // 存储第二个顶点

                vertex.position = pC;
                
                vertex.tangent.xyz = normalize(pD - pC);
                vertex.normal = normalize(cross(pC - pA, vertex.tangent.xyz));
                vertex.texCoord0 = float2(0f, 1f); // 设置纹理坐标
                streams.SetVertex(vi + 2, vertex); // 存储第三个顶点

                vertex.position = pD;
                vertex.normal = normalize(cross(pD - pB, vertex.tangent.xyz));
                vertex.texCoord0 = 1f; // 设置纹理坐标
                streams.SetVertex(vi + 3, vertex); // 存储第四个顶点

                // 设置两个三角形的索引
                streams.SetTriangle(ti + 0, vi + int3(0, 2, 1)); // 第一个三角形
                streams.SetTriangle(ti + 1, vi + int3(1, 2, 3)); // 第二个三角形
                
                pA = pC;
                pB = pD;
            }
        }
    }
}
