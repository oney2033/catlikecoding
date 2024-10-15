using Unity.Collections;          // 使用 NativeArray 等原生数据结构
using Unity.Jobs;                 // 使用 Job System 进行多线程调度
using Unity.Mathematics;           // 使用 Unity Mathematics 库来进行数学运算
using UnityEngine;                // Unity 的核心库
using static Noise;

public class NoiseVisualization : Visualization
{
    public enum NoiseType { Perlin, Value }

    [SerializeField]
    NoiseType type;

    // Shader 属性的 ID，用于在着色器中设置对应的缓冲区和配置
    static int noiseId = Shader.PropertyToID("_Noise"); 

    [SerializeField]
    int seed; // 哈希函数的种子，用于生成不同的随机值

    NativeArray<float4> noise;
    ComputeBuffer noiseBuffer;

    [SerializeField]
    SpaceTRS domain = new SpaceTRS
    {
        scale = 8f
    };

    // 当脚本启用时调用
    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
    {
        noise = new NativeArray<float4>(dataLength, Allocator.Persistent);
        noiseBuffer = new ComputeBuffer(dataLength * 4, 4);
        propertyBlock.SetBuffer(noiseId, noiseBuffer);
    }

    // 当脚本禁用时调用
    protected override void DisableVisualization()
    {
        noise.Dispose();
        noiseBuffer.Release();
        noiseBuffer = null;
    }

    static ScheduleDelegate[,] noiseJobs = {
        {
            Job<Lattice1D<Perlin>>.ScheduleParallel,
            Job<Lattice2D<Perlin>>.ScheduleParallel,
            Job<Lattice3D<Perlin>>.ScheduleParallel
        },
        {
            Job<Lattice1D<Value>>.ScheduleParallel,
            Job<Lattice2D<Value>>.ScheduleParallel,
            Job<Lattice3D<Value>>.ScheduleParallel
        }
    };

    [SerializeField, Range(1, 3)]
    int dimensions = 3;

    // 每帧更新时调用
    protected override void UpdateVisualization(
        NativeArray<float3x4> positions, int resolution, JobHandle handle
    )
    {

        noiseJobs[(int)type, dimensions - 1](
                positions, noise, seed, domain, resolution, handle
            ).Complete();
        noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
    }
}
