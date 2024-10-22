using UnityEngine; // 引入 Unity 引擎命名空间，提供 Unity 的核心功能，例如 Bounds 类型。

namespace ProceduralMeshes // 声明一个命名空间，称为 ProceduralMeshes，用于组织相关的网格生成代码。
{
    // 定义一个网格生成器接口 IMeshGenerator
    public interface IMeshGenerator
    {
        // Execute 方法：根据给定的索引 i，执行网格生成操作
        // 泛型 S 必须是一个结构体，并且实现了 IMeshStreams 接口
        void Execute<S>(int i, S streams) where S : struct, IMeshStreams;

        // 属性：网格的顶点数量
        int VertexCount { get; }

        // 属性：网格的索引数量（用于定义三角形）
        int IndexCount { get; }

        // 属性：工作（Job）的长度，通常与 Resolution 相关，表示有多少任务需要执行
        int JobLength { get; }

        // 属性：网格的包围盒（Bounds），用于定义网格的边界
        Bounds Bounds { get; }

        // 属性：网格生成的分辨率，用于控制网格的细节程度
        int Resolution { get; set; }
    }
}
