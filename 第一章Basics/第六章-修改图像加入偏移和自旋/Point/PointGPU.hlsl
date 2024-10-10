#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float3> _Positions;
#endif

float _Step;

void ShaderGraphFunction_float(float3 In, out float3 Out)
{
    Out = In;
}

void ShaderGraphFunction_half(half3 In, out half3 Out)
{
    Out = In;
}

void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		float3 position = _Positions[unity_InstanceID];

		unity_ObjectToWorld = 0.0;
		unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
		unity_ObjectToWorld._m00_m11_m22 = _Step;
#endif
}

/*
// ��������˳���ʵ��������
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // ����һ���ṹ�������������ڴ洢������ά���꣨λ�ã�
    StructuredBuffer<float3> _Positions;
#endif

// ���������������ڼ����е���λ�û���������
float _Step;

// ����һ�����������������ά����ֱ�����
void ShaderGraphFunction_float(float3 In, out float3 Out)
{
    Out = In; // ֱ�ӽ����븳ֵ�����
}

// ����һ��������������İ뾫����ά����ֱ�����
void ShaderGraphFunction_half(half3 In, out half3 Out)
{
    Out = In; // ֱ�ӽ����븳ֵ�����
}

// ���ó���ʵ�����ĺ���
void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // �ӽṹ���������л�ȡ��ǰʵ����λ��
    float3 position = _Positions[unity_InstanceID];

    // ��ʼ�����嵽����ı任����
    unity_ObjectToWorld = 0.0; // �������ʼ��Ϊ�����

    // ���þ����ƽ�Ʋ��֣�ʹ��λ������
    unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);

    // ���þ�������Ų��֣�ʹ�ò���ֵ
    unity_ObjectToWorld._m00_m11_m22 = _Step;
#endif
}

*/