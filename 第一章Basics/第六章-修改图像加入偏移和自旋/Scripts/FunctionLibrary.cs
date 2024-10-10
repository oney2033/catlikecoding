using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf; // 使用 Unity 的 Mathf 静态类，便于直接调用数学函数

// 定义一个静态的函数库类，用于生成不同的三维函数图形
public static class FunctionLibrary
{
    // 定义一个委托类型，表示一个接收三个浮点参数并返回 Vector3 的函数
    public delegate Vector3 Function(float u, float v, float t);

    // 定义枚举，包含不同的函数名称，用于选择不同的函数
    public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus }

    // 一个静态数组，存放所有定义的函数，按枚举的顺序
    static Function[] functions = { Wave, MultiWave, Ripple, Sphere, Torus };

    // 获取下一个函数名称，循环回到第一个
    public static FunctionName GetNextFunctionName(FunctionName name) =>
            (int)name < functions.Length - 1 ? name + 1 : 0;
    public static int FunctionCount => functions.Length;

    // 获取随机的函数名称，但不包括传入的名称
    public static FunctionName GetRandomFunctionNameOtherThan(FunctionName name)
    {
        // 随机选择一个函数名称，确保不等于传入的名称
        var choice = (FunctionName)Random.Range(1, functions.Length); // 从 1 开始，避免选择第一个（Wave）
        return choice == name ? 0 : choice; // 如果选择的是传入的名称，则返回第一个
    }

    // 根据传入的枚举值，返回对应的函数
    public static Function GetFunction(FunctionName name) => functions[(int)name];

    // 在两个函数之间进行插值，返回新的位置
    public static Vector3 Morph(float u, float v, float t, Function from, Function to, float progress)
    {
        // 使用 LerpUnclamped 函数进行插值，不限制结果在 0 到 1 之间
        return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
    }

    // 定义一个环面（Torus）的函数，基于参数 u 和 v 生成 3D 坐标，并随时间 t 产生动态变化
    public static Vector3 Torus(float u, float v, float t)
    {
        // r1 控制主环的半径，带有正弦变化
        float r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
        // r2 控制次环（横截面）的半径，也带有正弦变化
        float r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));
        // s 是 r1 和 r2 的组合，用于计算 x 和 z 坐标
        float s = r1 + r2 * Cos(PI * v);

        // 定义一个 Vector3 p 来存储 3D 坐标
        Vector3 p;
        // 计算 x 坐标
        p.x = s * Sin(PI * u);
        // 计算 y 坐标
        p.y = r2 * Sin(PI * v);
        // 计算 z 坐标
        p.z = s * Cos(PI * u);

        return p; // 返回计算出的 3D 坐标
    }

    // 定义一个球体（Sphere）的函数，基于参数 u 和 v 生成 3D 坐标，并随时间 t 产生动态变化
    public static Vector3 Sphere(float u, float v, float t)
    {
        // r 是球的半径，带有正弦变化
        float r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
        // s 是 r 和角度 v 的组合，用于计算 x 和 z 坐标
        float s = r * Cos(0.5f * PI * v);

        // 定义一个 Vector3 p 来存储 3D 坐标
        Vector3 p;
        // 计算 x 坐标
        p.x = s * Sin(PI * u);
        // 计算 y 坐标
        p.y = r * Sin(0.5f * PI * v);
        // 计算 z 坐标
        p.z = s * Cos(PI * u);

        return p; // 返回计算出的 3D 坐标
    }

    // 定义一个波浪（Wave）的函数，基于参数 u 和 v 生成 3D 坐标，并随时间 t 产生动态变化
    public static Vector3 Wave(float u, float v, float t)
    {
        // 定义一个 Vector3 p 来存储 3D 坐标
        Vector3 p;
        // 计算 x 坐标，直接使用 u
        p.x = u;
        // 计算 y 坐标，使用正弦函数，基于 u, v 和 t 的组合生成波动效果
        p.y = Sin(PI * (u + v + t));
        // 计算 z 坐标，直接使用 v
        p.z = v;

        return p; // 返回计算出的 3D 坐标
    }

    // 定义一个多重波浪（MultiWave）的函数，基于参数 u 和 v 生成 3D 坐标，并随时间 t 产生动态变化
    public static Vector3 MultiWave(float u, float v, float t)
    {
        // 定义一个 Vector3 p 来存储 3D 坐标
        Vector3 p;
        // 计算 x 坐标，直接使用 u
        p.x = u;
        // 计算 y 坐标，使用多个正弦波的叠加来生成复杂波动效果
        p.y = Sin(PI * (u + 0.5f * t));
        p.y += 0.5f * Sin(2f * PI * (v + t));
        p.y += Sin(PI * (u + v + 0.25f * t));
        // 将 y 坐标按比例缩小，使波浪高度适中
        p.y *= 1f / 2.5f;
        // 计算 z 坐标，直接使用 v
        p.z = v;

        return p; // 返回计算出的 3D 坐标
    }

    // 定义一个波纹（Ripple）的函数，基于参数 u 和 v 生成 3D 坐标，并随时间 t 产生动态变化
    public static Vector3 Ripple(float u, float v, float t)
    {
        // 计算距离 d，表示从中心点 (0,0) 到点 (u,v) 的距离
        float d = Sqrt(u * u + v * v);

        // 定义一个 Vector3 p 来存储 3D 坐标
        Vector3 p;
        // 计算 x 坐标，直接使用 u
        p.x = u;
        // 计算 y 坐标，使用正弦函数来模拟波纹效果
        p.y = Sin(PI * (4f * d - t));
        // y 坐标除以一个与距离 d 成比例的值，使波纹随着距离的增加而衰减
        p.y /= 1f + 10f * d;
        // 计算 z 坐标，直接使用 v
        p.z = v;

        return p; // 返回计算出的 3D 坐标
    }
}
