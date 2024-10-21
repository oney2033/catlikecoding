using Unity.Mathematics; // ������ѧ��صĿ�
using UnityEngine; // ����Unity����Ŀ�

using static Unity.Mathematics.math; // ������ѧ���еľ�̬����

namespace ProceduralMeshes.Generators // �����ռ䣬��ʾ���ɳ��������ģ��
{
    // ���巽��������������ʵ��IMeshGenerator�ӿ�
    public struct SquareGrid : IMeshGenerator
    {
        // ���㶥��������ÿ��������4�����㣬�ֱ���ΪResolution
        public int VertexCount => 4 * Resolution * Resolution;
        // ��������������ÿ��������6�����������������Σ����ֱ���ΪResolution
        public int IndexCount => 6 * Resolution * Resolution;
        // ��ҵ���ȣ���ʾҪ���������
        public int JobLength => Resolution;
        // ����ı߽磬������(0,0,0)����Ⱥ����Ϊ1
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));
        public int Resolution { get; set; } // ����ķֱ���

        // ִ���������ɵ��߼�
        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            // ���㵱ǰ�еĶ�������������������
            int vi = 4 * Resolution * z; // ��������
            int ti = 2 * Resolution * z; // ����������

            // ������ǰ���е�ÿһ��
            for (int x = 0; x < Resolution; x++, vi += 4, ti += 2)
            {
                // ���㵱ǰ�����x��z����
                var xCoordinates = float2(x, x + 1f) / Resolution - 0.5f; // x����
                var zCoordinates = float2(z, z + 1f) / Resolution - 0.5f; // z����

                // ����һ������ʵ��
                var vertex = new Vertex();
                vertex.normal.y = 1f; // ��������
                vertex.tangent.xw = float2(1f, -1f); // ���ߵ�x��w����
                vertex.position.x = xCoordinates.x; // ���ö���λ��x
                vertex.position.z = zCoordinates.x; // ���ö���λ��z
                streams.SetVertex(vi + 0, vertex); // �洢��һ������

                vertex.position.x = xCoordinates.y; // ������һ������λ��x
                vertex.texCoord0 = float2(1f, 0f); // ������������
                streams.SetVertex(vi + 1, vertex); // �洢�ڶ�������

                vertex.position.x = xCoordinates.x; // ������һ������λ��x
                vertex.position.z = zCoordinates.y; // ���ö���λ��z
                vertex.texCoord0 = float2(0f, 1f); // ������������
                streams.SetVertex(vi + 2, vertex); // �洢����������

                vertex.position.x = xCoordinates.y; // ������һ������λ��x
                vertex.texCoord0 = 1f; // ������������
                streams.SetVertex(vi + 3, vertex); // �洢���ĸ�����

                // �������������ε�����
                streams.SetTriangle(ti + 0, vi + int3(0, 2, 1)); // ��һ��������
                streams.SetTriangle(ti + 1, vi + int3(1, 2, 3)); // �ڶ���������
            }
        }
    }
}
