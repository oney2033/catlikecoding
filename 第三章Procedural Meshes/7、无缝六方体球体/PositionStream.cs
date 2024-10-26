using System.Runtime.CompilerServices; // 引入用于编译优化的命名空间
using System.Runtime.InteropServices; // 引入用于结构布局的命名空间
using Unity.Collections; // 引入 Unity 的集合命名空间
using Unity.Mathematics; // 引入 Unity 的数学命名空间
using UnityEngine; // 引入 Unity 引擎命名空间
using UnityEngine.Rendering; // 引入 Unity 渲染命名空间
using Unity.Collections.LowLevel.Unsafe; // 引入低级别的集合操作的命名空间

namespace ProceduralMeshes.Streams // 定义 ProceduralMeshes.Streams 命名空间
{
    // 实现 IMeshStreams 接口的结构体，用于管理顶点位置流和三角形索引流
    public struct PositionStream : IMeshStreams
    {
        // 禁用容器安全性检查，以提高性能
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float3> stream0; // 顶点位置流

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TriangleUInt16> triangles; // 三角形索引流

        // 设置网格数据的方法
        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
        {
            // 创建一个用于描述顶点属性的数组
            var descriptor = new NativeArray<VertexAttributeDescriptor>(
                1, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3); // 设置顶点属性的维度为3（x, y, z）

            // 设置网格的顶点缓冲区参数
            meshData.SetVertexBufferParams(vertexCount, descriptor);
            descriptor.Dispose(); // 释放描述符数组的资源

            // 设置网格的索引缓冲区参数
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            // 设置网格的子网格数量和参数
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(
                 0, new SubMeshDescriptor(0, indexCount)
                 {
                     bounds = bounds, // 设置子网格的边界
                     vertexCount = vertexCount // 设置顶点数量
                 },
                 // 优化性能：不重新计算边界和不验证索引
                 MeshUpdateFlags.DontRecalculateBounds |
                 MeshUpdateFlags.DontValidateIndices
             );

            // 获取顶点数据并分配给 stream0
            stream0 = meshData.GetVertexData<float3>(); // 获取顶点位置数据
            // 获取索引数据，并重新解释为 TriangleUInt16 格式
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2); // 获取三角形索引数据
        }

        // 设置单个顶点数据的方法
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, Vertex vertex)
        {
            stream0[index] = vertex.position; // 设置顶点位置
        }

        // 设置三角形数据的方法
        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle; // 设置三角形索引
    }
}
