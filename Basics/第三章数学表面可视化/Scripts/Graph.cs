using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // ���ڴ洢���ɵĵ�� Transform
    Transform[] points;

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

    // ��ÿһ֡���ã����ڸ��µ��λ��
    void Update()
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
            points[i].localPosition = f(u, v, time);
        }
    }
}
