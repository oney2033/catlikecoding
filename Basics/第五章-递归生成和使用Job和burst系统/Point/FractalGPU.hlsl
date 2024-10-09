#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float3x4> _Matrices;
#endif

void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		float3x4 m = _Matrices[unity_InstanceID];
		unity_ObjectToWorld._m00_m01_m02_m03 = m._m00_m01_m02_m03;
		unity_ObjectToWorld._m10_m11_m12_m13 = m._m10_m11_m12_m13;
		unity_ObjectToWorld._m20_m21_m22_m23 = m._m20_m21_m22_m23;
		unity_ObjectToWorld._m30_m31_m32_m33 = float4(0.0, 0.0, 0.0, 1.0);
#endif
}
void ShaderGraphFunction_float(float3 In, out float3 Out)
{
    Out = In;
}

void ShaderGraphFunction_half(half3 In, out half3 Out)
{
    Out = In;
}


/*
// 如果启用了程序实例化功能
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // 声明一个结构化缓冲区，用于存储浮点三维坐标（位置）
    StructuredBuffer<float3> _Positions;
#endif

// 步长变量，用于在计算中调整位置或其他属性
float _Step;

// 定义一个函数，将输入的三维向量直接输出
void ShaderGraphFunction_float(float3 In, out float3 Out)
{
    Out = In; // 直接将输入赋值给输出
}

// 定义一个函数，将输入的半精度三维向量直接输出
void ShaderGraphFunction_half(half3 In, out half3 Out)
{
    Out = In; // 直接将输入赋值给输出
}

// 配置程序实例化的函数
void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // 从结构化缓冲区中获取当前实例的位置
    float3 position = _Positions[unity_InstanceID];

    // 初始化物体到世界的变换矩阵
    unity_ObjectToWorld = 0.0; // 将矩阵初始化为零矩阵

    // 设置矩阵的平移部分，使用位置数据
    unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);

    // 设置矩阵的缩放部分，使用步长值
    unity_ObjectToWorld._m00_m11_m22 = _Step;
#endif
}

*/