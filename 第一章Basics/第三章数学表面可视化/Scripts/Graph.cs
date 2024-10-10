using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // 用于存储生成的点的 Transform
    Transform[] points;

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

    // 在每一帧调用，用于更新点的位置
    void Update()
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
            points[i].localPosition = f(u, v, time);
        }
    }
}
