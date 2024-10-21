using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Runtime.InteropServices;

// 确保该组件具有 MeshFilter 和 MeshRenderer
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AdvancedSingleStreamProceduralMesh : MonoBehaviour
{
    // 使用顺序布局方式定义结构体，确保字段按声明顺序排列
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        // 顶点的三个属性，position 代表顶点的位置坐标（float3：x, y, z）
        public float3 position, normal;

        // 切线属性（half4：x, y, z, w），使用半精度浮点数来减少内存占用
        public half4 tangent;

        // 纹理坐标属性（half2：u, v），表示纹理映射的二维坐标，使用半精度浮点数
        public half2 texCoord0;
    }


    // 当组件启用时调用
    void OnEnable()
    {
        int vertexAttributeCount = 4; // 顶点属性的数量
        int vertexCount = 4; // 顶点的数量（四个顶点用于构成一个矩形）
        int triangleIndexCount = 6; // 三角形顶点索引数量（两个三角形，共6个索引）

        // 分配可写的网格数据，创建一个网格数据数组
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0]; // 获取第一个网格数据

        // 创建一个用于顶点属性描述符的原生数组，用于定义网格的顶点属性
        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
            vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        // 定义位置属性，维度为3（x, y, z），即顶点的空间坐标
        vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);

        // 定义法线属性，维度为3，（法线是3D的方向，用来计算光照）
        vertexAttributes[1] = new VertexAttributeDescriptor(
            VertexAttribute.Normal, dimension: 3
        );

        // 定义切线属性，使用半精度浮点数（float16），维度为4（用于法线贴图计算切线空间）
        vertexAttributes[2] = new VertexAttributeDescriptor(
             VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4
        );

        // 定义纹理坐标属性，使用半精度浮点数（float16），维度为2（UV坐标）
        vertexAttributes[3] = new VertexAttributeDescriptor(
            VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2
        );

        // 设置网格的顶点缓冲区参数
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose(); // 释放顶点属性描述符数组的内存


        // 获取网格数据的顶点数组，类型为自定义的 Vertex 结构体
        NativeArray<Vertex> vertices = meshData.GetVertexData<Vertex>();

        // 定义两个 half 精度浮点数，h0 表示 0，h1 表示 1
        half h0 = half(0f), h1 = half(1f);

        // 创建一个新的 Vertex 结构体，并初始化法线为指向背面（负 Z 轴方向）
        // 切线初始化为 (1, 0, 0, -1)，表示沿 X 轴的方向
        var vertex = new Vertex
        {
            normal = back(), // 法线指向背面 (0, 0, -1)
            tangent = half4(h1, h0, h0, half(-1f)) // 切线向量为 (1, 0, 0, -1)
        };

        // 设置第一个顶点的位置为 (0, 0, 0) 并将纹理坐标设置为 (0, 0)
        vertex.position = 0f; // 顶点位置为 (0, 0, 0)
        vertex.texCoord0 = h0; // 纹理坐标为 (0, 0)
        vertices[0] = vertex; // 将顶点数据存入数组第一个元素

        // 设置第二个顶点的位置为 (1, 0, 0) 并将纹理坐标设置为 (1, 0)
        vertex.position = right(); // 顶点位置为 (1, 0, 0)
        vertex.texCoord0 = half2(h1, h0); // 纹理坐标为 (1, 0)
        vertices[1] = vertex; // 将顶点数据存入数组第二个元素

        // 设置第三个顶点的位置为 (0, 1, 0) 并将纹理坐标设置为 (0, 1)
        vertex.position = up(); // 顶点位置为 (0, 1, 0)
        vertex.texCoord0 = half2(h0, h1); // 纹理坐标为 (0, 1)
        vertices[2] = vertex; // 将顶点数据存入数组第三个元素

        // 设置第四个顶点的位置为 (1, 1, 0) 并将纹理坐标设置为 (1, 1)
        vertex.position = float3(1f, 1f, 0f); // 顶点位置为 (1, 1, 0)
        vertex.texCoord0 = h1; // 纹理坐标为 (1, 1)
        vertices[3] = vertex; // 将顶点数据存入数组第四个元素


        // 设置三角形索引缓冲区的参数，表示要用的索引数量
        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt16);
        // 获取三角形顶点索引，并设置两个三角形的顶点索引
        NativeArray<ushort> triangleIndices = meshData.GetIndexData<ushort>();
        triangleIndices[0] = 0; // 第一个三角形的顶点索引
        triangleIndices[1] = 2;
        triangleIndices[2] = 1;
        triangleIndices[3] = 1; // 第二个三角形的顶点索引
        triangleIndices[4] = 2;
        triangleIndices[5] = 3;

        // 设置网格的包围盒（Bounds）
        var bounds = new Bounds(new Vector3(0.5f, 0.5f), new Vector3(1f, 1f));
        meshData.subMeshCount = 1; // 设置子网格数量为1
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
        {
            bounds = bounds,
            vertexCount = vertexCount
        }, MeshUpdateFlags.DontRecalculateBounds); // 禁止重新计算包围盒

        // 创建一个新的网格对象并命名
        var mesh = new Mesh
        {
            bounds = bounds,
            name = "Procedural Mesh"
        };

        // 应用并处置可写的网格数据，将其赋值给新创建的网格
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        // 将生成的网格赋值给 MeshFilter 组件
        GetComponent<MeshFilter>().mesh = mesh;
    }
}
