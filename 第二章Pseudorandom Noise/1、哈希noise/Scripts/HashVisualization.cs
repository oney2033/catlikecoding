using Unity.Burst;                // 使用 Burst 编译器来优化性能
using Unity.Collections;          // 使用 NativeArray 等原生数据结构
using Unity.Jobs;                 // 使用 Job System 进行多线程调度
using Unity.Mathematics;           // 使用 Unity Mathematics 库来进行数学运算
using UnityEngine;                // Unity 的核心库
using static Unity.Mathematics.math; // 静态导入 math 类，方便调用 math 函数

public class HashVisualization : MonoBehaviour
{
    // BurstCompile 属性用于启用 Burst 编译器，提高 Job 的执行性能
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor  // IJobFor 是 Unity Job System 的接口，支持并行调度
    {
        [WriteOnly]  // 只写，避免读取操作，优化性能
        public NativeArray<uint> hashes; // 用于存储计算出的哈希值
        public int resolution; // 分辨率
        public float invResolution; // 分辨率的倒数，用于计算位置
        public SmallXXHash hash; // 使用 SmallXXHash 作为哈希函数

        public void Execute(int i)
        {
            // 根据索引计算二维网格中的 u 和 v 坐标
            int v = (int)floor(invResolution * i + 0.00001f);
            int u = i - resolution * v - resolution / 2;
            v -= resolution / 2;

            // 使用哈希函数将 u 和 v 坐标 "吞入" (Eat)，生成唯一的哈希值
            hashes[i] = hash.Eat(u).Eat(v);
        }
    }

    // Shader 属性的 ID，用于在着色器中设置对应的缓冲区和配置
    static int
        hashesId = Shader.PropertyToID("_Hashes"),  // 哈希值缓冲区的 Shader 属性 ID
        configId = Shader.PropertyToID("_Config");  // 配置向量的 Shader 属性 ID

    [SerializeField]  // 使用 [SerializeField] 使得这些字段可以在 Unity Inspector 中设置
    Mesh instanceMesh; // 用于实例化的 Mesh

    [SerializeField]
    Material material; // 使用的材质

    [SerializeField, Range(1, 512)] // 限制分辨率的范围为 1 到 512
    int resolution = 16; // 实例化的网格分辨率

    [SerializeField]
    int seed; // 哈希函数的种子，用于生成不同的随机值

    [SerializeField, Range(-2f, 2f)] // 限制垂直偏移的范围为 -2 到 2
    float verticalOffset = 1f; // 实例的垂直偏移量

    NativeArray<uint> hashes; // 用于存储每个实例的哈希值

    ComputeBuffer hashesBuffer; // ComputeBuffer 用于传递数据到 GPU

    MaterialPropertyBlock propertyBlock; // 用于在绘制时设置实例化材质属性

    // 当脚本启用时调用
    void OnEnable()
    {
        // 计算总实例数，分辨率 * 分辨率 = 实例数
        int length = resolution * resolution;

        // 分配 NativeArray，用于存储哈希值，分配在持久化内存中
        hashes = new NativeArray<uint>(length, Allocator.Persistent);

        // 创建一个 ComputeBuffer，大小为 length，每个元素为 4 字节（uint）
        hashesBuffer = new ComputeBuffer(length, 4);

        // 调度 HashJob 任务并立即完成
        new HashJob
        {
            hashes = hashes, // 将哈希数组传递给 Job
            resolution = resolution, // 设置分辨率
            invResolution = 1f / resolution, // 计算分辨率的倒数，用于位置计算
            hash = SmallXXHash.Seed(seed) // 生成哈希种子
        }.ScheduleParallel(hashes.Length, resolution, default).Complete();

        // 将计算出的哈希值数据传递到 ComputeBuffer
        hashesBuffer.SetData(hashes);

        // 如果 propertyBlock 为 null 则初始化
        propertyBlock ??= new MaterialPropertyBlock();

        // 将哈希缓冲区绑定到材质属性中
        propertyBlock.SetBuffer(hashesId, hashesBuffer);

        // 设置配置向量，包含分辨率、垂直偏移等参数
        propertyBlock.SetVector(configId, new Vector4(
            resolution, 1f / resolution, verticalOffset / resolution
        ));
    }

    // 当脚本禁用时调用
    void OnDisable()
    {
        // 释放 NativeArray 和 ComputeBuffer 占用的内存
        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null;
    }

    // 当 Inspector 中的参数发生变化时调用
    void OnValidate()
    {
        // 如果缓冲区存在且脚本启用，则重新初始化
        if (hashesBuffer != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    // 每帧更新时调用
    void Update()
    {
        // 使用实例化的网格绘制，并将属性块中的数据传递给 Shader
        Graphics.DrawMeshInstancedProcedural(
            instanceMesh, 0, material, new Bounds(Vector3.zero, Vector3.one),
            hashes.Length, propertyBlock
        );
    }
}
