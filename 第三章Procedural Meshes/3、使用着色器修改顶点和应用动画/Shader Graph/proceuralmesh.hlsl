// ����ʵ������Ⱦ�Ķ����ѡ��
#pragma multi_compile_instancing

// ���� PI �ĳ���ֵ
#define PI 3.14159265358979323846 // ����Բ���ʳ��� PI

// ���岨��Ч���ĺ��� Ripple_float
void Ripple_float(
    float3 PositionIn, // ���붥���λ��
    float3 Origin, // ����ԭ�㣨�������ģ�
    float Period, // ���Ƶ�����
    float Speed, // ���ƴ������ٶ�
    float Amplitude, // ���Ƶ���������ߣ�
    out float3 PositionOut, // ����Ķ���λ��
    out float3 NormalOut, // ����ķ��߷���
    out float3 TangentOut // ��������߷���
)
{
    // �������붥������ڲ���ԭ���λ������
    float3 p = PositionIn - Origin;
    
    // ������룬�������벨������֮��ľ���
    float d = length(p);
    
    // �������Һ������������ f��ʹ�ò������ڡ��ٶȺ͵�ǰʱ����м���
    float f = 2.0 * PI * Period * (d - Speed * _Time.y);
    
    // ͨ�����Һ����޸Ķ���� Y λ�ã����ɲ���Ч��
    PositionOut = PositionIn + float3(0.0, Amplitude * sin(f), 0.0);
    
    // ���㲨��Ч���ĵ�����ƫ�����������ڼ������ߺͷ���
    // ʹ�� cos(f) ����ȡ��Ӧλ�ô���б�ʣ����ı仯�ʣ�����ͨ�� d�����룩��һ��
    float2 derivatives = (2.0 * PI * Amplitude * Period * cos(f) / max(d, 0.0001)) * p.xz;
    
    // �������߷��򣺸��� X �᷽��Ͳ�����б��
    TangentOut = float3(1.0, derivatives.x, 0.0);
    
    // ���㷨�ߣ�ʹ�ò�������㷨�߷���ȷ�����ߴ�ֱ������
    NormalOut = cross(float3(0.0, derivatives.y, 1.0), TangentOut);
}
