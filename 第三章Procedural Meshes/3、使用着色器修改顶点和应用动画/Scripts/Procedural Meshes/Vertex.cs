using Unity.Mathematics;

namespace ProceduralMeshes
{
    // 顶点结构体，包含了位置、法线、切线和纹理坐标等属性
    public struct Vertex
    {
        // 顶点位置，表示顶点在三维空间中的位置
        public float3 position; // float3 表示一个包含 X, Y, Z 轴的三维向量

        // 顶点法线，表示顶点处的法线方向，用于光照计算
        public float3 normal;   // float3 表示一个三维向量，用于确定光线反射方向

        // 顶点切线，表示表面切线方向，常用于法线贴图
        public float4 tangent;  // float4 表示一个四维向量，前三个分量为方向，第四个为手性

        // 顶点的第一个纹理坐标，用于纹理映射
        public float2 texCoord0; // float2 表示二维向量，对应 U, V 纹理坐标
    }
}
