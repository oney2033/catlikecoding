using Unity.Mathematics; // ���� Unity ����ѧ��
using UnityEngine; // ���� Unity ����Ŀ�

using static Unity.Mathematics.math; // ʹ����ѧ���еľ�̬����

namespace ProceduralMeshes.Generators // ��������������ɵ������ռ�
{
    // ����һ���������������������������ʵ�� IMeshGenerator �ӿ�
    public struct SharedTriangleGrid : IMeshGenerator
    {
        // ���㶥������������Ķ�������Ϊ (Resolution + 1) * (Resolution + 1)
        public int VertexCount => (Resolution + 1) * (Resolution + 1);

        // ����������������������Ϊ 6 * Resolution * Resolution��ÿ�������� 6 ��������2 �������Σ�
        public int IndexCount => 6 * Resolution * Resolution;

        // ������ҵ���ȣ���ʾ����������ʹ�ù����㣬ÿ�еĶ�������Ϊ Resolution + 1
        public int JobLength => Resolution + 1;

        // ��������ı߽磨Bound���������� (0, 0, 0)�����Ϊ 1 + 0.5 / Resolution�����Ϊ�ȱ������εĸ߶�
        public Bounds Bounds => new Bounds(
            Vector3.zero, new Vector3(1f + 0.5f / Resolution, 0f, sqrt(3f) / 2f)
        );

        public int Resolution { get; set; } // ����ķֱ���

        // ִ���������ɵ��߼������� z ����ǰ������У�S �� IMeshStreams �Ľṹ��ʵ��
        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            // vi �Ƕ���������ti ����������������ʼʱΪ��ǰ�еĵ�һ�����������͵�һ������������
            int vi = (Resolution + 1) * z, ti = 2 * Resolution * (z - 1);
            float xOffset = -0.25f; // ƫ��������ʼ��Ϊ -0.25f
            float uOffset = 0f; // U �������������ƫ����

            // �����ĸ����������ƫ������iA��iB��iC��iD �����������εĶ���
            int iA = -Resolution - 2, iB = -Resolution - 1, iC = -1, iD = 0;
            // �������������εĶ���������Ԫ�飬�ֱ���������� A �������� B
            var tA = int3(iA, iC, iD);
            var tB = int3(iA, iD, iB);

            // �����ǰ��Ϊ�����У�����ƫ�����������εĶ�������˳��
            if ((z & 1) == 1)
            {
                xOffset = 0.25f; // ƫ������Ϊ 0.25f
                uOffset = 0.5f / (Resolution + 0.5f); // ���� U �������������ƫ��
                tA = int3(iA, iC, iB); // ������һ�������εĶ���˳��
                tB = int3(iB, iC, iD); // �����ڶ��������εĶ���˳��
            }

            xOffset = xOffset / Resolution - 0.5f; // �� X ����ƫ�����ŵ���ȷ�ı�����Χ
            // ��ʼ��һ������������÷��߷���Ϊ Y �ᣬ������������
            var vertex = new Vertex();
            vertex.normal.y = 1f;
            vertex.tangent.xw = float2(1f, -1f);

            // ���õ�һ�������λ�ú��������꣬X ������Ϊ xOffset��Z ������ڵ�ǰ��������
            vertex.position.x = xOffset;
            vertex.position.z = ((float)z / Resolution - 0.5f) * sqrt(3f) / 2f; // ���� Z ���꣨�ȱ������εĸ߶ȣ�
            vertex.texCoord0.x = uOffset; // ���� U �������������ƫ��
            vertex.texCoord0.y = vertex.position.z / (1f + 0.5f / Resolution) + 0.5f; // ���� V ������������
            streams.SetVertex(vi, vertex); // ���ö�����Ϣ
            vi += 1; // ������������

            // ѭ������ÿһ�еĶ���
            for (int x = 1; x <= Resolution; x++, vi++, ti += 2)
            {
                vertex.position.x = (float)x / Resolution + xOffset; // ���� X ����Ķ���λ��
                vertex.texCoord0.x = x / (Resolution + 0.5f) + uOffset; // ���� U �������������
                streams.SetVertex(vi, vertex); // ���õ�ǰ������Ϣ

                // ������ǵ�һ�У�����������������
                if (z > 0)
                {
                    // ���õ�һ�������εĶ�������
                    streams.SetTriangle(ti + 0, vi + tA);
                    // ���õڶ��������εĶ�������
                    streams.SetTriangle(ti + 1, vi + tB);
                }
            }
        }
    }
}
