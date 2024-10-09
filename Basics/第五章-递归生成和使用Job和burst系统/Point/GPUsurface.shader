Shader "Custom/GPUsurface"
{
    // 定义可调节的属性，允许用户在 Unity 编辑器中修改
    Properties 
    {
        // 定义了一个名为 _Smoothness 的浮点数范围（0 到 1），用于控制材质的光滑度
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

    // 定义 SubShader，它包含渲染逻辑
    SubShader 
    {
        // 声明使用 Cg/HLSL 代码编写的代码块
        CGPROGRAM

        // 定义 Surface Shader，使用标准渲染路径并支持全局前向阴影
        #pragma surface ConfigureSurface Standard fullforwardshadows
        // 指定实例化选项，假设统一缩放，并设置程序实例化的配置函数和阴影
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural addshadow
        // 允许编辑器同步编译
        #pragma editor_sync_compilation
        // 指定 Shader 的编译目标为 4.5，以支持更多图形特性
        #pragma target 4.5

        // 定义输入结构体 Input，它会从渲染管线中接收数据
        struct Input 
        {
            // 输入的世界坐标位置，类型为 float3（3D 向量）
            float3 worldPos;
        };

        // 声明一个名为 _Smoothness 的浮点数，用于控制表面的光滑度
        float _Smoothness;

        // 引入外部 HLSL 文件，可能包含其他功能或工具函数
        #include "PointGPU.hlsl"

        // 核心函数 ConfigureSurface，用于配置表面的视觉效果
        void ConfigureSurface (Input input, inout SurfaceOutputStandard surface)
        {
            // 计算表面的颜色（Albedo），通过将世界坐标转换为范围 [0, 1]
            // saturate() 函数确保结果被限制在 [0, 1] 范围内，防止溢出
            surface.Albedo = saturate(input.worldPos * 0.5 + 0.5);

            // 使用 _Smoothness 属性设置表面的光滑度
            surface.Smoothness = _Smoothness;
        }

        // 结束 CG 代码块
        ENDCG
    }
    
    // 当 Shader 不适用时，回退到 "Diffuse" Shader
    FallBack "Diffuse"
}
