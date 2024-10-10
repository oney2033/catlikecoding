using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;  // 使用Burst编译器以提高性能
using Unity.Collections;  // 用于管理内存中的原生数组
using Unity.Jobs;  // 用于调度和管理并行作业
using Unity.Mathematics;  // 数学库
using static Unity.Mathematics.math;  // 静态导入math中的数学方法
using quaternion = Unity.Mathematics.quaternion;  // 简化引用，避免与UnityEngine.Quaternion冲突


public class Fractal : MonoBehaviour
{
    // 使用 Burst 编译器来优化性能，启用同步编译，精度标准和模式分别设置为标准和快速
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor
    {
        public float spinAngleDelta;  // 控制每一帧旋转角度的增量
        public float scale;           // 分形的缩放比例

        [ReadOnly]
        public NativeArray<FractalPart> parents;  // 当前层级的父节点

        public NativeArray<FractalPart> parts;    // 当前层级的所有子节点

        [WriteOnly]
        public NativeArray<float3x4> matrices;    // 用于储存每个分形部分的变换矩阵

        // 执行并更新分形节点的旋转和位置
        public void Execute(int i)
        {
            FractalPart parent = parents[i / 5]; // 每5个子节点共享一个父节点
            FractalPart part = parts[i];         // 获取当前要处理的子节点
            part.spinAngle += spinAngleDelta;    // 更新子节点的旋转角度

            // 计算子节点的世界空间旋转，父节点的旋转*子节点的本地旋转
            part.worldRotation = mul(parent.worldRotation,
                mul(part.rotation, quaternion.RotateY(part.spinAngle))
            );
            // 计算子节点的世界空间位置
            part.worldPosition =
                parent.worldPosition +
                mul(parent.worldRotation, 1.5f * scale * part.direction);

            parts[i] = part;  // 更新子节点

            // 生成3x3的旋转矩阵并乘以缩放，最后构建3x4矩阵存储位置信息
            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    }

    [SerializeField, Range(1, 8)]
    int depth = 4;  // 控制分形的深度，即分形的层数

    [SerializeField]
    Mesh mesh;  // 分形每个部分使用的网格

    [SerializeField]
    Material material;  // 渲染分形的材质

    struct FractalPart
    {
        public float3 direction, worldPosition;    // 方向和世界空间位置
        public quaternion rotation, worldRotation; // 本地旋转和世界旋转
        public float spinAngle;  // 自旋角度，用于动画效果
    }

    // 定义分形各个方向的单位向量（上、右、左、前、后）
    static float3[] directions = {
        up(), right(), left(), forward(), back()
    };

    // 定义不同方向的旋转角度（基于单位四元数）
    static quaternion[] rotations = {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };

    NativeArray<FractalPart>[] parts;  // 储存分形所有部分的数组，每一层一个数组

    NativeArray<float3x4>[] matrices;  // 储存变换矩阵的数组，每一层一个数组
    ComputeBuffer[] matricesBuffers;   // 用于传递矩阵给 GPU 进行渲染的缓冲区
    static readonly int matricesId = Shader.PropertyToID("_Matrices");  // 获取着色器属性的 ID
    static MaterialPropertyBlock propertyBlock;  // 用于传递每个实例化对象的属性

    // 初始化分形数据，在对象启用时调用
    void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];  // 初始化每一层的分形节点
        matrices = new NativeArray<float3x4>[depth];  // 初始化每一层的变换矩阵
        matricesBuffers = new ComputeBuffer[depth];   // 初始化缓冲区
        int stride = 12 * 4;  // 每个矩阵的内存大小

        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            // 分形的每一层都有 5 的倍数个部分
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        // 初始化第一层的根节点
        parts[0][0] = CreatePart(0);

        // 为剩余层级的每个分形部分创建子节点
        for (int li = 1; li < parts.Length; li++)
        {
            NativeArray<FractalPart> levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                // 每 5 个子节点对应一个父节点
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }

        // 初始化材质属性块
        propertyBlock ??= new MaterialPropertyBlock();
    }

    // 释放所有分配的内存资源
    void OnDisable()
    {
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();  // 释放 GPU 缓冲区
            parts[i].Dispose();            // 释放 NativeArray 资源
            matrices[i].Dispose();         // 释放矩阵数组
        }
        parts = null;
        matrices = null;
        matricesBuffers = null;
    }

    // 当某些参数变化时（例如分形深度），需要重新初始化
    void OnValidate()
    {
        if (parts != null && enabled)
        {
            OnDisable();  // 先禁用再重新启用
            OnEnable();
        }
    }

    // 每一帧都要更新分形动画状态
    void Update()
    {
        float spinAngleDelta = 0.125f * PI * Time.deltaTime;  // 控制旋转速度
        FractalPart rootPart = parts[0][0];  // 获取根节点
        rootPart.spinAngle += spinAngleDelta;  // 更新根节点的旋转角度
        rootPart.worldRotation = mul(transform.rotation,
            mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle))
        );
        rootPart.worldPosition = transform.position;  // 根节点的位置是物体的当前位置
        parts[0][0] = rootPart;  // 更新根节点

        float objectScale = transform.lossyScale.x;  // 获取物体的缩放
        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;  // 计算缩放和旋转矩阵
        matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);  // 设置根节点的矩阵

        float scale = objectScale;  // 初始化缩放比例
        JobHandle jobHandle = default;  // 创建 JobHandle 用于处理并行任务
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;  // 每一层的缩放比例是上一层的一半
            // 调度并行任务，处理每一层分形节点的更新
            jobHandle = new UpdateFractalLevelJob
            {
                spinAngleDelta = spinAngleDelta,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.ScheduleParallel(parts[li].Length, 5, jobHandle);
        }
        jobHandle.Complete();  // 确保任务完成

        // 定义渲染的包围盒，防止剔除
        var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);

        // 更新每一层的矩阵数据，并使用 GPU 渲染分形
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);  // 将矩阵数据传递给 GPU
            propertyBlock.SetBuffer(matricesId, buffer);  // 设置材质属性
            Graphics.DrawMeshInstancedProcedural(
                mesh, 0, material, bounds, buffer.count, propertyBlock  // 实例化绘制
            );
        }
    }

    // 创建分形的一个部分，包含其方向和旋转信息
    FractalPart CreatePart(int childIndex) => new FractalPart
    {
        direction = directions[childIndex],  // 获取该子节点的方向
        rotation = rotations[childIndex]     // 获取该子节点的本地旋转
    };

    /*使用URP渲染管线
[SerializeField, Range(1, 8)]
int depth = 4;  // 分形深度，允许用户在1到8之间设置。
[SerializeField]
Mesh mesh;  // 要渲染的网格

[SerializeField]
Material material;  // 使用的材质

// 结构体，代表分形中的一个部分，包括方向、位置、旋转等信息
struct FractalPart
{
    public Vector3 direction, worldPosition;  // 分别是方向和世界坐标位置
    public Quaternion rotation, worldRotation;  // 局部和世界的旋转
    public float spinAngle;  // 旋转角度
}

// 预定义的方向数组，表示每个子节点的方向
static Vector3[] directions = {
    Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
};

// 预定义的旋转数组，表示每个子节点的初始旋转
static Quaternion[] rotations = {
    Quaternion.identity,
    Quaternion.Euler(0f, 0f, -90f), 
    Quaternion.Euler(0f, 0f, 90f),
    Quaternion.Euler(90f, 0f, 0f), 
    Quaternion.Euler(-90f, 0f, 0f)
};

// 用于存储每层分形部分的数组
FractalPart[][] parts;

// 用于存储矩阵的二维数组，每一层分形的每个部分都有一个对应的变换矩阵
Matrix4x4[][] matrices;
// ComputeBuffer 用于向 GPU 传递矩阵数据
ComputeBuffer[] matricesBuffers;
// 用于将矩阵数据传递给着色器的属性ID
static readonly int matricesId = Shader.PropertyToID("_Matrices");
// 用于在渲染时提供材质属性的块
static MaterialPropertyBlock propertyBlock;

void OnEnable()
{
    // 初始化分形部分和矩阵数组，矩阵缓冲区
    parts = new FractalPart[depth][];
    matrices = new Matrix4x4[depth][];
    matricesBuffers = new ComputeBuffer[depth];
    int stride = 16 * 4;  // 每个矩阵的大小为16个float，每个float 4字节
    for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
    {
        parts[i] = new FractalPart[length];
        matrices[i] = new Matrix4x4[length];
        matricesBuffers[i] = new ComputeBuffer(length, stride);  // 初始化缓冲区
    }

    // 初始化根节点的分形部分
    parts[0][0] = CreatePart(0);    
    for (int li = 1; li < parts.Length; li++)
    {
        // scale *= 0.5f;  // 可用于调整大小的缩放比例（注释掉）
        FractalPart[] levelParts = parts[li];
        for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
        {
            for (int ci = 0; ci < 5; ci++)
            {
                levelParts[fpi + ci] = CreatePart(ci);  // 初始化子节点
            }
        }
    }

    propertyBlock ??= new MaterialPropertyBlock();  // 确保材质属性块已初始化
}

void OnDisable()
{
    // 释放矩阵缓冲区，清理内存
    for (int i = 0; i < matricesBuffers.Length; i++)
    {
        matricesBuffers[i].Release();
    }
    parts = null;
    matrices = null;
    matricesBuffers = null;
}

// 处理在编辑器中更改参数时重新初始化分形
void OnValidate()
{
    if (parts != null && enabled)
    {
        OnDisable();
        OnEnable();
    }
}

void Update()
{
    // 每帧增加的旋转角度
    float spinAngleDelta = 22.5f * Time.deltaTime;
    FractalPart rootPart = parts[0][0];
    rootPart.spinAngle += spinAngleDelta;  // 更新旋转角度
    // 更新根节点的世界旋转
    rootPart.worldRotation =
         transform.rotation *
         (rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f));
    rootPart.worldPosition = transform.position;  // 更新根节点的世界位置
    parts[0][0] = rootPart;

    float objectScale = transform.lossyScale.x;  // 获取对象的缩放比例
    matrices[0][0] = Matrix4x4.TRS(
        rootPart.worldPosition, rootPart.worldRotation, objectScale * Vector3.one
    );

    // 递归更新每一层的分形
    float scale = objectScale;
    for (int li = 1; li < parts.Length; li++)
    {
        scale *= 0.5f;  // 每层缩小一半
        FractalPart[] parentParts = parts[li - 1];
        FractalPart[] levelParts = parts[li];
        Matrix4x4[] levelMatrices = matrices[li];
        for (int fpi = 0; fpi < levelParts.Length; fpi++)
        {
            FractalPart parent = parentParts[fpi / 5];  // 获取父节点
            FractalPart part = levelParts[fpi];
            part.spinAngle += spinAngleDelta;  // 更新子节点旋转角度
            part.worldRotation =
                parent.worldRotation *
                (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
            part.worldPosition =
                parent.worldPosition +
                parent.worldRotation * (1.5f * scale * part.direction);
            levelParts[fpi] = part;
            // 更新矩阵
            levelMatrices[fpi] = Matrix4x4.TRS(
                part.worldPosition, part.worldRotation, scale * Vector3.one
            );
        }
    }

    // 计算包围盒，用于在屏幕上绘制分形
    var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
    for (int i = 0; i < matricesBuffers.Length; i++)
    {
        ComputeBuffer buffer = matricesBuffers[i];
        buffer.SetData(matrices[i]);  // 将数据传递到 GPU
        propertyBlock.SetBuffer(matricesId, buffer);
        Graphics.DrawMeshInstancedProcedural(
            mesh, 0, material, bounds, buffer.count, propertyBlock
        );
    }
}

// 创建分形的一个部分，指定子节点索引
FractalPart CreatePart(int childIndex) => new FractalPart
{
    direction = directions[childIndex],  // 获取该子节点的方向
    rotation = rotations[childIndex]     // 获取该子节点的旋转
};

    */

    /*优化后还是不能达到8的深度
    [SerializeField, Range(1, 8)]
    int depth = 4;
    [SerializeField]
    Mesh mesh;

    [SerializeField]
    Material material;

    struct FractalPart
    {
        public Vector3 direction;
        public Quaternion rotation;
        public Transform transform;
    }


    static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };

    FractalPart[][] parts;

    void Awake()
    {
        parts = new FractalPart[depth][];
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new FractalPart[length];
        }
        float scale = 1f;
        parts[0][0] = CreatePart(0, 0, scale);
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            FractalPart[] levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(li, ci, scale);
                }
            }
        }
    }

    void Update()
    {
        Quaternion deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);
        FractalPart rootPart = parts[0][0];
        rootPart.rotation *= deltaRotation;
        rootPart.transform.localRotation = rootPart.rotation;
        parts[0][0] = rootPart;
        for (int li = 1; li < parts.Length; li++)
        {
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi++)
            {
                Transform parentTransform = parentParts[fpi / 5].transform;
                FractalPart part = levelParts[fpi];
                part.rotation *= deltaRotation;
                part.transform.localRotation = part.rotation;
                part.transform.localPosition =
                     parentTransform.localPosition +
                     parentTransform.localRotation *
                         (1.5f * part.transform.localScale.x * part.direction);
                levelParts[fpi] = part;
            }
        }
    }

    FractalPart CreatePart(int levelIndex, int childIndex, float scale)
        {
            var go = new GameObject("Fractal Part L" + levelIndex + " C" + childIndex);
            go.transform.localScale = scale * Vector3.one;
            go.transform.SetParent(transform, false);
             go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>().material = material;
        return new FractalPart()
        {
            direction = directions[childIndex],
            rotation = rotations[childIndex],
            transform = go.transform
        };
    }
    */

    /*通用方法深度到达8时，帧率只有3帧
    void Start()
    {
        name = "Fractal " + depth;
        if (depth <= 1)
        {
            return;
        }

        Fractal childA = CreateChild(Vector3.up, Quaternion.identity);
        Fractal childB = CreateChild(Vector3.right, Quaternion.Euler(0f, 0f, -90f));
        Fractal childC = CreateChild(Vector3.left, Quaternion.Euler(0f, 0f, 90f));
        Fractal childD = CreateChild(Vector3.forward, Quaternion.Euler(90f, 0f, 0f));
        Fractal childE = CreateChild(Vector3.back, Quaternion.Euler(-90f, 0f, 0f));

        childA.transform.SetParent(transform, false);
        childB.transform.SetParent(transform, false);
        childC.transform.SetParent(transform, false);
        childD.transform.SetParent(transform, false);
        childE.transform.SetParent(transform, false);
    }

    Fractal CreateChild(Vector3 direction, Quaternion rotation)
    {
        Fractal child = Instantiate(this);
        child.depth = depth - 1;
        child.transform.localPosition = 0.75f * direction;
        child.transform.localRotation = rotation;
        child.transform.localScale = 0.5f * Vector3.one;
        return child;
    }
    void Update()
    {
        transform.Rotate(0f, 22.5f * Time.deltaTime, 0f);
    }
    */
}
