using Unity.Burst;  // 引入 Burst 编译器命名空间
using Unity.Collections;  // 引入 Unity 的集合库
using Unity.Jobs;  // 引入 Unity 的作业系统
using UnityEngine;  // 引入 Unity 引擎的核心功能

namespace ProceduralMeshes // 自定义命名空间
{
    // 使用 Burst 编译器编译这个结构体，提高性能
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MeshJob<G, S> : IJobFor
        where G : struct, IMeshGenerator // G 必须是一个结构体，并实现 IMeshGenerator 接口
        where S : struct, IMeshStreams // S 必须是一个结构体，并实现 IMeshStreams 接口
    {
        G generator; // 生成器实例，用于生成网格数据
        [WriteOnly] // 指示该字段只能写入，不会读取
        S streams; // 网格流数据，用于存储顶点和三角形数据

        // 执行作业时调用生成器的 Execute 方法
        public void Execute(int i) => generator.Execute(i, streams);

        // 调度并行作业的静态方法
        public static JobHandle ScheduleParallel(
           Mesh mesh, // 输入的网格对象
           Mesh.MeshData meshData, // 网格数据
           int resolution, // 网格分辨率
           JobHandle dependency // 作业依赖
        )
        {
            var job = new MeshJob<G, S>(); // 创建一个新的 MeshJob 实例
            job.generator.Resolution = resolution; // 设置生成器的分辨率
            job.streams.Setup( // 设置网格流
                meshData,
                mesh.bounds = job.generator.Bounds, // 设置网格边界
                job.generator.VertexCount, // 设置顶点数量
                job.generator.IndexCount // 设置索引数量
            );
            // 调度并返回作业句柄
            return job.ScheduleParallel(job.generator.JobLength, 1, dependency);
        }
    }

    public delegate JobHandle MeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, JobHandle dependency
    );
}
