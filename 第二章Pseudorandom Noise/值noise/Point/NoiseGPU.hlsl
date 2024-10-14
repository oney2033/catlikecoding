// ����ʵ������Ⱦ�Ķ����ѡ��
#pragma multi_compile_instancing

// ��������˳���ʵ����������ṹ��������
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // �������ݵĽṹ��������
    StructuredBuffer<float> _Noise;
    // λ�úͷ��ߵĽṹ��������
    StructuredBuffer<float3> _Positions, _Normals;
#endif

// ���ò�����4ά����
float4 _Config;

// ���ó���ʵ�����ı任����
void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // ��ʼ���任����Ϊ��
    unity_ObjectToWorld = 0.0;
    // ���ñ任�����λ�ò��֣�ʹ�õ�ǰʵ����λ����Ϣ
    unity_ObjectToWorld._m03_m13_m23_m33 = float4(
        _Positions[unity_InstanceID], // �� _Positions ��������ȡ��ǰʵ����λ��
        1.0
    );
    // ��������ֵ�ͷ��ߵ����任�����ƽ�Ʋ���
    unity_ObjectToWorld._m03_m13_m23 +=
        _Config.z * _Noise[unity_InstanceID] * _Normals[unity_InstanceID];
    // ���ñ任��������Ų���
    unity_ObjectToWorld._m00_m11_m22 = _Config.y;
#endif
}

// ��ȡ������ɫ
float3 GetNoiseColor()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // ��������������ȡ��ǰʵ��������ֵ
    float noise = _Noise[unity_InstanceID];
    // ��������ֵ������ɫ��������Ϊ�����򷵻ظ�������Ϊ��ɫ����
    return noise < 0.0 ? float3(-noise, 0.0, 0.0) : noise;
#else
    // ���δ����ʵ���������ذ�ɫ
    return 1.0;
#endif
}

// ShaderGraph ���������� float ���͵�����
void ShaderGraphFunction_float(float3 In, out float3 Out, out float3 Color)
{
    Out = In; // ������ֱ�Ӵ��ݸ����
    Color = GetNoiseColor(); // ��ȡ������ɫ
}

// ShaderGraph ���������� half ���͵�����
void ShaderGraphFunction_half(half3 In, out half3 Out, out half3 Color)
{
    Out = In; // ������ֱ�Ӵ��ݸ����
    Color = GetNoiseColor(); // ��ȡ������ɫ
}
