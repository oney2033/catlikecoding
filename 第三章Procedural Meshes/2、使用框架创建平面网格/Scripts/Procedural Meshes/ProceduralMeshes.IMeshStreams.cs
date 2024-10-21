using Unity.Mathematics; // ���� Unity ��ѧ�⣬���ڴ�����ѧ����
using UnityEngine; // ���� Unity ����ĺ��Ŀ�

namespace ProceduralMeshes // ����һ�������ռ䣬��֯�����������صĴ���
{

    // ������һ�� IMeshStreams �ӿڣ����ڴ�������������
    public interface IMeshStreams
    {
        // ������������Ļ������ݽṹ
        // meshData: ��Ҫ��ʼ������������
        // bounds: ����ı߽�����ڶ�������İ�Χ��Χ
        // vertexCount: ���������
        // indexCount: ����������������
        void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount);

        // ����ָ���������Ķ�������
        // index: ���������
        // data: ����ľ�������
        void SetVertex(int index, Vertex data);

        // ��������������
        // index: �����ε�����
        // triangle: ������������ɵ������� (int3 ��ʾ3���������������)
        void SetTriangle(int index, int3 triangle);
    }

}
