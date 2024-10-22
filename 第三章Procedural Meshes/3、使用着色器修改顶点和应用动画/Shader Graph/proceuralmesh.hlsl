// 启用实例化渲染的多编译选项
#pragma multi_compile_instancing

// 定义 PI 的常量值
#define PI 3.14159265358979323846 // 定义圆周率常量 PI

// 定义波动效果的函数 Ripple_float
void Ripple_float(
    float3 PositionIn, // 输入顶点的位置
    float3 Origin, // 波的原点（波纹中心）
    float Period, // 波纹的周期
    float Speed, // 波纹传播的速度
    float Amplitude, // 波纹的振幅（波高）
    out float3 PositionOut, // 输出的顶点位置
    out float3 NormalOut, // 输出的法线方向
    out float3 TangentOut // 输出的切线方向
)
{
    // 计算输入顶点相对于波纹原点的位置向量
    float3 p = PositionIn - Origin;
    
    // 计算距离，即顶点与波纹中心之间的距离
    float d = length(p);
    
    // 计算正弦函数的输入参数 f，使用波的周期、速度和当前时间进行计算
    float f = 2.0 * PI * Period * (d - Speed * _Time.y);
    
    // 通过正弦函数修改顶点的 Y 位置，生成波动效果
    PositionOut = PositionIn + float3(0.0, Amplitude * sin(f), 0.0);
    
    // 计算波动效果的导数（偏导数），用于计算切线和法线
    // 使用 cos(f) 来获取相应位置处的斜率（波的变化率），并通过 d（距离）归一化
    float2 derivatives = (2.0 * PI * Amplitude * Period * cos(f) / max(d, 0.0001)) * p.xz;
    
    // 设置切线方向：根据 X 轴方向和波动的斜率
    TangentOut = float3(1.0, derivatives.x, 0.0);
    
    // 计算法线，使用叉积来计算法线方向，确保法线垂直于切线
    NormalOut = cross(float3(0.0, derivatives.y, 1.0), TangentOut);
}
