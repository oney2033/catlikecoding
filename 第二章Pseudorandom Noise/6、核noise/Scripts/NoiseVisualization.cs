using Unity.Collections;          // 使用 NativeArray 等原生数据结构
using Unity.Jobs;                 // 使用 Job System 进行多线程调度
using Unity.Mathematics;           // 使用 Unity Mathematics 库来进行数学运算
using UnityEngine;                // Unity 的核心库
using static Noise;               // 使用自定义的 Noise 类中的静态方法

// 定义一个噪声可视化类，继承自 Visualization 基类
public class NoiseVisualization : Visualization
{
    // 定义一个噪声类型的枚举，包含不同类型的噪声
    public enum NoiseType { Perlin, PerlinTurbulence, 
        Value, ValueTurbulence,
        VoronoiWorleyF1, VoronoiWorleyF2, VoronoiWorleyF2MinusF11,
        VoronoiChebyshevF1, VoronoiChebyshevF2, VoronoiChebyshevF2MinusF1,
        SimplexValue, SimplexValueTurbulence, Simplex, SimplexTurbulence,
    }

    [SerializeField]
    NoiseType type; // 序列化字段，在 Unity 编辑器中可以选择噪声类型

    // Shader 属性的 ID，用于在着色器中设置对应的缓冲区和配置
    static int noiseId = Shader.PropertyToID("_Noise");

    [SerializeField]
    Settings noiseSettings = Settings.Default; // 噪声的设定参数，默认值

    NativeArray<float4> noise; // 原生数组，用于存储噪声数据
    ComputeBuffer noiseBuffer; // 计算缓冲区，用于将噪声数据传递到着色器

    [SerializeField]
    SpaceTRS domain = new SpaceTRS // 定义噪声的空间变换，包含缩放参数
    {
        scale = 8f // 缩放值为 8
    };

    // 当脚本启用时调用，初始化噪声数据和缓冲区
    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
    {
        noise = new NativeArray<float4>(dataLength, Allocator.Persistent); // 分配噪声数组，长度为 dataLength
        noiseBuffer = new ComputeBuffer(dataLength * 4, 4); // 分配一个计算缓冲区
        propertyBlock.SetBuffer(noiseId, noiseBuffer); // 将缓冲区绑定到着色器属性
    }

    // 当脚本禁用时调用，释放噪声数据和缓冲区
    protected override void DisableVisualization()
    {
        noise.Dispose(); // 释放原生数组
        noiseBuffer.Release(); // 释放缓冲区
        noiseBuffer = null; // 清空缓冲区引用
    }

    // 噪声任务调度函数表，存储不同维度和噪声类型的并行任务
    static ScheduleDelegate[,] noiseJobs = {
        { // Perlin 噪声任务
            Job<Lattice1D<LatticeNormal, Perlin>>.ScheduleParallel,   // 1D 普通 Perlin 噪声
            Job<Lattice1D<LatticeTiling, Perlin>>.ScheduleParallel,   // 1D 平铺 Perlin 噪声
            Job<Lattice2D<LatticeNormal, Perlin>>.ScheduleParallel,   // 2D 普通 Perlin 噪声
            Job<Lattice2D<LatticeTiling, Perlin>>.ScheduleParallel,   // 2D 平铺 Perlin 噪声
            Job<Lattice3D<LatticeNormal, Perlin>>.ScheduleParallel,   // 3D 普通 Perlin 噪声
            Job<Lattice3D<LatticeTiling, Perlin>>.ScheduleParallel    // 3D 平铺 Perlin 噪声
        },
        { // Perlin 噪声的湍流版本任务
            Job<Lattice1D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel
        },
        { // Value 噪声任务
            Job<Lattice1D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Value>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Value>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Value>>.ScheduleParallel
        },
        { // Value 噪声的湍流版本任务
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
        },
        {
            Job<Simplex1D<Value>>.ScheduleParallel,
            Job<Simplex1D<Value>>.ScheduleParallel,
            Job<Simplex2D<Value>>.ScheduleParallel,
            Job<Simplex2D<Value>>.ScheduleParallel,
            Job<Simplex3D<Value>>.ScheduleParallel,
            Job<Simplex3D<Value>>.ScheduleParallel
        },
        {
            Job<Simplex1D<Turbulence<Value>>>.ScheduleParallel,
            Job<Simplex1D<Turbulence<Value>>>.ScheduleParallel,
            Job<Simplex2D<Turbulence<Value>>>.ScheduleParallel,
            Job<Simplex2D<Turbulence<Value>>>.ScheduleParallel,
            Job<Simplex3D<Turbulence<Value>>>.ScheduleParallel,
            Job<Simplex3D<Turbulence<Value>>>.ScheduleParallel
        },
        {
            Job<Simplex1D<Simplex>>.ScheduleParallel,
            Job<Simplex1D<Simplex>>.ScheduleParallel,
            Job<Simplex2D<Simplex>>.ScheduleParallel,
            Job<Simplex2D<Simplex>>.ScheduleParallel,
            Job<Simplex3D<Simplex>>.ScheduleParallel,
            Job<Simplex3D<Simplex>>.ScheduleParallel
        },
        {
            Job<Simplex1D<Turbulence<Simplex>>>.ScheduleParallel,
            Job<Simplex1D<Turbulence<Simplex>>>.ScheduleParallel,
            Job<Simplex2D<Turbulence<Simplex>>>.ScheduleParallel,
            Job<Simplex2D<Turbulence<Simplex>>>.ScheduleParallel,
            Job<Simplex3D<Turbulence<Simplex>>>.ScheduleParallel,
            Job<Simplex3D<Turbulence<Simplex>>>.ScheduleParallel
        }
    };

    [SerializeField, Range(1, 3)]
    int dimensions = 3; // 控制噪声的维度，范围从 1 到 3

    [SerializeField]
    bool tiling; // 是否启用平铺

    // 每帧更新时调用，更新噪声可视化
    protected override void UpdateVisualization(
        NativeArray<float3x4> positions, int resolution, JobHandle handle
    )
    {
        // 根据选定的噪声类型、维度和是否平铺，调度对应的噪声生成任务
        noiseJobs[(int)type, 2 * dimensions - (tiling ? 1 : 2)](
                positions, noise, noiseSettings, domain, resolution, handle
            ).Complete(); // 完成并行任务

        // 将噪声数据写入缓冲区，并传递到 GPU 进行渲染
        noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
    }
}
