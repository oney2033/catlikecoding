using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FunctionLibrary; // ���� FunctionLibrary �еľ�̬��Ա���Է���ʹ��

public class Graph : MonoBehaviour
{
    // ʹ�� [SerializeField] ʹ�ñ����� Unity �༭���пɼ������������ò�ͬ�� prefab
    [SerializeField]
    Transform pointPrefab; // ��������ͼ�ε�� prefab��Ԥ�Ƽ���

    // ʹ�� [Range] �����ڱ༭�������Ʒֱ��ʵķ�ΧΪ 10 �� 100
    [SerializeField, Range(10, 100)]
    int resolution = 10; // ����ķֱ��ʣ�����������

    // ѡ��Ҫʹ�õĺ�����ö��ֵ FunctionName �� FunctionLibrary �ж���
    [SerializeField]
    FunctionLibrary.FunctionName function;

    // ���庯���ĳ���ʱ��͹��ɳ���ʱ�䣬��СֵΪ 0
    [SerializeField, Min(0f)]
    float functionDuration = 1f, transitionDuration = 1f; // �ֱ����ں�������ʾ����ʱ��ͺ����л��ĳ���ʱ��
    float duration; // ��ǰ����ʱ�����

    bool transitioning; // ��ǵ�ǰ�Ƿ��ں�������״̬

    FunctionLibrary.FunctionName transitionFunction; // ���ڴ洢�л�ǰ�ĺ�����

    // ���ڴ洢���ɵĵ�� Transform
    Transform[] points; // �洢�������ɵ������

    // �������ģʽ��ö�٣�ѭ���������
    public enum TransitionMode { Cycle, Random }
    [SerializeField]
    TransitionMode transitionMode; // ��ǰ����ģʽ

    // �ڽű���ʼʱ���ã����ڳ�ʼ���������
    void Awake()
    {
        // step ������ÿ�����������еļ�ࣨ2 ����Ϊ���Ǽٶ����귶Χ�� [-1, 1]��
        float step = 2f / resolution;

        // scale ȷ��ÿ��������ű�����ʹ���С����������ƥ��
        var scale = Vector3.one * step;

        // ����һ�� Transform ���飬���ڴ洢���е��λ��
        points = new Transform[resolution * resolution];

        // ѭ������ÿһ���㣬ʵ�������ǲ��������ź͸���
        for (int i = 0; i < points.Length; i++)
        {
            // ʵ�����㲢�洢�� points ������
            Transform point = points[i] = Instantiate(pointPrefab);

            // ����ÿ��������ű�����ʹ�õ�Ĵ�С������һ��
            point.localScale = scale;

            // ��������Ϊ��ǰ Graph ������Ӷ��󣬱�������ռ䲻��
            point.SetParent(transform, false);
        }
    }

    // ���ѡ����һ������������к�����ѭ��ѡ��
    void PickNextFunction()
    {
        // ���ݹ���ģʽѡ����һ������
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) : // ѭ��ѡ����һ������
            FunctionLibrary.GetRandomFunctionNameOtherThan(function); // ���ѡ��ͬ�ڵ�ǰ�����ĺ���
    }

    // ÿ֡���õķ��������ڸ��º����͵��λ��
    void Update()
    {
        duration += Time.deltaTime; // ���³���ʱ��
        if (transitioning) // �����ǰ���ڹ���״̬
        {
            if (duration >= transitionDuration) // ����Ƿ�ﵽ���ɳ���ʱ��
            {
                duration -= transitionDuration; // ��ȥ���ɳ���ʱ��
                transitioning = false; // ��������״̬
            }
        }
        else if (duration >= functionDuration) // ������ڹ���״̬���Ҵﵽ��������ʱ��
        {
            duration -= functionDuration; // ��ȥ��������ʱ��
            transitioning = true; // ��ʼ����״̬
            transitionFunction = function; // �洢��ǰ����
            PickNextFunction(); // ѡ����һ������
        }
        // ���º�����λ��
        if (transitioning)
        {
            UpdateFunctionTransition(); // ���¹��ɺ�����λ��
        }
        else
        {
            UpdateFunction(); // ���µ�ǰ������λ��
        }
    }

    // �ڹ����ڼ���ã����ڸ��µ��λ��
    void UpdateFunctionTransition()
    {
        // ��ȡ��ǰ��Ŀ�꺯��
        FunctionLibrary.Function from = FunctionLibrary.GetFunction(transitionFunction),
                                   to = FunctionLibrary.GetFunction(function);
        float progress = duration / transitionDuration; // ������ɽ���
        float time = Time.time; // ��ǰʱ��
        float step = 2f / resolution; // ���񲽳�
        float v = 0.5f * step - 1f; // ��ʼ�� v ���꣬��ʾ��ǰ�е�λ��

        // ʹ��˫��ѭ������ÿ�����λ��
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            // ��� x �ﵽ�˷ֱ��ʵı߽磬���� x ������ z �Խ�����һ��
            if (x == resolution)
            {
                x = 0; // ���� x
                z += 1; // ���� z��������һ��
                // ���� v ���꣬���ڱ�ʾ��ǰ�е�λ��
                v = (z + 0.5f) * step - 1f;
            }

            // ���� u ���꣬��ʾ��ǰ�е�λ��
            float u = (x + 0.5f) * step - 1f;

            // ʹ�ú��� Morph �����µĵ��λ�ã����� u, v ��ʱ�� t ��Ϊ����
            points[i].localPosition = FunctionLibrary.Morph(
                 u, v, time, from, to, progress // ����λ�ò�ֵ
             );
        }
    }

    // ��ÿһ֡���ã����ڸ��µ��λ��
    void UpdateFunction()
    {
        // ��ȡ��ǰʱ�䣬�Ա����ڼ�����λ����ʱ��仯
        float time = Time.time;

        // ����ѡ��ĺ���������ȡ��Ӧ�ĺ���
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);

        // �ٴμ������񲽳�������ȷ��ÿ�����λ��
        float step = 2f / resolution;

        // ��ʼ�� v ���꣬��ʾ��ǰ�е�λ�ã���ʼֵ��Ӧ�ڵ�һ��
        float v = 0.5f * step - 1f;

        // ʹ��˫��ѭ������ÿ�����λ��
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            // ��� x �ﵽ�˷ֱ��ʵı߽磬���� x ������ z �Խ�����һ��
            if (x == resolution)
            {
                x = 0; // ���� x
                z += 1; // ���� z��������һ��
                // ���� v ���꣬���ڱ�ʾ��ǰ�е�λ��
                v = (z + 0.5f) * step - 1f;
            }

            // ���� u ���꣬��ʾ��ǰ�е�λ��
            float u = (x + 0.5f) * step - 1f;

            // ʹ�ú��� f �����µĵ��λ�ã����� u, v ��ʱ�� t ��Ϊ����
            points[i].localPosition = f(u, v, time); // ���µ��λ��
        }
    }
}
