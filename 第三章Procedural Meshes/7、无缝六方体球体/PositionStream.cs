using System.Runtime.CompilerServices; // �������ڱ����Ż��������ռ�
using System.Runtime.InteropServices; // �������ڽṹ���ֵ������ռ�
using Unity.Collections; // ���� Unity �ļ��������ռ�
using Unity.Mathematics; // ���� Unity ����ѧ�����ռ�
using UnityEngine; // ���� Unity ���������ռ�
using UnityEngine.Rendering; // ���� Unity ��Ⱦ�����ռ�
using Unity.Collections.LowLevel.Unsafe; // ����ͼ���ļ��ϲ����������ռ�

namespace ProceduralMeshes.Streams // ���� ProceduralMeshes.Streams �����ռ�
{
    // ʵ�� IMeshStreams �ӿڵĽṹ�壬���ڹ�����λ������������������
    public struct PositionStream : IMeshStreams
    {
        // ����������ȫ�Լ�飬���������
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float3> stream0; // ����λ����

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TriangleUInt16> triangles; // ������������

        // �����������ݵķ���
        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
        {
            // ����һ�����������������Ե�����
            var descriptor = new NativeArray<VertexAttributeDescriptor>(
                1, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3); // ���ö������Ե�ά��Ϊ3��x, y, z��

            // ��������Ķ��㻺��������
            meshData.SetVertexBufferParams(vertexCount, descriptor);
            descriptor.Dispose(); // �ͷ��������������Դ

            // �����������������������
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            // ��������������������Ͳ���
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(
                 0, new SubMeshDescriptor(0, indexCount)
                 {
                     bounds = bounds, // ����������ı߽�
                     vertexCount = vertexCount // ���ö�������
                 },
                 // �Ż����ܣ������¼���߽�Ͳ���֤����
                 MeshUpdateFlags.DontRecalculateBounds |
                 MeshUpdateFlags.DontValidateIndices
             );

            // ��ȡ�������ݲ������ stream0
            stream0 = meshData.GetVertexData<float3>(); // ��ȡ����λ������
            // ��ȡ�������ݣ������½���Ϊ TriangleUInt16 ��ʽ
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2); // ��ȡ��������������
        }

        // ���õ����������ݵķ���
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, Vertex vertex)
        {
            stream0[index] = vertex.position; // ���ö���λ��
        }

        // �������������ݵķ���
        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle; // ��������������
    }
}
