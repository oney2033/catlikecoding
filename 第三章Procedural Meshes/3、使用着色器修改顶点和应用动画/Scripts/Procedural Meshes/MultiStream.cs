using System.Runtime.CompilerServices; // 引入用于编译优化的命名空间
using System.Runtime.InteropServices; // 引入用于结构布局的命名空间
using Unity.Collections; // 引入 Unity 的集合命名空间
using Unity.Mathematics; // 引入 Unity 的数学命名空间
using UnityEngine; // 引入 Unity 引擎命名空间
using UnityEngine.Rendering; // 引入 Unity 渲染命名空间
using Unity.Collections.LowLevel.Unsafe; // 引入不安全集合的命名空间

namespace ProceduralMeshes.Streams // 定义 ProceduralMeshes.Streams 命名空间
{
    // 实现 IMeshStreams 接口的结构体，用于管理多个顶点流
    public struct MultiStream : IMeshStreams
    {
        // 禁用容器安全性检查，以提高性能
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float3> stream0, stream1; // 顶点位置和法线流

        [NativeDisableContainerSafetyRestriction]
        NativeArray<float4> stream2; // 切线流

        [NativeDisableContainerSafetyRestriction]
        NativeArray<float2> stream3; // 纹理坐标流

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TriangleUInt16> triangles; // 三角形索引流

        // 设置网格数据的方法
        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
        {
            // 创建一个顶点属性描述符数组
            var descriptor = new NativeArray<VertexAttributeDescriptor>(
                4, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3); // 顶点位置流
            descriptor[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3, stream: 1 // 法线流
            );
            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4, stream: 2 // 切线流
            );
            descriptor[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2, stream: 3 // 纹理坐标流
            );
            // 设置网格的顶点缓冲区参数
            meshData.SetVertexBufferParams(vertexCount, descriptor);
            descriptor.Dispose(); // 释放描述符数组的资源

            // 设置网格的索引缓冲区参数
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            // 设置子网格数量和相关参数
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(
                 0, new SubMeshDescriptor(0, indexCount)
                 {
                     bounds = bounds, // 设置边界
                     vertexCount = vertexCount // 设置顶点数量
                 },
                 // 不重新计算边界和验证索引，提高性能
                 MeshUpdateFlags.DontRecalculateBounds |
                 MeshUpdateFlags.DontValidateIndices
             );
            // 获取顶点和索引数据
            stream0 = meshData.GetVertexData<float3>(); // 获取顶点位置数据
            stream1 = meshData.GetVertexData<float3>(1); // 获取法线数据
            stream2 = meshData.GetVertexData<float4>(2); // 获取切线数据
            stream3 = meshData.GetVertexData<float2>(3); // 获取纹理坐标数据
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2); // 获取三角形索引数据
        }

        // 设置顶点数据的方法
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, Vertex vertex)
        {
            stream0[index] = vertex.position; // 设置顶点位置
            stream1[index] = vertex.normal; // 设置法线
            stream2[index] = vertex.tangent; // 设置切线
            stream3[index] = vertex.texCoord0; // 设置纹理坐标
        }

        // 设置三角形数据的方法
        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle; // 设置三角形索引
    }
}
