using System.Runtime.CompilerServices; // 引入系统的运行时编译器服务，用于优化方法执行
using System.Runtime.InteropServices;  // 引入系统的互操作服务，用于定义数据布局
using Unity.Collections; // 引入 Unity 的集合库，主要用于原生数组的管理
using Unity.Mathematics; // 引入 Unity 的数学库，用于数学计算
using UnityEngine; // 引入 Unity 引擎核心库
using UnityEngine.Rendering; // 引入 Unity 的渲染系统
using Unity.Collections.LowLevel.Unsafe; // 引入 Unity 底层集合安全管理

namespace ProceduralMeshes.Streams // 定义命名空间，组织与程序化网格相关的代码流
{
    // 实现 IMeshStreams 接口的结构体，用于处理单一数据流的网格数据
    public struct SingleStream : IMeshStreams
    {
        // 内部结构体，定义了每个顶点的数据流，包括位置、法线、切线和纹理坐标
        [StructLayout(LayoutKind.Sequential)] // 指定数据布局为顺序布局
        struct Stream0
        {
            public float3 position, normal; // 顶点的位置和法线，均为三维向量
            public float4 tangent; // 顶点的切线，四维向量
            public float2 texCoord0; // 顶点的第一个纹理坐标，二维向量
        }

        // 禁用容器的安全限制，提升性能（但也增加了内存管理的风险）
        [NativeDisableContainerSafetyRestriction]
        NativeArray<Stream0> stream0; // 存储顶点数据的原生数组

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TriangleUInt16> triangles; // 存储三角形数据的原生数组

        // 实现接口的 Setup 方法，用于初始化网格数据
        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
        {
            // 创建一个顶点属性描述符的数组，用于定义顶点缓冲区的结构
            var descriptor = new NativeArray<VertexAttributeDescriptor>(
                4, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            // 设置顶点位置属性，三维向量
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
            // 设置法线属性，三维向量
            descriptor[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3
            );
            // 设置切线属性，四维向量
            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4
            );
            // 设置纹理坐标属性，二维向量
            descriptor[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2
            );
            // 设置顶点缓冲区参数
            meshData.SetVertexBufferParams(vertexCount, descriptor);
            // 释放描述符的内存
            descriptor.Dispose();

            // 设置索引缓冲区参数，使用 16 位整数
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            // 设置网格的子网格数量为 1
            meshData.subMeshCount = 1;
            // 设置子网格的属性，包括边界和顶点数量
            meshData.SetSubMesh(
                0, new SubMeshDescriptor(0, indexCount)
                {
                    bounds = bounds, // 设置子网格的边界
                    vertexCount = vertexCount // 顶点数量
                },
                // 不重新计算边界和验证索引，以提高性能
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontValidateIndices
            );
            // 获取顶点数据流并将其存储在 stream0 中
            stream0 = meshData.GetVertexData<Stream0>();
            // 获取三角形数据流并将其转换为 TriangleUInt16 格式
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }

        // 用于设置指定索引处的顶点数据
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // 优化方法调用
        public void SetVertex(int index, Vertex vertex) => stream0[index] = new Stream0
        {
            position = vertex.position, // 设置顶点的位置
            normal = vertex.normal,     // 设置顶点的法线
            tangent = vertex.tangent,   // 设置顶点的切线
            texCoord0 = vertex.texCoord0 // 设置顶点的纹理坐标
        };

        // 用于设置指定索引处的三角形数据
        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle;
    }
}
