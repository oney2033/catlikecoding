using Unity.Mathematics; // ���� Unity ����ѧ�⣬������ѧ����
using UnityEngine; // ���� Unity �����
using static Unity.Mathematics.math; // ��̬���� math ���еĺ�����ʹ�����ֱ�ӵ���

namespace ProceduralMeshes.Generators // �����ռ䣬�������ֲ�ͬ�Ĵ���ģ��
{
    // ����һ����������ƽ������������Ľṹ�壬�̳��� IMeshGenerator �ӿ�
    public struct FlatHexagonGrid : IMeshGenerator
    {
        // ����������ÿ����������7�����㣬��������Ϊ 7 * Resolution * Resolution
        public int VertexCount => 7 * Resolution * Resolution;

        // ����������ÿ����������6�������Σ�ÿ����������3������������������Ϊ 18 * Resolution * Resolution
        public int IndexCount => 18 * Resolution * Resolution;

        // ��ҵ���ȣ���ʾÿһ�е������������� Resolution ��
        public int JobLength => Resolution;

        // ��������ı߽������λ�� (0, 0, 0) �����ߴ��ɷֱ��ʾ���
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(
                0.75f + 0.25f / Resolution,  // ������
                0f,                          // ����ĸ߶�
                (Resolution > 1 ? 0.5f + 0.25f / Resolution : 0.5f) * sqrt(3f)  // �������
            ));

        // ����ֱ������ԣ���ʾ���ɵ������ε��������ܶ�
        public int Resolution { get; set; }

        // ���ĺ�����ִ������������߼������� x ��ʾ��ǰ������У�S ��ʵ�� IMeshStreams �ӿڵĽṹ��
        public void Execute<S>(int x, S streams) where S : struct, IMeshStreams
        {
            // vi ��ʾ��ǰ�������ʼ������ti ��ʾ��ǰ�����ε���ʼ����
            int vi = 7 * Resolution * x, ti = 6 * Resolution * x;

            // h �������εĸ߶�ƫ���������� sqrt(3) / 4�����ڼ��㶥��λ��
            float h = sqrt(3f) / 4f;

            // ����ƫ�����������ĵ�ƫ����
            float2 centerOffset = 0f;

            // ����ֱ��ʴ��� 1���������ĵ�ƫ������ʹ�����γ��ֽ�������
            if (Resolution > 1)
            {
                centerOffset.x = -0.375f * (Resolution - 1);
                centerOffset.y = (((x & 1) == 0 ? 0.5f : 1.5f) - Resolution) * h;
            }

            // ѭ��������ǰ���е����������Σ�z ��ʾ������
            for (int z = 0; z < Resolution; z++, vi += 7, ti += 6)
            {
                // ���㵱ǰ����������λ�ã�����������ƫ��
                var center = (float2(0.75f * x, 2f * h * z) + centerOffset) / Resolution;

                // ���������ζ���� x �� z ����
                var xCoordinates =
                    center.x + float4(-0.5f, -0.25f, 0.25f, 0.5f) / Resolution;
                var zCoordinates = center.y + float2(h, -h) / Resolution;

                // ����һ������ṹ�壬���ڴ洢ÿ��������������
                var vertex = new Vertex();
                vertex.normal.y = 1f; // ���ö��㷨�߷���
                vertex.tangent.xw = float2(1f, -1f); // ���ö������߷���

                // �������������Ķ��������
                vertex.position.xz = center;
                vertex.texCoord0 = 0.5f; // ���ĵ����������
                streams.SetVertex(vi + 0, vertex);

                // �����������������λ����Ϣ����������
                vertex.position.x = xCoordinates.x;
                vertex.texCoord0.x = 0f;
                streams.SetVertex(vi + 1, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.position.z = zCoordinates.x;
                vertex.texCoord0 = float2(0.25f, 0.5f + h);
                streams.SetVertex(vi + 2, vertex);

                vertex.position.x = xCoordinates.z;
                vertex.texCoord0.x = 0.75f;
                streams.SetVertex(vi + 3, vertex);

                vertex.position.x = xCoordinates.w;
                vertex.position.z = center.y;
                vertex.texCoord0 = float2(1f, 0.5f);
                streams.SetVertex(vi + 4, vertex);

                vertex.position.x = xCoordinates.z;
                vertex.position.z = zCoordinates.y;
                vertex.texCoord0 = float2(0.75f, 0.5f - h);
                streams.SetVertex(vi + 5, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.texCoord0.x = 0.25f;
                streams.SetVertex(vi + 6, vertex);

                // ���������ε����������Σ�����ÿ�������εĶ�������
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
