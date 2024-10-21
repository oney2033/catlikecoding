using UnityEngine; // ���� Unity ���������ռ䣬�ṩ Unity �ĺ��Ĺ��ܣ����� Bounds ���͡�

namespace ProceduralMeshes // ����һ�������ռ䣬��Ϊ ProceduralMeshes��������֯��ص��������ɴ��롣
{
    // ����һ�������������ӿ� IMeshGenerator
    public interface IMeshGenerator
    {
        // Execute ���������ݸ��������� i��ִ���������ɲ���
        // ���� S ������һ���ṹ�壬����ʵ���� IMeshStreams �ӿ�
        void Execute<S>(int i, S streams) where S : struct, IMeshStreams;

        // ���ԣ�����Ķ�������
        int VertexCount { get; }

        // ���ԣ�������������������ڶ��������Σ�
        int IndexCount { get; }

        // ���ԣ�������Job���ĳ��ȣ�ͨ���� Resolution ��أ���ʾ�ж���������Ҫִ��
        int JobLength { get; }

        // ���ԣ�����İ�Χ�У�Bounds�������ڶ�������ı߽�
        Bounds Bounds { get; }

        // ���ԣ��������ɵķֱ��ʣ����ڿ��������ϸ�ڳ̶�
        int Resolution { get; set; }
    }
}
