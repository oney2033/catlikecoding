using System.Collections;
using System.Collections.Generic;
using TMPro; // ���� TextMesh Pro �����ռ���ʹ���ı����
using UnityEngine;

public class FrameRateCounter : MonoBehaviour
{
    // �� Unity �༭����������ʾ֡�ʵ� TextMeshProUGUI ���
    [SerializeField]
    TextMeshProUGUI display;

    // ���Ʋ�������ʱ������ԣ���Χ�� 0.1 �� 2 ��֮��
    [SerializeField, Range(0.1f, 2f)]
    float sampleDuration = 1f; // ��������ʱ�䣬��λΪ��

    // ������ʾģʽ��ö�٣�֧�� FPS �ͺ��� (MS) ������ʾ��ʽ
    public enum DisplayMode { FPS, MS }

    // �ڱ༭����ѡ����ʾģʽ��Ĭ��Ϊ FPS
    [SerializeField]
    DisplayMode displayMode = DisplayMode.FPS;

    // ��¼֡�����ܳ���ʱ�䡢��Ѻ����֡ʱ��
    int frames; // ��ǰ֡��
    float duration; // ��ǰ����ʱ��
    float bestDuration = float.MaxValue; // ���֡ʱ��
    float worstDuration; // ���֡ʱ��

    // ÿ֡���õķ��������ڸ���֡�ʼ���
    void Update()
    {
        // ���㵱ǰ֡�ĳ���ʱ��
        float frameDuration = Time.unscaledDeltaTime;
        frames += 1; // ����֡����
        duration += frameDuration; // �ۼӳ���ʱ��

        // ������Ѻ����֡ʱ��
        if (frameDuration < bestDuration)
        {
            bestDuration = frameDuration; // �������֡ʱ��
        }
        if (frameDuration > worstDuration)
        {
            worstDuration = frameDuration; // �������֡ʱ��
        }

        // �������ʱ�䳬����������ʱ�䣬���и���
        if (duration >= sampleDuration)
        {
            // ���ݵ�ǰ��ʾģʽ�����ı�����
            if (displayMode == DisplayMode.FPS)
            {
                // ���� FPS �ı�����ʾ���֡�ʡ�ƽ��֡�ʺ����֡��
                display.SetText(
                    "FPS\n{0:0}\n{1:0}\n{2:0}",
                    1f / bestDuration, // ���֡��
                    frames / duration, // ƽ��֡��
                    1f / worstDuration // ���֡��
                );
            }
            else
            {
                // ���� MS �ı�����ʾ���ʱ�ӡ�ƽ��ʱ�Ӻ����ʱ�ӣ����룩
                display.SetText(
                   "MS\n{0:1}\n{1:1}\n{2:1}",
                    1000f * bestDuration, // ���ʱ�ӣ����룩
                    1000f * duration / frames, // ƽ��ʱ�ӣ����룩
                    1000f * worstDuration // ���ʱ�ӣ����룩
                );
            }
            // ���ü������ͳ���ʱ����׼����һ����������
            frames = 0; // ����֡����
            duration = 0f; // ���ó���ʱ��
            bestDuration = float.MaxValue; // �������֡ʱ��
            worstDuration = 0f; // �������֡ʱ��
        }
    }
}

