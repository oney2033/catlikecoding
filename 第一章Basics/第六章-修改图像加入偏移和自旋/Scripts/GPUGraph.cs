using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FunctionLibrary;

public class GPUGraph : MonoBehaviour
{
    const int maxResolution = 1000; // 定义最大分辨率为 1000
    // 使用 [Range] 属性在编辑器中限制分辨率的范围为 10 到 200
    [SerializeField, Range(10, maxResolution)]
    int resolution = 10; // 网格的分辨率，定义点的数量

    // 选择要使用的函数，枚举值 FunctionName 在 FunctionLibrary 中定义
    [SerializeField]
    FunctionLibrary.FunctionName function;

    // 定义函数的持续时间和过渡持续时间，最小值为 0
    [SerializeField, Min(0f)]
    float functionDuration = 1f, transitionDuration = 1f; // 分别用于函数的显示持续时间和函数切换的持续时间
    float duration; // 当前持续时间计数

    bool transitioning; // 标记当前是否处于函数过渡状态

    FunctionLibrary.FunctionName transitionFunction; // 用于存储切换前的函数名

    // 定义过渡模式的枚举（循环或随机）
    public enum TransitionMode { Cycle, Random }
    [SerializeField]
    TransitionMode transitionMode; // 当前过渡模式

    [SerializeField]
    Material material; // 用于渲染的材质

    [SerializeField]
    Mesh mesh; // 用于显示的网格

    [SerializeField]
    ComputeShader computeShader; // 用于计算的计算着色器
    ComputeBuffer positionsBuffer; // 存储点位置信息的缓冲区

    static readonly int
         positionsId = Shader.PropertyToID("_Positions"), // 定义着色器属性 ID
         resolutionId = Shader.PropertyToID("_Resolution"),
         stepId = Shader.PropertyToID("_Step"),
         timeId = Shader.PropertyToID("_Time"),
         transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    // 在启用时创建计算缓冲区
    void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4); // 创建缓冲区以存储顶点位置
    }

    // 在禁用时释放计算缓冲区
    void OnDisable()
    {
        positionsBuffer.Release(); // 释放缓冲区
        positionsBuffer = null; // 将缓冲区置为 null
    }

    // 更新 GPU 上的函数
    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution; // 计算步长，根据分辨率调整
        computeShader.SetInt(resolutionId, resolution); // 设置分辨率
        computeShader.SetFloat(stepId, step); // 设置步长
        computeShader.SetFloat(timeId, Time.time); // 将当前时间传递给着色器

        // 如果正在过渡状态，则计算过渡进度
        if (transitioning)
        {
            computeShader.SetFloat(
                transitionProgressId,
                Mathf.SmoothStep(0f, 1f, duration / transitionDuration) // 线性插值
            );
        }

        // 计算当前要使用的内核索引
        var kernelIndex =
            (int)function + (int)(transitioning ? transitionFunction : function) * FunctionLibrary.FunctionCount;

        // 设置缓冲区
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);
        int groups = Mathf.CeilToInt(resolution / 8f); // 计算要调度的组数
        computeShader.Dispatch(kernelIndex, groups, groups, 1); // 调度计算着色器

        material.SetBuffer(positionsId, positionsBuffer); // 将缓冲区传递给材质
        material.SetFloat(stepId, step); // 设置步长
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution)); // 计算边界
        Graphics.DrawMeshInstancedProcedural(
            mesh, 0, material, bounds, resolution * resolution // 通过实例化绘制网格
        );
    }

    // 随机选择下一个函数或从现有函数中循环选择
    void PickNextFunction()
    {
        // 根据过渡模式选择下一个函数
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) : // 循环选择下一个函数
            FunctionLibrary.GetRandomFunctionNameOtherThan(function); // 随机选择不同于当前函数的函数
    }

    // 每帧调用的方法，用于更新函数和点的位置
    void Update()
    {
        duration += Time.deltaTime; // 更新持续时间
        if (transitioning) // 如果当前处于过渡状态
        {
            if (duration >= transitionDuration) // 检查是否达到过渡持续时间
            {
                duration -= transitionDuration; // 减去过渡持续时间
                transitioning = false; // 结束过渡状态
            }
        }
        else if (duration >= functionDuration) // 如果不在过渡状态并且达到函数持续时间
        {
            duration -= functionDuration; // 减去函数持续时间
            transitioning = true; // 开始过渡状态
            transitionFunction = function; // 存储当前函数
            PickNextFunction(); // 选择下一个函数
        }
        UpdateFunctionOnGPU(); // 更新 GPU 上的函数
    }
}
