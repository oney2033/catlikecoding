using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using static Unity.Mathematics.math;

// 确保该组件具有 MeshFilter 和 MeshRenderer
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AdvancedMultiStreamProceduralMesh : MonoBehaviour
{
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

        // 定义法线属性，维度为3，使用流1（法线是3D的方向，用来计算光照）
        vertexAttributes[1] = new VertexAttributeDescriptor(
            VertexAttribute.Normal, dimension: 3, stream: 1
        );

        // 定义切线属性，使用半精度浮点数（float16），维度为4（用于法线贴图计算切线空间），使用流2
        vertexAttributes[2] = new VertexAttributeDescriptor(
             VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 2
        );

        // 定义纹理坐标属性，使用半精度浮点数（float16），维度为2（UV坐标），使用流3
        vertexAttributes[3] = new VertexAttributeDescriptor(
            VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 3
        );

        // 设置网格的顶点缓冲区参数
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose(); // 释放顶点属性描述符数组的内存

        // 获取顶点位置数据（float3），并设置四个顶点的位置
        NativeArray<float3> positions = meshData.GetVertexData<float3>();
        positions[0] = 0f; // 左下角
        positions[1] = right(); // 右下角
        positions[2] = up(); // 左上角
        positions[3] = float3(1f, 1f, 0f); // 右上角

        // 获取法线数据（float3），并设置四个顶点的法线方向为背面（back）
        NativeArray<float3> normals = meshData.GetVertexData<float3>(1);
        normals[0] = normals[1] = normals[2] = normals[3] = back(); // 所有顶点的法线指向背面

        // 定义半精度浮点数 h0 和 h1
        half h0 = half(0f), h1 = half(1f);

        // 获取切线数据（half4），并设置四个顶点的切线向量
        NativeArray<half4> tangents = meshData.GetVertexData<half4>(2);
        tangents[0] = tangents[1] = tangents[2] = tangents[3] =
            half4(h1, h0, h0, half(-1f)); // 所有顶点的切线向量相同

        // 获取纹理坐标数据（half2），并设置四个顶点的纹理坐标（UV）
        NativeArray<half2> texCoords = meshData.GetVertexData<half2>(3);
        texCoords[0] = h0; // 左下角 (0,0)
        texCoords[1] = half2(h1, h0); // 右下角 (1,0)
        texCoords[2] = half2(h0, h1); // 左上角 (0,1)
        texCoords[3] = h1; // 右上角 (1,1)

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
