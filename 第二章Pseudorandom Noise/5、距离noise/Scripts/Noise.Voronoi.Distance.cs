using Unity.Mathematics;           // ���� Unity.Mathematics �⣬������ѧ����
using static Unity.Mathematics.math; // ���� math ��̬������ʹ�ÿ���ֱ��ʹ����ѧ����

// ����һ�� Noise �����ռ��еľ�̬��
public static partial class Noise
{
    // ����һ���ӿڣ����� Voronoi �������
    public interface IVoronoiDistance
    {
        // �� 1D �ռ��м��� Voronoi ���룬���� x �ǲ����㵽 Voronoi ��ľ���
        float4 GetDistance(float4 x);

        // �� 2D �ռ��м��� Voronoi ���룬���� x �� y ������ά�ȵľ���
        float4 GetDistance(float4 x, float4 y);

        // �� 3D �ռ��м��� Voronoi ���룬���� x, y �� z ������ά�ȵľ���
        float4 GetDistance(float4 x, float4 y, float4 z);

        // ���մ��� 1D �ռ��е� Voronoi ������Сֵ������ minima �ǰ���������Сֵ�� float4x2 �ṹ
        float4x2 Finalize1D(float4x2 minima);

        // ���մ��� 2D �ռ��е� Voronoi ������Сֵ
        float4x2 Finalize2D(float4x2 minima);

        // ���մ��� 3D �ռ��е� Voronoi ������Сֵ
        float4x2 Finalize3D(float4x2 minima);
    }
}
