using Unity.Collections;          // ʹ�� NativeArray ��ԭ�����ݽṹ
using Unity.Jobs;                 // ʹ�� Job System ���ж��̵߳���
using Unity.Mathematics;           // ʹ�� Unity Mathematics ����������ѧ����
using UnityEngine;                // Unity �ĺ��Ŀ�
using static Noise;               // ʹ���Զ���� Noise ���еľ�̬����

// ����һ���������ӻ��࣬�̳��� Visualization ����
public class NoiseVisualization : Visualization
{
    // ����һ���������͵�ö�٣�������ͬ���͵�����
    public enum NoiseType { Perlin, PerlinTurbulence, 
        Value, ValueTurbulence, 
        VoronoiWorleyF1, VoronoiWorleyF2, VoronoiWorleyF2MinusF11,
        VoronoiChebyshevF1, VoronoiChebyshevF2, VoronoiChebyshevF2MinusF1
    }

    [SerializeField]
    NoiseType type; // ���л��ֶΣ��� Unity �༭���п���ѡ����������

    // Shader ���Ե� ID����������ɫ�������ö�Ӧ�Ļ�����������
    static int noiseId = Shader.PropertyToID("_Noise");

    [SerializeField]
    Settings noiseSettings = Settings.Default; // �������趨������Ĭ��ֵ

    NativeArray<float4> noise; // ԭ�����飬���ڴ洢��������
    ComputeBuffer noiseBuffer; // ���㻺���������ڽ��������ݴ��ݵ���ɫ��

    [SerializeField]
    SpaceTRS domain = new SpaceTRS // ���������Ŀռ�任���������Ų���
    {
        scale = 8f // ����ֵΪ 8
    };

    // ���ű�����ʱ���ã���ʼ���������ݺͻ�����
    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
    {
        noise = new NativeArray<float4>(dataLength, Allocator.Persistent); // �����������飬����Ϊ dataLength
        noiseBuffer = new ComputeBuffer(dataLength * 4, 4); // ����һ�����㻺����
        propertyBlock.SetBuffer(noiseId, noiseBuffer); // ���������󶨵���ɫ������
    }

    // ���ű�����ʱ���ã��ͷ��������ݺͻ�����
    protected override void DisableVisualization()
    {
        noise.Dispose(); // �ͷ�ԭ������
        noiseBuffer.Release(); // �ͷŻ�����
        noiseBuffer = null; // ��ջ���������
    }

    // ����������Ⱥ������洢��ͬά�Ⱥ��������͵Ĳ�������
    static ScheduleDelegate[,] noiseJobs = {
        { // Perlin ��������
            Job<Lattice1D<LatticeNormal, Perlin>>.ScheduleParallel,   // 1D ��ͨ Perlin ����
            Job<Lattice1D<LatticeTiling, Perlin>>.ScheduleParallel,   // 1D ƽ�� Perlin ����
            Job<Lattice2D<LatticeNormal, Perlin>>.ScheduleParallel,   // 2D ��ͨ Perlin ����
            Job<Lattice2D<LatticeTiling, Perlin>>.ScheduleParallel,   // 2D ƽ�� Perlin ����
            Job<Lattice3D<LatticeNormal, Perlin>>.ScheduleParallel,   // 3D ��ͨ Perlin ����
            Job<Lattice3D<LatticeTiling, Perlin>>.ScheduleParallel    // 3D ƽ�� Perlin ����
        },
        { // Perlin �����������汾����
            Job<Lattice1D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel
        },
        { // Value ��������
            Job<Lattice1D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Value>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Value>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Value>>.ScheduleParallel
        },
        { // Value �����������汾����
            Job<Lattice1D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal,Worley, F1>>.ScheduleParallel,
            Job<Voronoi1D<LatticeTiling,Worley, F1>>.ScheduleParallel,
            Job<Voronoi2D<LatticeNormal,Worley, F1>>.ScheduleParallel,
            Job<Voronoi2D<LatticeTiling,Worley, F1>>.ScheduleParallel,
            Job<Voronoi3D<LatticeNormal,Worley, F1>>.ScheduleParallel,
            Job<Voronoi3D<LatticeTiling,Worley, F1>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal,Worley, F2>>.ScheduleParallel,
            Job<Voronoi1D<LatticeTiling,Worley, F2>>.ScheduleParallel,
            Job<Voronoi2D<LatticeNormal,Worley, F2>>.ScheduleParallel,
            Job<Voronoi2D<LatticeTiling,Worley, F2>>.ScheduleParallel,
            Job<Voronoi3D<LatticeNormal,Worley, F2>>.ScheduleParallel,
            Job<Voronoi3D<LatticeTiling,Worley, F2>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal,Worley, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi1D<LatticeTiling,Worley, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi2D<LatticeNormal,Worley, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi2D<LatticeTiling,Worley, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi3D<LatticeNormal,Worley, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi3D<LatticeTiling,Worley, F2MinusF1>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
            Job<Voronoi1D<LatticeTiling, Worley, F1>>.ScheduleParallel,
            Job<Voronoi2D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel,
            Job<Voronoi2D<LatticeTiling, Chebyshev, F1>>.ScheduleParallel,
            Job<Voronoi3D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel,
            Job<Voronoi3D<LatticeTiling, Chebyshev, F1>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
            Job<Voronoi1D<LatticeTiling, Worley, F2>>.ScheduleParallel,
            Job<Voronoi2D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel,
            Job<Voronoi2D<LatticeTiling, Chebyshev, F2>>.ScheduleParallel,
            Job<Voronoi3D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel,
            Job<Voronoi3D<LatticeTiling, Chebyshev, F2>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi1D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi2D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi2D<LatticeTiling, Chebyshev, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi3D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel,
            Job<Voronoi3D<LatticeTiling, Chebyshev, F2MinusF1>>.ScheduleParallel
        }
    };

    [SerializeField, Range(1, 3)]
    int dimensions = 3; // ����������ά�ȣ���Χ�� 1 �� 3

    [SerializeField]
    bool tiling; // �Ƿ�����ƽ��

    // ÿ֡����ʱ���ã������������ӻ�
    protected override void UpdateVisualization(
        NativeArray<float3x4> positions, int resolution, JobHandle handle
    )
    {
        // ����ѡ�����������͡�ά�Ⱥ��Ƿ�ƽ�̣����ȶ�Ӧ��������������
        noiseJobs[(int)type, 2 * dimensions - (tiling ? 1 : 2)](
                positions, noise, noiseSettings, domain, resolution, handle
            ).Complete(); // ��ɲ�������

        // ����������д�뻺�����������ݵ� GPU ������Ⱦ
        noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
    }
}
