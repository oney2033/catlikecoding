using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Runtime.InteropServices;

// ȷ����������� MeshFilter �� MeshRenderer
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AdvancedSingleStreamProceduralMesh : MonoBehaviour
{
    // ʹ��˳�򲼾ַ�ʽ����ṹ�壬ȷ���ֶΰ�����˳������
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        // ������������ԣ�position �������λ�����꣨float3��x, y, z��
        public float3 position, normal;

        // �������ԣ�half4��x, y, z, w����ʹ�ð뾫�ȸ������������ڴ�ռ��
        public half4 tangent;

        // �����������ԣ�half2��u, v������ʾ����ӳ��Ķ�ά���꣬ʹ�ð뾫�ȸ�����
        public half2 texCoord0;
    }


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

        // ���巨�����ԣ�ά��Ϊ3����������3D�ķ�������������գ�
        vertexAttributes[1] = new VertexAttributeDescriptor(
            VertexAttribute.Normal, dimension: 3
        );

        // �����������ԣ�ʹ�ð뾫�ȸ�������float16����ά��Ϊ4�����ڷ�����ͼ�������߿ռ䣩
        vertexAttributes[2] = new VertexAttributeDescriptor(
             VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4
        );

        // ���������������ԣ�ʹ�ð뾫�ȸ�������float16����ά��Ϊ2��UV���꣩
        vertexAttributes[3] = new VertexAttributeDescriptor(
            VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2
        );

        // ��������Ķ��㻺��������
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose(); // �ͷŶ�������������������ڴ�


        // ��ȡ�������ݵĶ������飬����Ϊ�Զ���� Vertex �ṹ��
        NativeArray<Vertex> vertices = meshData.GetVertexData<Vertex>();

        // �������� half ���ȸ�������h0 ��ʾ 0��h1 ��ʾ 1
        half h0 = half(0f), h1 = half(1f);

        // ����һ���µ� Vertex �ṹ�壬����ʼ������Ϊָ���棨�� Z �᷽��
        // ���߳�ʼ��Ϊ (1, 0, 0, -1)����ʾ�� X ��ķ���
        var vertex = new Vertex
        {
            normal = back(), // ����ָ���� (0, 0, -1)
            tangent = half4(h1, h0, h0, half(-1f)) // ��������Ϊ (1, 0, 0, -1)
        };

        // ���õ�һ�������λ��Ϊ (0, 0, 0) ����������������Ϊ (0, 0)
        vertex.position = 0f; // ����λ��Ϊ (0, 0, 0)
        vertex.texCoord0 = h0; // ��������Ϊ (0, 0)
        vertices[0] = vertex; // ���������ݴ��������һ��Ԫ��

        // ���õڶ��������λ��Ϊ (1, 0, 0) ����������������Ϊ (1, 0)
        vertex.position = right(); // ����λ��Ϊ (1, 0, 0)
        vertex.texCoord0 = half2(h1, h0); // ��������Ϊ (1, 0)
        vertices[1] = vertex; // ���������ݴ�������ڶ���Ԫ��

        // ���õ����������λ��Ϊ (0, 1, 0) ����������������Ϊ (0, 1)
        vertex.position = up(); // ����λ��Ϊ (0, 1, 0)
        vertex.texCoord0 = half2(h0, h1); // ��������Ϊ (0, 1)
        vertices[2] = vertex; // ���������ݴ������������Ԫ��

        // ���õ��ĸ������λ��Ϊ (1, 1, 0) ����������������Ϊ (1, 1)
        vertex.position = float3(1f, 1f, 0f); // ����λ��Ϊ (1, 1, 0)
        vertex.texCoord0 = h1; // ��������Ϊ (1, 1)
        vertices[3] = vertex; // ���������ݴ���������ĸ�Ԫ��


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
