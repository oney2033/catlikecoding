using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf; // ʹ�� Unity �� Mathf ��̬�࣬����ֱ�ӵ�����ѧ����

// ����һ����̬�ĺ������࣬�������ɲ�ͬ����ά����ͼ��
public static class FunctionLibrary
{
    // ����һ��ί�����ͣ���ʾһ����������������������� Vector3 �ĺ���
    public delegate Vector3 Function(float u, float v, float t);

    // ����ö�٣�������ͬ�ĺ������ƣ�����ѡ��ͬ�ĺ���
    public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus }

    // һ����̬���飬������ж���ĺ�������ö�ٵ�˳��
    static Function[] functions = { Wave, MultiWave, Ripple, Sphere, Torus };

    // ��ȡ��һ���������ƣ�ѭ���ص���һ��
    public static FunctionName GetNextFunctionName(FunctionName name) =>
            (int)name < functions.Length - 1 ? name + 1 : 0;
    public static int FunctionCount => functions.Length;

    // ��ȡ����ĺ������ƣ������������������
    public static FunctionName GetRandomFunctionNameOtherThan(FunctionName name)
    {
        // ���ѡ��һ���������ƣ�ȷ�������ڴ��������
        var choice = (FunctionName)Random.Range(1, functions.Length); // �� 1 ��ʼ������ѡ���һ����Wave��
        return choice == name ? 0 : choice; // ���ѡ����Ǵ�������ƣ��򷵻ص�һ��
    }

    // ���ݴ����ö��ֵ�����ض�Ӧ�ĺ���
    public static Function GetFunction(FunctionName name) => functions[(int)name];

    // ����������֮����в�ֵ�������µ�λ��
    public static Vector3 Morph(float u, float v, float t, Function from, Function to, float progress)
    {
        // ʹ�� LerpUnclamped �������в�ֵ�������ƽ���� 0 �� 1 ֮��
        return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
    }

    // ����һ�����棨Torus���ĺ��������ڲ��� u �� v ���� 3D ���꣬����ʱ�� t ������̬�仯
    public static Vector3 Torus(float u, float v, float t)
    {
        // r1 ���������İ뾶���������ұ仯
        float r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
        // r2 ���ƴλ�������棩�İ뾶��Ҳ�������ұ仯
        float r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));
        // s �� r1 �� r2 ����ϣ����ڼ��� x �� z ����
        float s = r1 + r2 * Cos(PI * v);

        // ����һ�� Vector3 p ���洢 3D ����
        Vector3 p;
        // ���� x ����
        p.x = s * Sin(PI * u);
        // ���� y ����
        p.y = r2 * Sin(PI * v);
        // ���� z ����
        p.z = s * Cos(PI * u);

        return p; // ���ؼ������ 3D ����
    }

    // ����һ�����壨Sphere���ĺ��������ڲ��� u �� v ���� 3D ���꣬����ʱ�� t ������̬�仯
    public static Vector3 Sphere(float u, float v, float t)
    {
        // r ����İ뾶���������ұ仯
        float r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
        // s �� r �ͽǶ� v ����ϣ����ڼ��� x �� z ����
        float s = r * Cos(0.5f * PI * v);

        // ����һ�� Vector3 p ���洢 3D ����
        Vector3 p;
        // ���� x ����
        p.x = s * Sin(PI * u);
        // ���� y ����
        p.y = r * Sin(0.5f * PI * v);
        // ���� z ����
        p.z = s * Cos(PI * u);

        return p; // ���ؼ������ 3D ����
    }

    // ����һ�����ˣ�Wave���ĺ��������ڲ��� u �� v ���� 3D ���꣬����ʱ�� t ������̬�仯
    public static Vector3 Wave(float u, float v, float t)
    {
        // ����һ�� Vector3 p ���洢 3D ����
        Vector3 p;
        // ���� x ���ֱ꣬��ʹ�� u
        p.x = u;
        // ���� y ���꣬ʹ�����Һ��������� u, v �� t ��������ɲ���Ч��
        p.y = Sin(PI * (u + v + t));
        // ���� z ���ֱ꣬��ʹ�� v
        p.z = v;

        return p; // ���ؼ������ 3D ����
    }

    // ����һ�����ز��ˣ�MultiWave���ĺ��������ڲ��� u �� v ���� 3D ���꣬����ʱ�� t ������̬�仯
    public static Vector3 MultiWave(float u, float v, float t)
    {
        // ����һ�� Vector3 p ���洢 3D ����
        Vector3 p;
        // ���� x ���ֱ꣬��ʹ�� u
        p.x = u;
        // ���� y ���꣬ʹ�ö�����Ҳ��ĵ��������ɸ��Ӳ���Ч��
        p.y = Sin(PI * (u + 0.5f * t));
        p.y += 0.5f * Sin(2f * PI * (v + t));
        p.y += Sin(PI * (u + v + 0.25f * t));
        // �� y ���갴������С��ʹ���˸߶�����
        p.y *= 1f / 2.5f;
        // ���� z ���ֱ꣬��ʹ�� v
        p.z = v;

        return p; // ���ؼ������ 3D ����
    }

    // ����һ�����ƣ�Ripple���ĺ��������ڲ��� u �� v ���� 3D ���꣬����ʱ�� t ������̬�仯
    public static Vector3 Ripple(float u, float v, float t)
    {
        // ������� d����ʾ�����ĵ� (0,0) ���� (u,v) �ľ���
        float d = Sqrt(u * u + v * v);

        // ����һ�� Vector3 p ���洢 3D ����
        Vector3 p;
        // ���� x ���ֱ꣬��ʹ�� u
        p.x = u;
        // ���� y ���꣬ʹ�����Һ�����ģ�Ⲩ��Ч��
        p.y = Sin(PI * (4f * d - t));
        // y �������һ������� d �ɱ�����ֵ��ʹ�������ž�������Ӷ�˥��
        p.y /= 1f + 10f * d;
        // ���� z ���ֱ꣬��ʹ�� v
        p.z = v;

        return p; // ���ؼ������ 3D ����
    }
}
