using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using static Unity.Mathematics.math;

// ȷ����������� MeshFilter �� MeshRenderer
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AdvancedMultiStreamProceduralMesh : MonoBehaviour
{
    // ���������ʱ����
    void OnEnable()
    {
        int vertexAttributeCount = 4; // �������Ե�����
        int vertexCount = 4; // ������������ĸ��������ڹ���һ�����Σ�
        int triangleIndexCount = 6; // �����ζ����������������������Σ���6��������

        // �����д���������ݣ�����һ��������������
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0]; // ��ȡ��һ����������

        // ����һ�����ڶ���������������ԭ�����飬���ڶ�������Ķ�������
        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
            vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        // ����λ�����ԣ�ά��Ϊ3��x, y, z����������Ŀռ�����
        vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);

        // ���巨�����ԣ�ά��Ϊ3��ʹ����1��������3D�ķ�������������գ�
        vertexAttributes[1] = new VertexAttributeDescriptor(
            VertexAttribute.Normal, dimension: 3, stream: 1
        );

        // �����������ԣ�ʹ�ð뾫�ȸ�������float16����ά��Ϊ4�����ڷ�����ͼ�������߿ռ䣩��ʹ����2
        vertexAttributes[2] = new VertexAttributeDescriptor(
             VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 2
        );

        // ���������������ԣ�ʹ�ð뾫�ȸ�������float16����ά��Ϊ2��UV���꣩��ʹ����3
        vertexAttributes[3] = new VertexAttributeDescriptor(
            VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 3
        );

        // ��������Ķ��㻺��������
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose(); // �ͷŶ�������������������ڴ�

        // ��ȡ����λ�����ݣ�float3�����������ĸ������λ��
        NativeArray<float3> positions = meshData.GetVertexData<float3>();
        positions[0] = 0f; // ���½�
        positions[1] = right(); // ���½�
        positions[2] = up(); // ���Ͻ�
        positions[3] = float3(1f, 1f, 0f); // ���Ͻ�

        // ��ȡ�������ݣ�float3�����������ĸ�����ķ��߷���Ϊ���棨back��
        NativeArray<float3> normals = meshData.GetVertexData<float3>(1);
        normals[0] = normals[1] = normals[2] = normals[3] = back(); // ���ж���ķ���ָ����

        // ����뾫�ȸ����� h0 �� h1
        half h0 = half(0f), h1 = half(1f);

        // ��ȡ�������ݣ�half4�����������ĸ��������������
        NativeArray<half4> tangents = meshData.GetVertexData<half4>(2);
        tangents[0] = tangents[1] = tangents[2] = tangents[3] =
            half4(h1, h0, h0, half(-1f)); // ���ж��������������ͬ

        // ��ȡ�����������ݣ�half2�����������ĸ�������������꣨UV��
        NativeArray<half2> texCoords = meshData.GetVertexData<half2>(3);
        texCoords[0] = h0; // ���½� (0,0)
        texCoords[1] = half2(h1, h0); // ���½� (1,0)
        texCoords[2] = half2(h0, h1); // ���Ͻ� (0,1)
        texCoords[3] = h1; // ���Ͻ� (1,1)

        // ���������������������Ĳ�������ʾҪ�õ���������
        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt16);
        // ��ȡ�����ζ������������������������εĶ�������
        NativeArray<ushort> triangleIndices = meshData.GetIndexData<ushort>();
        triangleIndices[0] = 0; // ��һ�������εĶ�������
        triangleIndices[1] = 2;
        triangleIndices[2] = 1;
        triangleIndices[3] = 1; // �ڶ��������εĶ�������
        triangleIndices[4] = 2;
        triangleIndices[5] = 3;

        // ��������İ�Χ�У�Bounds��
        var bounds = new Bounds(new Vector3(0.5f, 0.5f), new Vector3(1f, 1f));
        meshData.subMeshCount = 1; // ��������������Ϊ1
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
        {
            bounds = bounds,
            vertexCount = vertexCount
        }, MeshUpdateFlags.DontRecalculateBounds); // ��ֹ���¼����Χ��

        // ����һ���µ������������
        var mesh = new Mesh
        {
            bounds = bounds,
            name = "Procedural Mesh"
        };

        // Ӧ�ò����ÿ�д���������ݣ����丳ֵ���´���������
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        // �����ɵ�����ֵ�� MeshFilter ���
        GetComponent<MeshFilter>().mesh = mesh;
    }
}
