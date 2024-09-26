using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FunctionLibrary; // 引入 FunctionLibrary 中的静态成员，以方便使用

public class Graph : MonoBehaviour
{
    // 使用 [SerializeField] 使得变量在 Unity 编辑器中可见，并允许设置不同的 prefab
    [SerializeField]
    Transform pointPrefab; // 用于生成图形点的 prefab（预制件）

    // 使用 [Range] 属性在编辑器中限制分辨率的范围为 10 到 100
    [SerializeField, Range(10, 100)]
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

    // 用于存储生成的点的 Transform
    Transform[] points; // 存储所有生成点的数组

    // 定义过渡模式的枚举（循环或随机）
    public enum TransitionMode { Cycle, Random }
    [SerializeField]
    TransitionMode transitionMode; // 当前过渡模式

    // 在脚本开始时调用，用于初始化点的网格
    void Awake()
    {
        // step 定义了每个点在网格中的间距（2 是因为我们假定坐标范围是 [-1, 1]）
        float step = 2f / resolution;

        // scale 确定每个点的缩放比例，使其大小与网格间距相匹配
        var scale = Vector3.one * step;

        // 创建一个 Transform 数组，用于存储所有点的位置
        points = new Transform[resolution * resolution];

        // 循环遍历每一个点，实例化它们并设置缩放和父级
        for (int i = 0; i < points.Length; i++)
        {
            // 实例化点并存储在 points 数组中
            Transform point = points[i] = Instantiate(pointPrefab);

            // 设置每个点的缩放比例，使得点的大小和网格一致
            point.localScale = scale;

            // 将点设置为当前 Graph 对象的子对象，保持世界空间不变
            point.SetParent(transform, false);
        }
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
        // 更新函数的位置
        if (transitioning)
        {
            UpdateFunctionTransition(); // 更新过渡函数的位置
        }
        else
        {
            UpdateFunction(); // 更新当前函数的位置
        }
    }

    // 在过渡期间调用，用于更新点的位置
    void UpdateFunctionTransition()
    {
        // 获取当前和目标函数
        FunctionLibrary.Function from = FunctionLibrary.GetFunction(transitionFunction),
                                   to = FunctionLibrary.GetFunction(function);
        float progress = duration / transitionDuration; // 计算过渡进度
        float time = Time.time; // 当前时间
        float step = 2f / resolution; // 网格步长
        float v = 0.5f * step - 1f; // 初始化 v 坐标，表示当前行的位置

        // 使用双层循环更新每个点的位置
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            // 如果 x 达到了分辨率的边界，重置 x 并增加 z 以进入下一行
            if (x == resolution)
            {
                x = 0; // 重置 x
                z += 1; // 增加 z，进入下一行
                // 更新 v 坐标，用于表示当前行的位置
                v = (z + 0.5f) * step - 1f;
            }

            // 计算 u 坐标，表示当前列的位置
            float u = (x + 0.5f) * step - 1f;

            // 使用函数 Morph 计算新的点的位置，传入 u, v 和时间 t 作为参数
            points[i].localPosition = FunctionLibrary.Morph(
                 u, v, time, from, to, progress // 进行位置插值
             );
        }
    }

    // 在每一帧调用，用于更新点的位置
    void UpdateFunction()
    {
        // 获取当前时间，以便用于计算点的位置随时间变化
        float time = Time.time;

        // 根据选择的函数名，获取相应的函数
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);

        // 再次计算网格步长，用于确定每个点的位置
        float step = 2f / resolution;

        // 初始化 v 坐标，表示当前行的位置，初始值对应于第一行
        float v = 0.5f * step - 1f;

        // 使用双层循环更新每个点的位置
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            // 如果 x 达到了分辨率的边界，重置 x 并增加 z 以进入下一行
            if (x == resolution)
            {
                x = 0; // 重置 x
                z += 1; // 增加 z，进入下一行
                // 更新 v 坐标，用于表示当前行的位置
                v = (z + 0.5f) * step - 1f;
            }

            // 计算 u 坐标，表示当前列的位置
            float u = (x + 0.5f) * step - 1f;

            // 使用函数 f 计算新的点的位置，传入 u, v 和时间 t 作为参数
            points[i].localPosition = f(u, v, time); // 更新点的位置
        }
    }
}
