// 启用实例化渲染的多编译选项
#pragma multi_compile_instancing

// 如果启用了程序化实例化，则定义结构化缓冲区
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // 噪声数据的结构化缓冲区
    StructuredBuffer<float> _Noise;
    // 位置和法线的结构化缓冲区
    StructuredBuffer<float3> _Positions, _Normals;
#endif

// 配置参数的4维向量
float4 _Config;

// 配置程序化实例化的变换矩阵
void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // 初始化变换矩阵为零
    unity_ObjectToWorld = 0.0;
    // 设置变换矩阵的位置部分，使用当前实例的位置信息
    unity_ObjectToWorld._m03_m13_m23_m33 = float4(
        _Positions[unity_InstanceID], // 从 _Positions 缓冲区获取当前实例的位置
        1.0
    );
    // 根据噪声值和法线调整变换矩阵的平移部分
    unity_ObjectToWorld._m03_m13_m23 +=
        _Config.z * _Noise[unity_InstanceID] * _Normals[unity_InstanceID];
    // 设置变换矩阵的缩放部分
    unity_ObjectToWorld._m00_m11_m22 = _Config.y;
#endif
}

// 获取噪声颜色
float3 GetNoiseColor()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // 从噪声缓冲区获取当前实例的噪声值
    float noise = _Noise[unity_InstanceID];
    // 根据噪声值计算颜色，若噪声为负，则返回负噪声作为红色分量
    return noise < 0.0 ? float3(-noise, 0.0, 0.0) : noise;
#else
    // 如果未启用实例化，返回白色
    return 1.0;
#endif
}

// ShaderGraph 函数，处理 float 类型的输入
void ShaderGraphFunction_float(float3 In, out float3 Out, out float3 Color)
{
    Out = In; // 将输入直接传递给输出
    Color = GetNoiseColor(); // 获取噪声颜色
}

// ShaderGraph 函数，处理 half 类型的输入
void ShaderGraphFunction_half(half3 In, out half3 Out, out half3 Color)
{
    Out = In; // 将输入直接传递给输出
    Color = GetNoiseColor(); // 获取噪声颜色
}
