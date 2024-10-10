using System.Collections;
using System.Collections.Generic;
using TMPro; // 引入 TextMesh Pro 命名空间以使用文本组件
using UnityEngine;

public class FrameRateCounter : MonoBehaviour
{
    // 在 Unity 编辑器中引用显示帧率的 TextMeshProUGUI 组件
    [SerializeField]
    TextMeshProUGUI display;

    // 控制采样持续时间的属性，范围在 0.1 到 2 秒之间
    [SerializeField, Range(0.1f, 2f)]
    float sampleDuration = 1f; // 采样持续时间，单位为秒

    // 定义显示模式的枚举，支持 FPS 和毫秒 (MS) 两种显示方式
    public enum DisplayMode { FPS, MS }

    // 在编辑器中选择显示模式，默认为 FPS
    [SerializeField]
    DisplayMode displayMode = DisplayMode.FPS;

    // 记录帧数、总持续时间、最佳和最差帧时长
    int frames; // 当前帧数
    float duration; // 当前持续时间
    float bestDuration = float.MaxValue; // 最佳帧时长
    float worstDuration; // 最差帧时长

    // 每帧调用的方法，用于更新帧率计数
    void Update()
    {
        // 计算当前帧的持续时间
        float frameDuration = Time.unscaledDeltaTime;
        frames += 1; // 增加帧计数
        duration += frameDuration; // 累加持续时间

        // 更新最佳和最差帧时长
        if (frameDuration < bestDuration)
        {
            bestDuration = frameDuration; // 更新最佳帧时长
        }
        if (frameDuration > worstDuration)
        {
            worstDuration = frameDuration; // 更新最差帧时长
        }

        // 如果持续时间超过采样持续时间，进行更新
        if (duration >= sampleDuration)
        {
            // 根据当前显示模式更新文本内容
            if (displayMode == DisplayMode.FPS)
            {
                // 设置 FPS 文本，显示最佳帧率、平均帧率和最差帧率
                display.SetText(
                    "FPS\n{0:0}\n{1:0}\n{2:0}",
                    1f / bestDuration, // 最佳帧率
                    frames / duration, // 平均帧率
                    1f / worstDuration // 最差帧率
                );
            }
            else
            {
                // 设置 MS 文本，显示最佳时延、平均时延和最差时延（毫秒）
                display.SetText(
                   "MS\n{0:1}\n{1:1}\n{2:1}",
                    1000f * bestDuration, // 最佳时延（毫秒）
                    1000f * duration / frames, // 平均时延（毫秒）
                    1000f * worstDuration // 最差时延（毫秒）
                );
            }
            // 重置计数器和持续时间以准备下一个采样周期
            frames = 0; // 重置帧计数
            duration = 0f; // 重置持续时间
            bestDuration = float.MaxValue; // 重置最佳帧时长
            worstDuration = 0f; // 重置最差帧时长
        }
    }
}

