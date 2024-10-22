using Unity.Burst;  // ���� Burst �����������ռ�
using Unity.Collections;  // ���� Unity �ļ��Ͽ�
using Unity.Jobs;  // ���� Unity ����ҵϵͳ
using UnityEngine;  // ���� Unity ����ĺ��Ĺ���

namespace ProceduralMeshes // �Զ��������ռ�
{
    // ʹ�� Burst ��������������ṹ�壬�������
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MeshJob<G, S> : IJobFor
        where G : struct, IMeshGenerator // G ������һ���ṹ�壬��ʵ�� IMeshGenerator �ӿ�
        where S : struct, IMeshStreams // S ������һ���ṹ�壬��ʵ�� IMeshStreams �ӿ�
    {
        G generator; // ������ʵ��������������������
        [WriteOnly] // ָʾ���ֶ�ֻ��д�룬�����ȡ
        S streams; // ���������ݣ����ڴ洢���������������

        // ִ����ҵʱ������������ Execute ����
        public void Execute(int i) => generator.Execute(i, streams);

        // ���Ȳ�����ҵ�ľ�̬����
        public static JobHandle ScheduleParallel(
           Mesh mesh, // ������������
           Mesh.MeshData meshData, // ��������
           int resolution, // ����ֱ���
           JobHandle dependency // ��ҵ����
        )
        {
            var job = new MeshJob<G, S>(); // ����һ���µ� MeshJob ʵ��
            job.generator.Resolution = resolution; // �����������ķֱ���
            job.streams.Setup( // ����������
                meshData,
                mesh.bounds = job.generator.Bounds, // ��������߽�
                job.generator.VertexCount, // ���ö�������
                job.generator.IndexCount // ������������
            );
            // ���Ȳ�������ҵ���
            return job.ScheduleParallel(job.generator.JobLength, 1, dependency);
        }
    }

    public delegate JobHandle MeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, JobHandle dependency
    );
}
