using Unity.Mathematics; // ������ѧ��صĿ�
using UnityEngine; // ����Unity����Ŀ�

using static Unity.Mathematics.math; // ������ѧ���еľ�̬����

namespace ProceduralMeshes.Generators // �����ռ䣬��ʾ���ɳ��������ģ��
{
    // ���巽��������������ʵ��IMeshGenerator�ӿ�
    public struct SharedSquareGrid : IMeshGenerator
    {
        // ���㶥����������������Ķ�������Ϊ (Resolution + 1) * (Resolution + 1)
        public int VertexCount => (Resolution + 1) * (Resolution + 1);

        // ��������������ÿ��������6�����������������Σ�����������Ϊ 6 * Resolution * Resolution
        public int IndexCount => 6 * Resolution * Resolution;

        // ��ҵ���ȣ���ʾ��Ҫ���������������ʹ�ù����㣬ÿ�еĶ�����Ϊ Resolution + 1
        public int JobLength => Resolution + 1;

        // ����ı߽磬����һ�������� (0, 0, 0)����Ⱥ����Ϊ 1 �ı߽��
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));

        public int Resolution { get; set; } // ����ķֱ���

        // ִ�����������߼��ĺ��������� z ��ʾ��ǰ������У�S ��ʵ�� IMeshStreams �Ľṹ��
        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            // vi �Ƕ���������ti ����������������ǰ�еĵ�һ������͵�һ������������
            int vi = (Resolution + 1) * z, ti = 2 * Resolution * (z - 1);

            // ��ʼ��һ����������趨���߷���Y�ᣩ������
            var vertex = new Vertex();
            vertex.normal.y = 1f;
            vertex.tangent.xw = float2(1f, -1f);

            // ���õ�һ�������λ�ú��������꣬X ������Ϊ -0.5f
            vertex.position.x = -0.5f;
            vertex.position.z = (float)z / Resolution - 0.5f; // Z ����λ��
            vertex.texCoord0.y = (float)z / Resolution; // ��������Y��
            streams.SetVertex(vi, vertex); // �趨����
            vi += 1; // ��������

            // ѭ��Ϊÿ�����ɶ���
            for (int x = 1; x <= Resolution; x++, vi++, ti += 2)
            {
                vertex.position.x = (float)x / Resolution - 0.5f; // X����λ��
                vertex.texCoord0.x = (float)x / Resolution; // ��������X��
                streams.SetVertex(vi, vertex); // �趨����

                // ������ǵ�һ�У�������������
                if (z > 0)
                {
                    // ���õ�һ�������Σ�ʹ�ù���Ķ�������
                    streams.SetTriangle(
                        ti + 0, vi + int3(-Resolution - 2, -1, -Resolution - 1)
                    );
                    // ���õڶ���������
                    streams.SetTriangle(
                        ti + 1, vi + int3(-Resolution - 1, -1, 0)
                    );
                }
            }
        }
    }
}
