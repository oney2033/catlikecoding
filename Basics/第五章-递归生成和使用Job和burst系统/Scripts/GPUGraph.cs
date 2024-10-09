using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FunctionLibrary;

public class GPUGraph : MonoBehaviour
{
    const int maxResolution = 1000; // �������ֱ���Ϊ 1000
    // ʹ�� [Range] �����ڱ༭�������Ʒֱ��ʵķ�ΧΪ 10 �� 200
    [SerializeField, Range(10, maxResolution)]
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

    // �������ģʽ��ö�٣�ѭ���������
    public enum TransitionMode { Cycle, Random }
    [SerializeField]
    TransitionMode transitionMode; // ��ǰ����ģʽ

    [SerializeField]
    Material material; // ������Ⱦ�Ĳ���

    [SerializeField]
    Mesh mesh; // ������ʾ������

    [SerializeField]
    ComputeShader computeShader; // ���ڼ���ļ�����ɫ��
    ComputeBuffer positionsBuffer; // �洢��λ����Ϣ�Ļ�����

    static readonly int
         positionsId = Shader.PropertyToID("_Positions"), // ������ɫ������ ID
         resolutionId = Shader.PropertyToID("_Resolution"),
         stepId = Shader.PropertyToID("_Step"),
         timeId = Shader.PropertyToID("_Time"),
         transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    // ������ʱ�������㻺����
    void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4); // �����������Դ洢����λ��
    }

    // �ڽ���ʱ�ͷż��㻺����
    void OnDisable()
    {
        positionsBuffer.Release(); // �ͷŻ�����
        positionsBuffer = null; // ����������Ϊ null
    }

    // ���� GPU �ϵĺ���
    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution; // ���㲽�������ݷֱ��ʵ���
        computeShader.SetInt(resolutionId, resolution); // ���÷ֱ���
        computeShader.SetFloat(stepId, step); // ���ò���
        computeShader.SetFloat(timeId, Time.time); // ����ǰʱ�䴫�ݸ���ɫ��

        // ������ڹ���״̬���������ɽ���
        if (transitioning)
        {
            computeShader.SetFloat(
                transitionProgressId,
                Mathf.SmoothStep(0f, 1f, duration / transitionDuration) // ���Բ�ֵ
            );
        }

        // ���㵱ǰҪʹ�õ��ں�����
        var kernelIndex =
            (int)function + (int)(transitioning ? transitionFunction : function) * FunctionLibrary.FunctionCount;

        // ���û�����
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);
        int groups = Mathf.CeilToInt(resolution / 8f); // ����Ҫ���ȵ�����
        computeShader.Dispatch(kernelIndex, groups, groups, 1); // ���ȼ�����ɫ��

        material.SetBuffer(positionsId, positionsBuffer); // �����������ݸ�����
        material.SetFloat(stepId, step); // ���ò���
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution)); // ����߽�
        Graphics.DrawMeshInstancedProcedural(
            mesh, 0, material, bounds, resolution * resolution // ͨ��ʵ������������
        );
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
        UpdateFunctionOnGPU(); // ���� GPU �ϵĺ���
    }
}
