using Unity.Burst;                // 使用 Burst 编译器来优化性能
using Unity.Collections;          // 使用 NativeArray 等原生数据结构
using Unity.Jobs;                 // 使用 Job System 进行多线程调度
using Unity.Mathematics;           // 使用 Unity Mathematics 库来进行数学运算
using UnityEngine;                // Unity 的核心库
using static Unity.Mathematics.math; // 静态导入 math 类，方便调用 math 函数

public class HashVisualization : Visualization
{

    // BurstCompile 属性用于启用 Burst 编译器，提高 Job 的执行性能
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor  // IJobFor 是 Unity Job System 的接口，支持并行调度
    {
        [ReadOnly]
        public NativeArray<float3x4> positions;

        [WriteOnly]  // 只写，避免读取操作，优化性能
        public NativeArray<uint4> hashes; // 用于存储计算出的哈希值
        public SmallXXHash4 hash; // 使用 SmallXXHash 作为哈希函数
        public float3x4 domainTRS;
     
        public void Execute(int i)
        {
            float4x3 p = domainTRS.TransformVectors(transpose(positions[i]));
            int4 u = (int4)floor(p.c0);
            int4 v = (int4)floor(p.c1);
            int4 w = (int4)floor(p.c2);

            hashes[i] = hash.Eat(u).Eat(v).Eat(w);
        }
    }


    // Shader 属性的 ID，用于在着色器中设置对应的缓冲区和配置
    static int
        hashesId = Shader.PropertyToID("_Hashes");  // 哈希值缓冲区的 Shader 属性 ID
      

    [SerializeField]
    int seed; // 哈希函数的种子，用于生成不同的随机值

    NativeArray<uint4> hashes; // 用于存储每个实例的哈希值

    ComputeBuffer hashesBuffer;

    [SerializeField]
    SpaceTRS domain = new SpaceTRS
    {
        scale = 8f
    };

    // 当脚本启用时调用
    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
    {
        hashes = new NativeArray<uint4>(dataLength, Allocator.Persistent);
       // positions = new NativeArray<float3x4>(length, Allocator.Persistent);
      //  normals = new NativeArray<float3x4>(length, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(dataLength * 4, 4);
       
        // 将哈希缓冲区绑定到材质属性中
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
    }

    // 当脚本禁用时调用
    protected override void DisableVisualization()
    {
        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null;
    }

    // 每帧更新时调用
    protected override void UpdateVisualization(
         NativeArray<float3x4> positions, int resolution, JobHandle handle
     )
    {
        new HashJob
        {
            positions = positions,
            hashes = hashes,
            hash = SmallXXHash.Seed(seed),
            domainTRS = domain.Matrix
        }.ScheduleParallel(hashes.Length, resolution, handle).Complete();
        
        hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
    }
}
