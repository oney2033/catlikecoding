using Unity.Mathematics; // ������ѧ��صĿ�
using UnityEngine; // ����Unity����Ŀ�

using static Unity.Mathematics.math; // ������ѧ���еľ�̬����

namespace ProceduralMeshes.Generators // �����ռ䣬��ʾ���ɳ��������ģ��
{
    // ����UV������������ʵ��IMeshGenerator�ӿ�
    public struct UVSphere : IMeshGenerator
    {
        // ���㶥����������������Ϊ (Resolution + 1) * (Resolution + 1) - 2
        public int VertexCount => (ResolutionU + 1) * (ResolutionV + 1) - 2;

        // ��������������ÿ��������6�����������������Σ�����������Ϊ 6 * Resolution * Resolution
        public int IndexCount => 6 * ResolutionU * (ResolutionV - 1);

        // ��ҵ���ȣ���ʾ��Ҫ���������
        public int JobLength => ResolutionU + 1;

        // ����ı߽磬����һ�������� (0, 0, 0)����Ⱥ����Ϊ 1 �ı߽��
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));

        // ����ķֱ��ʣ�Resolution ��������������ľ�ϸ��
        public int Resolution { get; set; }

        // ResolutionV ����γ�ȷ���ķֱ��ʣ����� Resolution ������
        int ResolutionV => 2 * Resolution;

        // ResolutionU ���ؾ��ȷ���ķֱ��ʣ����� Resolution ���ı�
        int ResolutionU => 4 * Resolution;

        // ִ�г�������������߼����������� u ��ʾ��ǰ������У�S ��ʵ�� IMeshStreams �Ľṹ��
        public void ExecuteRegular<S>(int u, S streams) where S : struct, IMeshStreams
        {
            // vi �Ƕ���������ti ����������������ǰ�еĵ�һ������͵�һ������������
            int vi = (ResolutionV + 1) * u - 2, ti = 2 * (ResolutionV - 1) * (u - 1);

            // ��ʼ��һ����������趨���߷���Y�ᣩ������
            var vertex = new Vertex();
            vertex.position.y = vertex.normal.y = -1f;

            // ʹ�� sincos �����������ߵ� x �� z ������������ w Ϊ -1
            sincos(
                2f * PI * (u - 0.5f) / ResolutionU,
                out vertex.tangent.z, out vertex.tangent.x
            );
            vertex.tangent.w = -1f;

            // ������������
            vertex.texCoord0.x = (u - 0.5f) / ResolutionU;
            streams.SetVertex(vi, vertex); // ���õ�ǰ������Ϣ

            // ���ö����Ķ���
            vertex.position.y = vertex.normal.y = 1f;
            vertex.texCoord0.y = 1f;
            streams.SetVertex(vi + ResolutionV, vertex);
            vi += 1; // ��������

            // ����Բ���ϸ�������Һ�����ֵ���õ���λԲ������
            float2 circle;
            sincos(2f * PI * u / ResolutionU, out circle.x, out circle.y);
            vertex.tangent.xz = circle.yx; // ���� y �� x
            circle.y = -circle.y; // ��ת y ����

            vertex.texCoord0.x = (float)u / ResolutionU;

            // �ж��Ƿ���Ҫƫ�������εĶ�������
            int shiftLeft = (u == 1 ? 0 : -1) - ResolutionV;

            // ���������εĶ�������
            streams.SetTriangle(ti, vi + int3(-1, shiftLeft, 0));
            ti += 1;

            // ѭ��Ϊÿ�����ɶ���
            for (int v = 1; v < ResolutionV; v++, vi++) //, ti += 2)
            {
                // ����γ�ȷ����ϵ�Բ�ΰ뾶
                sincos(
                     PI + PI * v / ResolutionV,
                     out float circleRadius, out vertex.position.y
                 );

                // ���㵱ǰ�����λ��
                vertex.position.xz = circle * -circleRadius;
                vertex.normal = vertex.position; // ���ߺͶ���λ����ͬ
                vertex.texCoord0.y = (float)v / ResolutionV; // ������������
                streams.SetVertex(vi, vertex); // ���ö�����Ϣ

                // ������ǵ�һ�У������������εĶ�������
                if (v > 1)
                {
                    streams.SetTriangle(ti + 0, vi + int3(shiftLeft - 1, shiftLeft, -1));
                    streams.SetTriangle(ti + 1, vi + int3(-1, shiftLeft, 0));
                    ti += 2;
                }
            }
            // ���һ��������
            streams.SetTriangle(ti, vi + int3(shiftLeft - 1, 0, -1));
        }

        // ��������ӷ�ĺ��������ɽӷ첿�ֵĶ���
        public void ExecuteSeam<S>(S streams) where S : struct, IMeshStreams
        {
            // ��ʼ��һ���������
            var vertex = new Vertex();

            // �������߷���
            vertex.tangent.x = 1f;
            vertex.tangent.w = -1f;

            // ѭ��Ϊ�ӷ����ɶ���
            for (int v = 1; v < ResolutionV; v++) //, ti += 2)
            {
                // ����γ�ȷ����ϵ�λ�úͷ���
                sincos(
                     PI + PI * v / ResolutionV,
                     out vertex.position.z, out vertex.position.y
                 );
                vertex.normal = vertex.position; // ���ߺͶ���λ����ͬ
                vertex.texCoord0.y = (float)v / ResolutionV; // ������������
                streams.SetVertex(v - 1, vertex); // ���ö�����Ϣ
            }
        }

        // ��ִ�к����������������ö�Ӧ�Ĵ�����
        public void Execute<S>(int u, S streams) where S : struct, IMeshStreams
        {
            if (u == 0)
            {
                ExecuteSeam(streams); // ����ӷ첿��
            }
            else
            {
                ExecuteRegular(u, streams); // ���������߼�
            }
        }
    }
}
