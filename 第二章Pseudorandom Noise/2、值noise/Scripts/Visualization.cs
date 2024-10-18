using Unity.Collections;          // 使用 NativeArray 等原生数据结构
using Unity.Jobs;                 // 使用 Job System 进行多线程调度
using Unity.Mathematics;           // 使用 Unity Mathematics 库来进行数学运算
using UnityEngine;                // Unity 的核心库
using static Unity.Mathematics.math; // 静态导入 math 类，方便调用 math 函数

public abstract class Visualization : MonoBehaviour
{
    // 抽象方法：启用可视化，参数是数据长度和材质属性块
    protected abstract void EnableVisualization(
         int dataLength, MaterialPropertyBlock propertyBlock
     );

    // 抽象方法：禁用可视化
    protected abstract void DisableVisualization();

    // 抽象方法：更新可视化，参数是位置数组、分辨率和作业句柄
    protected abstract void UpdateVisualization(
    NativeArray<float3x4> positions, int resolution, JobHandle handle
);

    // 枚举：定义三种形状
    public enum Shape { Plane, Sphere, Torus }

    // 定义了每种形状的作业调度方法
    static Shapes.ScheduleDelegate[] shapeJobs = {
        Shapes.Job<Shapes.Plane>.ScheduleParallel,
        Shapes.Job<Shapes.Sphere>.ScheduleParallel,
        Shapes.Job<Shapes.Torus>.ScheduleParallel
    };

    [SerializeField]
    Shape shape;  // 用于选择可视化形状

    // Shader 属性 ID，用于在 Shader 中引用缓冲区和配置参数
    static int
        positionsId = Shader.PropertyToID("_Positions"), // 位置属性 ID
        normalsId = Shader.PropertyToID("_Normals"),     // 法线属性 ID
        configId = Shader.PropertyToID("_Config");       // 配置向量 ID

    [SerializeField]  // 使得这些字段在 Unity Inspector 中可见
    Mesh instanceMesh; // 用于实例化的网格（mesh）

    [SerializeField]
    Material material; // 使用的材质

    [SerializeField, Range(1, 512)] // 限制分辨率的范围为 1 到 512
    int resolution = 16;  // 实例化的网格分辨率

    [SerializeField, Range(-0.5f, 0.5f)]
    float displacement = 0.1f; // 垂直偏移量

    NativeArray<float3x4> positions, normals;  // 存储位置信息和法线的原生数组

    ComputeBuffer positionsBuffer, normalsBuffer; // 用于 GPU 计算的缓冲区

    MaterialPropertyBlock propertyBlock; // 存储实例化材质的属性

    Bounds bounds;  // 确定绘制区域的边界框
    bool isDirty;   // 标记是否需要更新

    [SerializeField, Range(0.1f, 10f)]
    float instanceScale = 2f;  // 实例化网格的缩放比例

    // 当脚本启用时调用
    void OnEnable()
    {
        isDirty = true;  // 标记需要更新
        int length = resolution * resolution;  // 计算总实例数
        length = length / 4 + (length & 1); // 确保长度为 4 的倍数，优化性能

        // 分配 NativeArray 用于存储位置信息和法线信息
        positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        normals = new NativeArray<float3x4>(length, Allocator.Persistent);

        // 分配 GPU 计算的 ComputeBuffer，用于存储位置和法线数据
        positionsBuffer = new ComputeBuffer(length * 4, 3 * 4); // 每个位置有 3 个 float4
        normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

        // 初始化 MaterialPropertyBlock
        propertyBlock ??= new MaterialPropertyBlock();
        EnableVisualization(length, propertyBlock);  // 启用可视化

        // 将缓冲区数据绑定到 Shader 的属性中
        propertyBlock.SetBuffer(positionsId, positionsBuffer);
        propertyBlock.SetBuffer(normalsId, normalsBuffer);

        // 设置 Shader 中的配置向量，包含分辨率、缩放、位移等参数
        propertyBlock.SetVector(configId, new Vector4(
            resolution, instanceScale / resolution, displacement
        ));
    }

    // 当脚本禁用时调用
    void OnDisable()
    {
        // 释放 NativeArray 和 ComputeBuffer 占用的内存
        positions.Dispose();
        normals.Dispose();
        positionsBuffer.Release();
        normalsBuffer.Release();
        positionsBuffer = null;
        normalsBuffer = null;
        DisableVisualization();  // 禁用可视化
    }

    // 当 Inspector 中的参数发生变化时调用
    void OnValidate()
    {
        // 如果缓冲区已存在且脚本已启用，重新初始化缓冲区
        if (positionsBuffer != null && enabled)
        {
            OnDisable();  // 禁用当前资源
            OnEnable();   // 重新启用
        }
    }

    // 每帧更新时调用
    void Update()
    {
        // 如果需要更新或者变换发生了变化
        if (isDirty || transform.hasChanged)
        {
            isDirty = false;  // 重置更新标记
            transform.hasChanged = false;  // 重置变换标记

            // 调用形状作业调度方法来更新可视化数据
            UpdateVisualization(
                positions, resolution,
                shapeJobs[(int)shape]( // 根据选择的形状进行作业调度
                    positions, normals, resolution, transform.localToWorldMatrix, default
                )
            );

            // 将计算得到的位置信息和法线数据传递给 ComputeBuffer
            positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
            normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));

            // 计算绘制的边界框
            bounds = new Bounds(
                transform.position,
                float3(2f * cmax(abs(transform.lossyScale)) + displacement)
            );
        }

        // 使用 Graphics.DrawMeshInstancedProcedural 来绘制实例化的网格
        Graphics.DrawMeshInstancedProcedural(
            instanceMesh, 0, material, bounds,
            resolution * resolution, propertyBlock
        );
    }
}
