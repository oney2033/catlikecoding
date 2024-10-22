using Unity.Mathematics;

namespace ProceduralMeshes
{
    // ����ṹ�壬������λ�á����ߡ����ߺ��������������
    public struct Vertex
    {
        // ����λ�ã���ʾ��������ά�ռ��е�λ��
        public float3 position; // float3 ��ʾһ������ X, Y, Z �����ά����

        // ���㷨�ߣ���ʾ���㴦�ķ��߷������ڹ��ռ���
        public float3 normal;   // float3 ��ʾһ����ά����������ȷ�����߷��䷽��

        // �������ߣ���ʾ�������߷��򣬳����ڷ�����ͼ
        public float4 tangent;  // float4 ��ʾһ����ά������ǰ��������Ϊ���򣬵��ĸ�Ϊ����

        // ����ĵ�һ���������꣬��������ӳ��
        public float2 texCoord0; // float2 ��ʾ��ά��������Ӧ U, V ��������
    }
}
