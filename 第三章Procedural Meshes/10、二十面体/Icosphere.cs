using Unity.Mathematics; // ������ѧ��صĿ�
using UnityEngine; // ����Unity����Ŀ�

using static Unity.Mathematics.math; // ������ѧ���еľ�̬����

namespace ProceduralMeshes.Generators // �����ռ䣬��ʾ���ɳ��������ģ��
{
    // ���巽��������������ʵ��IMeshGenerator�ӿ�
    public struct Icosphere : IMeshGenerator
    {
        // ���㶥��������ÿ��������4�����㣬�ֱ���ΪResolution
        public int VertexCount => 5 * ResolutionV * Resolution + 2;
        // ��������������ÿ��������6�����������������Σ����ֱ���ΪResolution
        public int IndexCount => 6 * 5 * ResolutionV * Resolution;
        // ��ҵ���ȣ���ʾҪ���������
        public int JobLength => 5 * Resolution;
        // ����ı߽磬������(0,0,0)����Ⱥ����Ϊ1
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        public int Resolution { get; set; } // ����ķֱ���

        int ResolutionV => 2 * Resolution;

        // ����һ���ṹ�壬���ڱ�ʾ�������ÿһ��
        struct Strip
        {
            public int id; // ���������ID
            public float3 lowLeftCorner, lowRightCorner, 
                          highLeftCorner, highRightCorner;

        }

        static float3 GetCorner(int id, int ySign) => float3(
             0.4f * sqrt(5f) * sin(0.2f * PI * id),
             ySign * 0.2f * sqrt(5f),
             -0.4f * sqrt(5f) * cos(0.2f * PI * id)
         );

        // ����ID��ȡ�������һ��
        static Strip GetStrip(int id) => id switch
        {
            0 => CreateStrip(0),
            1 => CreateStrip(1),
            2 => CreateStrip(2),
            3 => CreateStrip(3),
            _ => CreateStrip(4)
        };

        static Strip CreateStrip(int id) => new Strip
        {
            id = id,
            lowLeftCorner = GetCorner(2 * id, -1),
            lowRightCorner = GetCorner(id == 4 ? 0 : 2 * id + 2, -1),
            highLeftCorner = GetCorner(id == 0 ? 9 : 2 * id - 1, 1),
            highRightCorner = GetCorner(2 * id + 1, 1)
        };

        // ִ���������ɵ��߼�
        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {
            int u = i / 5;
            Strip strip = GetStrip(i - 5 * u);
            int vi = ResolutionV * (Resolution * strip.id + u) + 2;
            int ti = 2 * ResolutionV * (Resolution * strip.id + u);// ���㵱ǰ����������

            bool firstColumn = u == 0;
            int4 quad = int4(
                vi,
                firstColumn ? 0 : vi - ResolutionV,
                firstColumn ?
                    strip.id == 0 ?
                        4 * ResolutionV * Resolution + 2 :
                        vi - ResolutionV * (Resolution + u) :
                    vi - ResolutionV + 1,
                vi + 1
            );

            u += 1;

            float3 columnBottomDir = strip.lowRightCorner - down();
            float3 columnBottomStart = down() + columnBottomDir * u / Resolution;
            float3 columnBottomEnd = strip.lowLeftCorner + columnBottomDir * u / Resolution;

            float3 columnLowDir = strip.highRightCorner - strip.lowLeftCorner;
            float3 columnLowStart =
                strip.lowRightCorner + columnLowDir * ((float)u / Resolution - 1f);
            float3 columnLowEnd = strip.lowLeftCorner + columnLowDir * u / Resolution;

            float3 columnTopDir = up() - strip.highLeftCorner;
            float3 columnTopStart =
                strip.highRightCorner + columnTopDir * ((float)u / Resolution - 1f);
            float3 columnTopEnd = strip.highLeftCorner + columnTopDir * u / Resolution;

            float3 columnHighDir = strip.highRightCorner - strip.lowLeftCorner;
            float3 columnHighStart = strip.lowLeftCorner + columnHighDir * u / Resolution;
            float3 columnHighEnd = strip.highLeftCorner + columnHighDir * u / Resolution;

            var vertex = new Vertex(); // ����һ������ʵ��
            if (i == 0)
            {
                vertex.position = down();
                streams.SetVertex(0, vertex);
                vertex.position = up();
                streams.SetVertex(1, vertex);
            }
            vertex.position = normalize(columnBottomStart);

            streams.SetVertex(vi, vertex);

            vi += 1;

            // ������ǰ���е�ÿһ��
            for (int v = 1; v < ResolutionV; v++, vi++, ti += 2)
            {
                if (v <= Resolution - u) {
					vertex.position =
						lerp(columnBottomStart, columnBottomEnd, (float)v / Resolution);
				}
				else if (v < Resolution) {
                    vertex.position =
                        lerp(columnLowStart, columnLowEnd, (float)v / Resolution);
                }
				else if (v <= ResolutionV - u) {
                    vertex.position =
                        lerp(columnHighStart, columnHighEnd, (float)v / Resolution - 1f);
                }
				else {
					vertex.position =
						lerp(columnTopStart, columnTopEnd, (float)v / Resolution - 1f);
				}

                vertex.position = normalize(vertex.position);
                streams.SetVertex(vi, vertex);
                // �������������ε�����

                streams.SetTriangle(ti + 0, quad.xyz);
                streams.SetTriangle(ti + 1, quad.xzw);

                quad.y = quad.z;
                quad +=
                    int4(1, 0, firstColumn && v <= Resolution - u ? ResolutionV : 1, 1);
            }
            if (!firstColumn)
            {
                quad.z = ResolutionV * Resolution * (strip.id == 0 ? 5 : strip.id) -
                    Resolution + u + 1;
            }
            quad.w = u < Resolution ? quad.z + 1 : 1;

            streams.SetTriangle(ti + 0, quad.xyz);
            streams.SetTriangle(ti + 1, quad.xzw);
        }
    }
}
