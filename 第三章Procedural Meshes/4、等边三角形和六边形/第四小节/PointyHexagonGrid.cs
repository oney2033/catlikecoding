using Unity.Mathematics; // ������ѧ��صĿ�
using UnityEngine; // ����Unity����Ŀ�

using static Unity.Mathematics.math; // ������ѧ���еľ�̬����

namespace ProceduralMeshes.Generators // �����ռ䣬��ʾ���ɳ��������ģ��
{
    // ����������������������ʵ��IMeshGenerator�ӿ�
    public struct PointyHexagonGrid : IMeshGenerator
    {
        // ���㶥��������ÿ����������7�����㣬���� Resolution * Resolution ��������
        public int VertexCount => 7 * Resolution * Resolution;

        // ��������������ÿ����������6�������Σ�ÿ����������Ҫ3������
        public int IndexCount => 18 * Resolution * Resolution;

        // ��ҵ���ȣ���ʾ��Ҫ�����������ÿһ���� Resolution ��������
        public int JobLength => Resolution;

        // ����ı߽磬����һ�������� (0, 0, 0)����Ⱥ����Ϊ 1 �ı߽��
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(
            (Resolution > 1 ? 0.5f + 0.25f / Resolution : 0.5f) * sqrt(3f), // X�᷽���С
            0f, // Y�᷽���С
            0.75f + 0.25f / Resolution // Z�᷽���С
        ));

        public int Resolution { get; set; } // ����ķֱ���

        // ִ�����������߼��ĺ��������� z ��ʾ��ǰ������У�S ��ʵ�� IMeshStreams �Ľṹ��
        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            // vi �Ƕ���������ti ����������������ǰ�еĵ�һ������͵�һ������������
            int vi = 7 * Resolution * z, ti = 6 * Resolution * z;

            // ���������εĸ߶�
            float h = sqrt(3f) / 4f;
            // ��ʼ������ƫ����
            float2 centerOffset = 0f;

            // ����ֱ��ʴ���1���������ĵ�ƫ��
            if (Resolution > 1)
            {
                centerOffset.x = (((z & 1) == 0 ? 0.5f : 1.5f) - Resolution) * h;
                centerOffset.y = -0.375f * (Resolution - 1);
            }

            // ѭ��Ϊÿ�����ɶ���
            for (int x = 0; x < Resolution; x++, vi += 7, ti += 6)
            {
                // ���������ε����ĵ�λ�ã���������Էֱ��ʽ��й�һ��
                var center = (float2(2f * h * x, 0.75f * z) + centerOffset) / Resolution;

                // ���������ε�X�����Z���������
                var xCoordinates = center.x + float2(-h, h) / Resolution;
                var zCoordinates = center.y + float4(-0.5f, -0.25f, 0.25f, 0.5f) / Resolution;

                var vertex = new Vertex(); // ��ʼ��һ���������
                vertex.normal.y = 1f; // �趨���߷���Y�����ϣ�
                vertex.tangent.xw = float2(1f, -1f); // ���ö�������

                // �������Ķ���
                vertex.position.xz = center; // XZƽ��λ��
                vertex.texCoord0 = 0.5f; // ���Ķ�����������Ϊ (0.5, 0.5)
                streams.SetVertex(vi + 0, vertex); // ���õ�һ�����Ķ���

                // ���������ε���������
                vertex.position.z = zCoordinates.x;
                vertex.texCoord0.y = 0f; // ���������Y����Ϊ0
                streams.SetVertex(vi + 1, vertex); // ���õڶ�������

                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.y;
                vertex.texCoord0 = float2(0.5f - h, 0.25f); // �������������
                streams.SetVertex(vi + 2, vertex); // ���õ���������

                vertex.position.z = zCoordinates.z;
                vertex.texCoord0.y = 0.75f; // ���������Y����Ϊ0.75
                streams.SetVertex(vi + 3, vertex); // ���õ��ĸ�����

                vertex.position.x = center.x;
                vertex.position.z = zCoordinates.w;
                vertex.texCoord0 = float2(0.5f, 1f); // �������������Ϊ (0.5, 1)
                streams.SetVertex(vi + 4, vertex); // ���õ��������

                vertex.position.x = xCoordinates.y;
                vertex.position.z = zCoordinates.z;
                vertex.texCoord0 = float2(0.5f + h, 0.75f); // �������������
                streams.SetVertex(vi + 5, vertex); // ���õ���������

                vertex.position.z = zCoordinates.y;
                vertex.texCoord0.y = 0.25f; // ���������Y����Ϊ0.25
                streams.SetVertex(vi + 6, vertex); // ���õ��߸�����

                // ����ÿ�������ε�����������������ʹ��֮ǰ���õĶ���
                streams.SetTriangle(ti + 0, vi + int3(0, 1, 2));
                streams.SetTriangle(ti + 1, vi + int3(0, 2, 3));
                streams.SetTriangle(ti + 2, vi + int3(0, 3, 4));
                streams.SetTriangle(ti + 3, vi + int3(0, 4, 5));
                streams.SetTriangle(ti + 4, vi + int3(0, 5, 6));
                streams.SetTriangle(ti + 5, vi + int3(0, 6, 1));
            }
        }
    }
}
