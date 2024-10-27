using Unity.Mathematics; // 引入数学相关的库
using UnityEngine; // 引入Unity引擎的库

using static Unity.Mathematics.math; // 导入数学库中的静态函数

namespace ProceduralMeshes.Generators // 命名空间，表示生成程序网格的模块
{
    // 定义方形网格生成器，实现IMeshGenerator接口
    public struct Octasphere : IMeshGenerator
    {
        // 计算顶点总数：每个网格有4个顶点，分辨率为Resolution
        public int VertexCount => 4 * Resolution * Resolution + 2 * Resolution + 7;
        // 计算索引总数：每个网格有6个索引（两个三角形），分辨率为Resolution
        public int IndexCount => 6 * 4 * Resolution * Resolution;
        // 作业长度，表示要处理的行数
        public int JobLength => 4 * Resolution + 1;
        // 网格的边界，中心在(0,0,0)，宽度和深度为1
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        public int Resolution { get; set; } // 网格的分辨率

        // 定义一个结构体，用于表示立方体的每一面
        struct Rhombus
        {
            public int id; // 立方体面的ID
            public float3 leftCorner, rightCorner;

        }

        // 根据ID获取立方体的一面
        static Rhombus GetRhombus(int id) => id switch
        {
            0 => new Rhombus
            {
                id = id,
                leftCorner = back(),
                rightCorner = right()
            },
            1 => new Rhombus
            {
                id = id,
                leftCorner = right(),
                rightCorner = forward()
            },
            2 => new Rhombus
            {
                id = id,
                leftCorner = forward(),
                rightCorner = left()
            },
            _ => new Rhombus
            {
                id = id,
                leftCorner = left(),
                rightCorner = back()
            }
        };

        static float2 GetTangentXZ(float3 p) => normalize(float2(-p.z, p.x));

       // static float2 GetTexCoord(float3 p) => float2(
       //     atan2(p.x, p.z) / (-2f * PI) + 0.5f,
       //     asin(p.y) / PI + 0.5f
       // );
        static float2 GetTexCoord(float3 p)
        {
            var texCoord = float2(
                atan2(p.x, p.z) / (-2f * PI) + 0.5f,
                asin(p.y) / PI + 0.5f
            );
            if (texCoord.x < 1e-6f)
            {
                texCoord.x = 1f;
            }
            return texCoord;
        }

        // 执行网格生成的逻辑
        public void ExecuteRegular<S>(int i, S streams) where S : struct, IMeshStreams
        {
            int u = i / 4;
            Rhombus rhombus = GetRhombus(i - 4 * u);
            int vi = Resolution * (Resolution * rhombus.id + u + 2) + 7;
            int ti = 2 * Resolution * (Resolution * rhombus.id + u); // 计算当前三角形索引

            bool firstColumn = u == 0;
            int4 quad = int4(
                vi,
                firstColumn ? rhombus.id : vi - Resolution,
                firstColumn ?
                    rhombus.id == 0 ? 8 : vi - Resolution * (Resolution + u) :
                    vi - Resolution + 1,
                vi + 1
            );
           
            u += 1;

            float3 columnBottomDir = rhombus.rightCorner - down();
            float3 columnBottomStart = down() + columnBottomDir * u / Resolution;
            float3 columnBottomEnd = rhombus.leftCorner + columnBottomDir * u / Resolution;

            float3 columnTopDir = up() - rhombus.leftCorner;
            float3 columnTopStart =
                 rhombus.rightCorner + columnTopDir * ((float)u / Resolution - 1f);
            float3 columnTopEnd = rhombus.leftCorner + columnTopDir * u / Resolution;

            var vertex = new Vertex(); // 创建一个顶点实例
            vertex.position = normalize(columnBottomStart);
            vertex.tangent.xz = GetTangentXZ(vertex.position);
            vertex.tangent.w = -1f;
            vertex.texCoord0 = GetTexCoord(vertex.position);
            streams.SetVertex(vi, vertex);
       
            vi += 1;
          
            // 遍历当前行中的每一列
            for (int v = 1; v < Resolution; v++, vi++, ti += 2)
            {
                if (v <= Resolution - u)
                {
                    vertex.position =
                        lerp(columnBottomStart, columnBottomEnd, (float)v / Resolution);
                }
                else
                {
                    vertex.position =
                        lerp(columnTopStart, columnTopEnd, (float)v / Resolution);
                }
                vertex.normal = vertex.position = normalize(vertex.position);
                vertex.tangent.xz = GetTangentXZ(vertex.position);
                vertex.texCoord0 = GetTexCoord(vertex.position);
                streams.SetVertex(vi, vertex);
                // 设置两个三角形的索引

                streams.SetTriangle(ti + 0, quad.xyz);
                streams.SetTriangle(ti + 1, quad.xzw);

                quad.y = quad.z;
                quad += int4(1, 0, firstColumn && rhombus.id != 0 ? Resolution : 1, 1);
            }
           quad.z = Resolution * Resolution * rhombus.id + Resolution + u + 6;
			quad.w = u < Resolution ? quad.z + 1 : rhombus.id + 4;
         
            streams.SetTriangle(ti + 0, quad.xyz);
            streams.SetTriangle(ti + 1, quad.xzw);
        }

        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {
            if (i == 0)
            {
                ExecutePolesAndSeam(streams);
            }
            else
            {
                ExecuteRegular(i - 1, streams);
            }
        }
        public void ExecutePolesAndSeam<S>(S streams) where S : struct, IMeshStreams 
        {
            var vertex = new Vertex();
            vertex.tangent = float4(sqrt(0.5f), 0f, sqrt(0.5f), -1f);
            vertex.texCoord0.x = 0.125f;

            for (int i = 0; i < 4; i++)
            {
                vertex.position = vertex.normal = down();
                vertex.texCoord0.y = 0f;
                streams.SetVertex(i, vertex);
                vertex.position = vertex.normal = up();
                vertex.texCoord0.y = 1f;
                streams.SetVertex(i + 4, vertex);
                vertex.tangent.xz = float2(-vertex.tangent.z, vertex.tangent.x);
                vertex.texCoord0.x += 0.25f;
            }
            vertex.tangent.xz = float2(1f, 0f);
            vertex.texCoord0.x = 0f;

            for (int v = 1; v < 2 * Resolution; v++)
            {
                if (v < Resolution)
                {
                    vertex.position = lerp(down(), back(), (float)v / Resolution);
                }
                else
                {
                    vertex.position =
                        lerp(back(), up(), (float)(v - Resolution) / Resolution);
                }
                vertex.normal = vertex.position = normalize(vertex.position);
               // vertex.texCoord0.y = (vertex.texCoord0 = GetTexCoord(vertex.position)).y;
                 vertex.texCoord0 = GetTexCoord(vertex.position); // 返回 float2 赋值给 texCoord0
                vertex.texCoord0.y = GetTexCoord(vertex.position).y;
                vertex.texCoord0.x = 0f;
                streams.SetVertex(v + 7, vertex);
            }
        }
    }
}
