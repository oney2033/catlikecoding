using System.Runtime.CompilerServices; // ����ϵͳ������ʱ���������������Ż�����ִ��
using System.Runtime.InteropServices;  // ����ϵͳ�Ļ������������ڶ������ݲ���
using Unity.Collections; // ���� Unity �ļ��Ͽ⣬��Ҫ����ԭ������Ĺ���
using Unity.Mathematics; // ���� Unity ����ѧ�⣬������ѧ����
using UnityEngine; // ���� Unity ������Ŀ�
using UnityEngine.Rendering; // ���� Unity ����Ⱦϵͳ
using Unity.Collections.LowLevel.Unsafe; // ���� Unity �ײ㼯�ϰ�ȫ����

namespace ProceduralMeshes.Streams // ���������ռ䣬��֯�����������صĴ�����
{
    // ʵ�� IMeshStreams �ӿڵĽṹ�壬���ڴ���һ����������������
    public struct SingleStream : IMeshStreams
    {
        // �ڲ��ṹ�壬������ÿ�������������������λ�á����ߡ����ߺ���������
        [StructLayout(LayoutKind.Sequential)] // ָ�����ݲ���Ϊ˳�򲼾�
        struct Stream0
        {
            public float3 position, normal; // �����λ�úͷ��ߣ���Ϊ��ά����
            public float4 tangent; // ��������ߣ���ά����
            public float2 texCoord0; // ����ĵ�һ���������꣬��ά����
        }

        // ���������İ�ȫ���ƣ��������ܣ���Ҳ�������ڴ����ķ��գ�
        [NativeDisableContainerSafetyRestriction]
        NativeArray<Stream0> stream0; // �洢�������ݵ�ԭ������

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TriangleUInt16> triangles; // �洢���������ݵ�ԭ������

        // ʵ�ֽӿڵ� Setup ���������ڳ�ʼ����������
        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
        {
            // ����һ���������������������飬���ڶ��嶥�㻺�����Ľṹ
            var descriptor = new NativeArray<VertexAttributeDescriptor>(
                4, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            // ���ö���λ�����ԣ���ά����
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
            // ���÷������ԣ���ά����
            descriptor[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3
            );
            // �����������ԣ���ά����
            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4
            );
            // ���������������ԣ���ά����
            descriptor[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2
            );
            // ���ö��㻺��������
            meshData.SetVertexBufferParams(vertexCount, descriptor);
            // �ͷ����������ڴ�
            descriptor.Dispose();

            // ��������������������ʹ�� 16 λ����
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            // �������������������Ϊ 1
            meshData.subMeshCount = 1;
            // ��������������ԣ������߽�Ͷ�������
            meshData.SetSubMesh(
                0, new SubMeshDescriptor(0, indexCount)
                {
                    bounds = bounds, // ����������ı߽�
                    vertexCount = vertexCount // ��������
                },
                // �����¼���߽����֤���������������
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontValidateIndices
            );
            // ��ȡ����������������洢�� stream0 ��
            stream0 = meshData.GetVertexData<Stream0>();
            // ��ȡ������������������ת��Ϊ TriangleUInt16 ��ʽ
            triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }

        // ��������ָ���������Ķ�������
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // �Ż���������
        public void SetVertex(int index, Vertex vertex) => stream0[index] = new Stream0
        {
            position = vertex.position, // ���ö����λ��
            normal = vertex.normal,     // ���ö���ķ���
            tangent = vertex.tangent,   // ���ö��������
            texCoord0 = vertex.texCoord0 // ���ö������������
        };

        // ��������ָ��������������������
        public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle;
    }
}
