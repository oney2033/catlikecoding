using System.Runtime.CompilerServices; // �������ڱ����Ż��������ռ�
using System.Runtime.InteropServices; // �������ڽṹ���ֵ������ռ�
using Unity.Collections; // ���� Unity �ļ��������ռ�
using Unity.Mathematics; // ���� Unity ����ѧ�����ռ�
using UnityEngine; // ���� Unity ���������ռ�
using UnityEngine.Rendering; // ���� Unity ��Ⱦ�����ռ�
using Unity.Collections.LowLevel.Unsafe; // ���벻��ȫ���ϵ������ռ�

namespace ProceduralMeshes.Streams // ���� ProceduralMeshes.Streams �����ռ�
{
    // ʵ�� IMeshStreams �ӿڵĽṹ�壬���ڹ�����������
    public struct MultiStream : IMeshStreams
    {
        // ����������ȫ�Լ�飬���������
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float3> stream0, stream1; // ����λ�úͷ�����

        [NativeDisableContainerSafetyRestriction]
        NativeArray<float4> stream2; // ������

        [NativeDisableContainerSafetyRestriction]
        NativeArray<float2> stream3; // ����������

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TriangleUInt16> triangles; // ������������

        // �����������ݵķ���
        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
        {
            // ����һ��������������������
            var descriptor = new NativeArray<VertexAttributeDescriptor>(
                4, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3); // ����λ����
            descriptor[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3, stream: 1 // ������
            );
            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4, stream: 2 // ������
            );
            descriptor[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2, stream: 3 // ����������
            );
            // ��������Ķ��㻺��������
            meshData.SetVertexBufferParams(vertexCount, descriptor);
            descriptor.Dispose(); // �ͷ��������������Դ

            // �����������������������
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            // ������������������ز���
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(
                 0, new SubMeshDescriptor(0, indexCount)
                 {
                     bounds = bounds, // ���ñ߽�
                     vertexCount = vertexCount // ���ö�������
                 },
                 // �����¼���߽����֤�������������
                 MeshUpdateFlags.DontRecalculateBounds |
                 MeshUpdateFlags.DontValidateIndices
             );
            // ��ȡ�������������
            stream0 = meshData.GetVertexData<float3>(); // ��ȡ����λ������
            stream1 = meshData.GetVertexData<float3>(1); // ��ȡ��������
            stream2 = meshData.GetVertexData<float4>(2); // ��ȡ��������
            stream3 = meshData.GetVertexData<float2>(3); // ��ȡ������������
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2); // ��ȡ��������������
        }

        // ���ö������ݵķ���
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, Vertex vertex)
        {
            stream0[index] = vertex.position; // ���ö���λ��
            stream1[index] = vertex.normal; // ���÷���
            stream2[index] = vertex.tangent; // ��������
            stream3[index] = vertex.texCoord0; // ������������
        }

        // �������������ݵķ���
        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle; // ��������������
    }
}
