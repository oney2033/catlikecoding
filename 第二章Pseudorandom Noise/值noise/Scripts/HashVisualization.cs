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
        [ReadOnly]
        public NativeArray<float3x4> positions;

        [WriteOnly]  // 只写，避免读取操作，优化性能
        public NativeArray<uint4> hashes; // 用于存储计算出的哈希值
        public SmallXXHash4 hash; // 使用 SmallXXHash 作为哈希函数
        public float3x4 domainTRS;
        float4x3 TransformPositions(float3x4 trs, float4x3 p) => float4x3(
            trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x,
            trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y,
            trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z
        );

        public void Execute(int i)
        {
            float4x3 p = TransformPositions(domainTRS, transpose(positions[i]));

            int4 u = (int4)floor(p.c0);
            int4 v = (int4)floor(p.c1);
            int4 w = (int4)floor(p.c2);

            hashes[i] = hash.Eat(u).Eat(v).Eat(w);
        }
    }

    public enum Shape { Plane, Sphere, Torus }

    static Shapes.ScheduleDelegate[] shapeJobs = {
        Shapes.Job<Shapes.Plane>.ScheduleParallel,
        Shapes.Job<Shapes.Sphere>.ScheduleParallel,
        Shapes.Job<Shapes.Torus>.ScheduleParallel
    };

    [SerializeField]
    Shape shape;

    // Shader 属性的 ID，用于在着色器中设置对应的缓冲区和配置
    static int
        hashesId = Shader.PropertyToID("_Hashes"),  // 哈希值缓冲区的 Shader 属性 ID
        positionsId = Shader.PropertyToID("_Positions"),
        normalsId = Shader.PropertyToID("_Normals"),
        configId = Shader.PropertyToID("_Config");  // 配置向量的 Shader 属性 ID

    [SerializeField]  // 使用 [SerializeField] 使得这些字段可以在 Unity Inspector 中设置
    Mesh instanceMesh; // 用于实例化的 Mesh

    [SerializeField]
    Material material; // 使用的材质

    [SerializeField, Range(1, 512)] // 限制分辨率的范围为 1 到 512
    int resolution = 16; // 实例化的网格分辨率

    [SerializeField]
    int seed; // 哈希函数的种子，用于生成不同的随机值

    [SerializeField, Range(-0.5f, 0.5f)]
    float displacement = 0.1f;

    NativeArray<uint4> hashes; // 用于存储每个实例的哈希值
    NativeArray<float3x4> positions, normals;

    ComputeBuffer hashesBuffer, positionsBuffer, normalsBuffer; // ComputeBuffer 用于传递数据到 GPU

    MaterialPropertyBlock propertyBlock; // 用于在绘制时设置实例化材质属性

    [SerializeField]
    SpaceTRS domain = new SpaceTRS
    {
        scale = 8f
    };
    Bounds bounds;
    bool isDirty;

    [SerializeField, Range(0.1f, 10f)]
    float instanceScale = 2f;

    // 当脚本启用时调用
    void OnEnable()
    {
        isDirty = true;
        // 计算总实例数，分辨率 * 分辨率 = 实例数
        int length = resolution * resolution;

        // 分配 NativeArray，用于存储哈希值，分配在持久化内存中
        length = length / 4 + (length & 1);
        hashes = new NativeArray<uint4>(length, Allocator.Persistent);
        positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        normals = new NativeArray<float3x4>(length, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(length * 4, 4);
        positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);
        normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

        // 如果 propertyBlock 为 null 则初始化
        propertyBlock ??= new MaterialPropertyBlock();

        // 将哈希缓冲区绑定到材质属性中
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
        propertyBlock.SetBuffer(positionsId, positionsBuffer);
        propertyBlock.SetBuffer(normalsId, normalsBuffer);

        // 设置配置向量，包含分辨率、垂直偏移等参数
        propertyBlock.SetVector(configId, new Vector4(
            resolution, instanceScale / resolution, displacement
        ));
    }

    // 当脚本禁用时调用
    void OnDisable()
    {
        // 释放 NativeArray 和 ComputeBuffer 占用的内存
        hashes.Dispose();
        positions.Dispose();
        normals.Dispose();
        hashesBuffer.Release();
        positionsBuffer.Release();
        normalsBuffer.Release();
        hashesBuffer = null;
        positionsBuffer = null;
        normalsBuffer = null;
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
        if (isDirty || transform.hasChanged)
        {
            isDirty = false;
            transform.hasChanged = false;

            JobHandle handle = shapeJobs[(int)shape](
                positions, normals, resolution, transform.localToWorldMatrix, default
            );

            new HashJob
            {
                positions = positions,
                hashes = hashes,
                hash = SmallXXHash.Seed(seed),
                domainTRS = domain.Matrix
            }.ScheduleParallel(hashes.Length, resolution, handle).Complete();

            hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
            positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
            normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));
            bounds = new Bounds(
                transform.position,
                float3(2f * cmax(abs(transform.lossyScale)) + displacement)
            );
        }
        // 使用实例化的网格绘制，并将属性块中的数据传递给 Shader
        Graphics.DrawMeshInstancedProcedural(
            instanceMesh, 0, material, bounds,
            resolution * resolution, propertyBlock
        );
    }
}
