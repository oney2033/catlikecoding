using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;  // ʹ��Burst���������������
using Unity.Collections;  // ���ڹ����ڴ��е�ԭ������
using Unity.Jobs;  // ���ڵ��Ⱥ͹�������ҵ
using Unity.Mathematics;  // ��ѧ��
using static Unity.Mathematics.math;  // ��̬����math�е���ѧ����
using quaternion = Unity.Mathematics.quaternion;  // �����ã�������UnityEngine.Quaternion��ͻ
using Random = UnityEngine.Random;

public class Fractal : MonoBehaviour
{
    // ʹ�� Burst ���������Ż����ܣ�����ͬ�����룬���ȱ�׼��ģʽ�ֱ�����Ϊ��׼�Ϳ���
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor
    {
        public float deltaTime;  // ����ÿһ֡��ת�Ƕȵ�����
        public float scale;           // ���ε����ű���

        [ReadOnly]
        public NativeArray<FractalPart> parents;  // ��ǰ�㼶�ĸ��ڵ�

        public NativeArray<FractalPart> parts;    // ��ǰ�㼶�������ӽڵ�

        [WriteOnly]
        public NativeArray<float3x4> matrices;    // ���ڴ���ÿ�����β��ֵı任����

        // ִ�в����·��νڵ����ת��λ��
        public void Execute(int i)
        {
            FractalPart parent = parents[i / 5]; // ÿ5���ӽڵ㹲��һ�����ڵ�
            FractalPart part = parts[i];         // ��ȡ��ǰҪ������ӽڵ�
            part.spinAngle += part.spinVelocity * deltaTime;    // �����ӽڵ����ת�Ƕ�

            // �����Z��20�����ת
            quaternion zRotation = quaternion.RotateZ(radians(20f));  // ������Z��20�����ת

            // �����ӽڵ�����ڸ��ڵ���Ϸ���
            float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up());
            // �����ӽڵ���´��������Ϸ���Ĳ����
            float3 sagAxis = cross(up(), upAxis);
            // �����´���ĳ��ȣ�����ȷ���´��̶�
            float sagMagnitude = length(sagAxis);
            quaternion baseRotation; // �洢���յĻ�����ת

            // ����´���ĳ��ȴ����㣬��ʾ����Ч���´�����
            if (sagMagnitude > 0f)
            {
                sagAxis /= sagMagnitude; // ��׼���´���
                                         // ����һ��Χ���´������ת��Ԫ������ת�Ƕ�Ϊ����´��Ƕȳ����´��̶�
                quaternion sagRotation = quaternion.AxisAngle(sagAxis, part.maxSagAngle * sagMagnitude);
                // ���´���ת�븸�ڵ����ת��ˣ��õ�������ת
                baseRotation = mul(sagRotation, parent.worldRotation);
            }
            else
            {
                // ���û����Ч���´��ᣬ�������ת��Ϊ���ڵ����ת
                baseRotation = parent.worldRotation;
            }

            // ����Z���20����ת���ӵ����ڵ����ת��
            baseRotation = mul(zRotation, baseRotation);

            // �����ӽڵ������ռ���ת�����ڵ����ת*�ӽڵ�ı�����ת
            part.worldRotation = mul(baseRotation,
                mul(part.rotation, quaternion.RotateY(part.spinAngle))
            );
            part.worldPosition =
                parent.worldPosition +
                //mul(parent.worldRotation, (1.5f * scale * part.direction));
                mul(part.worldRotation, float3(0f, 1.5f * scale, 0f));

            parts[i] = part;  // �����ӽڵ�

            // ����3x3����ת���󲢳������ţ���󹹽�3x4����洢λ����Ϣ
            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    }

    [SerializeField]
    Gradient gradientA, gradientB; // ����ɫ����

    [SerializeField]
    Color leafColorA, leafColorB; // Ҷ����ɫ����

    [SerializeField, Range(0f, 90f)]
    float maxSagAngleA = 15f, maxSagAngleB = 25f; // ����´��Ƕȷ�Χ

    [SerializeField, Range(0f, 90f)]
    float spinSpeedA = 20f, spinSpeedB = 25f; // �����ٶȷ�Χ

    [SerializeField, Range(0f, 1f)]
    float reverseSpinChance = 0.25f; // ���������ĸ���


    [SerializeField, Range(3, 8)]
    int depth = 4;  // ���Ʒ��ε���ȣ������εĲ���

    [SerializeField]
    Mesh mesh, leafMesh;  // ����ÿ������ʹ�õ�����

    [SerializeField]
    Material material;  // ��Ⱦ���εĲ���

    struct FractalPart
    {
        public float3 worldPosition;    //����ռ�λ��
        public quaternion rotation, worldRotation; // ������ת��������ת
        public float maxSagAngle, spinAngle, spinVelocity;  // �����Ƕȣ����ڶ���Ч��
    }


    // ���岻ͬ�������ת�Ƕȣ����ڵ�λ��Ԫ����
    static quaternion[] rotations = {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };

    NativeArray<FractalPart>[] parts;  // ����������в��ֵ����飬ÿһ��һ������

    NativeArray<float3x4>[] matrices;  // ����任��������飬ÿһ��һ������
    ComputeBuffer[] matricesBuffers;   // ���ڴ��ݾ���� GPU ������Ⱦ�Ļ�����
    static readonly int
        colorAId = Shader.PropertyToID("_ColorA"),
        colorBId = Shader.PropertyToID("_ColorB"),
         matricesId = Shader.PropertyToID("_Matrices"),// ��ȡ��ɫ�����Ե� ID
        sequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");  

    static MaterialPropertyBlock propertyBlock;  // ���ڴ���ÿ��ʵ�������������
    Vector4[] sequenceNumbers;
    // ��ʼ���������ݣ��ڶ�������ʱ����
    void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];  // ��ʼ��ÿһ��ķ��νڵ�
        matrices = new NativeArray<float3x4>[depth];  // ��ʼ��ÿһ��ı任����
        matricesBuffers = new ComputeBuffer[depth];   // ��ʼ��������
        sequenceNumbers = new Vector4[depth];
        int stride = 12 * 4;  // ÿ��������ڴ��С

        // ��ʼ�����θ���Ĳ��ֺ;���
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            // ���ε�ÿһ�㶼�� 5 �ı��������֣�length ��ʾ��ǰ��Ĳ�������
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent); // ������ǰ��ķ��β������飬ʹ�ó־û�������
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent); // ������ǰ��ľ������飬ʹ�ó־û�������
            matricesBuffers[i] = new ComputeBuffer(length, stride); // �������㻺���������ڴ��ݾ������ݵ� GPU������Ϊ��ǰ�㲿��������stride ��ÿ��������ֽڴ�С
                                                                    // �������һ�� Vector4 ����ÿ������кţ������ĸ����ֵ
            sequenceNumbers[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
        }

        // ��ʼ����һ��ĸ��ڵ�
        parts[0][0] = CreatePart(0);

        // Ϊʣ��㼶��ÿ�����β��ִ����ӽڵ�
        for (int li = 1; li < parts.Length; li++)
        {
            NativeArray<FractalPart> levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                // ÿ 5 ���ӽڵ��Ӧһ�����ڵ�
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }

        // ��ʼ���������Կ�
        propertyBlock ??= new MaterialPropertyBlock();
    }

    // �ͷ����з�����ڴ���Դ
    void OnDisable()
    {
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();  // �ͷ� GPU ������
            parts[i].Dispose();            // �ͷ� NativeArray ��Դ
            matrices[i].Dispose();         // �ͷž�������
        }
        parts = null;
        matrices = null;
        matricesBuffers = null;
        sequenceNumbers = null;
    }

    // ��ĳЩ�����仯ʱ�����������ȣ�����Ҫ���³�ʼ��
    void OnValidate()
    {
        if (parts != null && enabled)
        {
            OnDisable();  // �Ƚ�������������
            OnEnable();
        }
    }

    // ÿһ֡��Ҫ���·��ζ���״̬
    void Update()
    {
        float deltaTime = Time.deltaTime;  // ������ת�ٶ�
        FractalPart rootPart = parts[0][0];  // ��ȡ���ڵ�
        rootPart.spinAngle += rootPart.spinVelocity * deltaTime;  // ���¸��ڵ����ת�Ƕ�
        rootPart.worldRotation = mul(transform.rotation,
            mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle))
        );
        rootPart.worldPosition = transform.position;  // ���ڵ��λ��������ĵ�ǰλ��
        parts[0][0] = rootPart;  // ���¸��ڵ�

        float objectScale = transform.lossyScale.x;  // ��ȡ���������
        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;  // �������ź���ת����
        matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);  // ���ø��ڵ�ľ���

        float scale = objectScale;  // ��ʼ�����ű���
        JobHandle jobHandle = default;  // ���� JobHandle ���ڴ���������
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;  // ÿһ������ű�������һ���һ��
            // ���Ȳ������񣬴���ÿһ����νڵ�ĸ���
            jobHandle = new UpdateFractalLevelJob
            {
                deltaTime = deltaTime,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.ScheduleParallel(parts[li].Length, 5, jobHandle);
        }
        jobHandle.Complete();  // ȷ���������

        // ������Ⱦ�İ�Χ�У���ֹ�޳�
        var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        int leafIndex = matricesBuffers.Length - 1;
        // ����ÿһ��ľ������ݣ���ʹ�� GPU ��Ⱦ����
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);  // ���������ݴ��ݸ� GPU
            buffer.SetData(matrices[i]);

            Color colorA, colorB;
            Mesh instanceMesh;
            if (i == leafIndex)
            {
                colorA = leafColorA;
                colorB = leafColorB;
                instanceMesh = leafMesh;
            }
            else
            {
                float gradientInterpolator = i / (matricesBuffers.Length - 2f);
                colorA = gradientA.Evaluate(gradientInterpolator);
                colorB = gradientB.Evaluate(gradientInterpolator);
                instanceMesh = mesh;
            }
            propertyBlock.SetColor(colorAId, colorA);
            propertyBlock.SetColor(colorBId, colorB);

            propertyBlock.SetBuffer(matricesId, buffer);  // ���ò�������
            propertyBlock.SetVector(sequenceNumbersId, sequenceNumbers[i]);
            Graphics.DrawMeshInstancedProcedural(
                instanceMesh, 0, material, bounds, buffer.count, propertyBlock  // ʵ��������
            );
        }
    }

    // �������ε�һ�����֣������䷽�����ת��Ϣ
    FractalPart CreatePart(int childIndex) => new FractalPart
    {
        maxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
        spinVelocity =
            (Random.value < reverseSpinChance ? -1f : 1f) *
            radians(Random.Range(spinSpeedA, spinSpeedB)),
        rotation = rotations[childIndex]     // ��ȡ���ӽڵ�ı�����ת
    };

    /*ʹ��URP��Ⱦ����
[SerializeField, Range(1, 8)]
int depth = 4;  // ������ȣ������û���1��8֮�����á�
[SerializeField]
Mesh mesh;  // Ҫ��Ⱦ������

[SerializeField]
Material material;  // ʹ�õĲ���

// �ṹ�壬��������е�һ�����֣���������λ�á���ת����Ϣ
struct FractalPart
{
    public Vector3 direction, worldPosition;  // �ֱ��Ƿ������������λ��
    public Quaternion rotation, worldRotation;  // �ֲ����������ת
    public float spinAngle;  // ��ת�Ƕ�
}

// Ԥ����ķ������飬��ʾÿ���ӽڵ�ķ���
static Vector3[] directions = {
    Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
};

// Ԥ�������ת���飬��ʾÿ���ӽڵ�ĳ�ʼ��ת
static Quaternion[] rotations = {
    Quaternion.identity,
    Quaternion.Euler(0f, 0f, -90f), 
    Quaternion.Euler(0f, 0f, 90f),
    Quaternion.Euler(90f, 0f, 0f), 
    Quaternion.Euler(-90f, 0f, 0f)
};

// ���ڴ洢ÿ����β��ֵ�����
FractalPart[][] parts;

// ���ڴ洢����Ķ�ά���飬ÿһ����ε�ÿ�����ֶ���һ����Ӧ�ı任����
Matrix4x4[][] matrices;
// ComputeBuffer ������ GPU ���ݾ�������
ComputeBuffer[] matricesBuffers;
// ���ڽ��������ݴ��ݸ���ɫ��������ID
static readonly int matricesId = Shader.PropertyToID("_Matrices");
// ��������Ⱦʱ�ṩ�������ԵĿ�
static MaterialPropertyBlock propertyBlock;

void OnEnable()
{
    // ��ʼ�����β��ֺ;������飬���󻺳���
    parts = new FractalPart[depth][];
    matrices = new Matrix4x4[depth][];
    matricesBuffers = new ComputeBuffer[depth];
    int stride = 16 * 4;  // ÿ������Ĵ�СΪ16��float��ÿ��float 4�ֽ�
    for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
    {
        parts[i] = new FractalPart[length];
        matrices[i] = new Matrix4x4[length];
        matricesBuffers[i] = new ComputeBuffer(length, stride);  // ��ʼ��������
    }

    // ��ʼ�����ڵ�ķ��β���
    parts[0][0] = CreatePart(0);    
    for (int li = 1; li < parts.Length; li++)
    {
        // scale *= 0.5f;  // �����ڵ�����С�����ű�����ע�͵���
        FractalPart[] levelParts = parts[li];
        for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
        {
            for (int ci = 0; ci < 5; ci++)
            {
                levelParts[fpi + ci] = CreatePart(ci);  // ��ʼ���ӽڵ�
            }
        }
    }

    propertyBlock ??= new MaterialPropertyBlock();  // ȷ���������Կ��ѳ�ʼ��
}

void OnDisable()
{
    // �ͷž��󻺳����������ڴ�
    for (int i = 0; i < matricesBuffers.Length; i++)
    {
        matricesBuffers[i].Release();
    }
    parts = null;
    matrices = null;
    matricesBuffers = null;
}

// �����ڱ༭���и��Ĳ���ʱ���³�ʼ������
void OnValidate()
{
    if (parts != null && enabled)
    {
        OnDisable();
        OnEnable();
    }
}

void Update()
{
    // ÿ֡���ӵ���ת�Ƕ�
    float spinAngleDelta = 22.5f * Time.deltaTime;
    FractalPart rootPart = parts[0][0];
    rootPart.spinAngle += spinAngleDelta;  // ������ת�Ƕ�
    // ���¸��ڵ��������ת
    rootPart.worldRotation =
         transform.rotation *
         (rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f));
    rootPart.worldPosition = transform.position;  // ���¸��ڵ������λ��
    parts[0][0] = rootPart;

    float objectScale = transform.lossyScale.x;  // ��ȡ��������ű���
    matrices[0][0] = Matrix4x4.TRS(
        rootPart.worldPosition, rootPart.worldRotation, objectScale * Vector3.one
    );

    // �ݹ����ÿһ��ķ���
    float scale = objectScale;
    for (int li = 1; li < parts.Length; li++)
    {
        scale *= 0.5f;  // ÿ����Сһ��
        FractalPart[] parentParts = parts[li - 1];
        FractalPart[] levelParts = parts[li];
        Matrix4x4[] levelMatrices = matrices[li];
        for (int fpi = 0; fpi < levelParts.Length; fpi++)
        {
            FractalPart parent = parentParts[fpi / 5];  // ��ȡ���ڵ�
            FractalPart part = levelParts[fpi];
            part.spinAngle += spinAngleDelta;  // �����ӽڵ���ת�Ƕ�
            part.worldRotation =
                parent.worldRotation *
                (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
            part.worldPosition =
                parent.worldPosition +
                parent.worldRotation * (1.5f * scale * part.direction);
            levelParts[fpi] = part;
            // ���¾���
            levelMatrices[fpi] = Matrix4x4.TRS(
                part.worldPosition, part.worldRotation, scale * Vector3.one
            );
        }
    }

    // �����Χ�У���������Ļ�ϻ��Ʒ���
    var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
    for (int i = 0; i < matricesBuffers.Length; i++)
    {
        ComputeBuffer buffer = matricesBuffers[i];
        buffer.SetData(matrices[i]);  // �����ݴ��ݵ� GPU
        propertyBlock.SetBuffer(matricesId, buffer);
        Graphics.DrawMeshInstancedProcedural(
            mesh, 0, material, bounds, buffer.count, propertyBlock
        );
    }
}

// �������ε�һ�����֣�ָ���ӽڵ�����
FractalPart CreatePart(int childIndex) => new FractalPart
{
    direction = directions[childIndex],  // ��ȡ���ӽڵ�ķ���
    rotation = rotations[childIndex]     // ��ȡ���ӽڵ����ת
};

    */

    /*�Ż����ǲ��ܴﵽ8�����
    [SerializeField, Range(1, 8)]
    int depth = 4;
    [SerializeField]
    Mesh mesh;

    [SerializeField]
    Material material;

    struct FractalPart
    {
        public Vector3 direction;
        public Quaternion rotation;
        public Transform transform;
    }


    static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };

    FractalPart[][] parts;

    void Awake()
    {
        parts = new FractalPart[depth][];
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new FractalPart[length];
        }
        float scale = 1f;
        parts[0][0] = CreatePart(0, 0, scale);
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            FractalPart[] levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(li, ci, scale);
                }
            }
        }
    }

    void Update()
    {
        Quaternion deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);
        FractalPart rootPart = parts[0][0];
        rootPart.rotation *= deltaRotation;
        rootPart.transform.localRotation = rootPart.rotation;
        parts[0][0] = rootPart;
        for (int li = 1; li < parts.Length; li++)
        {
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi++)
            {
                Transform parentTransform = parentParts[fpi / 5].transform;
                FractalPart part = levelParts[fpi];
                part.rotation *= deltaRotation;
                part.transform.localRotation = part.rotation;
                part.transform.localPosition =
                     parentTransform.localPosition +
                     parentTransform.localRotation *
                         (1.5f * part.transform.localScale.x * part.direction);
                levelParts[fpi] = part;
            }
        }
    }

    FractalPart CreatePart(int levelIndex, int childIndex, float scale)
        {
            var go = new GameObject("Fractal Part L" + levelIndex + " C" + childIndex);
            go.transform.localScale = scale * Vector3.one;
            go.transform.SetParent(transform, false);
             go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>().material = material;
        return new FractalPart()
        {
            direction = directions[childIndex],
            rotation = rotations[childIndex],
            transform = go.transform
        };
    }
    */

    /*ͨ�÷�����ȵ���8ʱ��֡��ֻ��3֡
    void Start()
    {
        name = "Fractal " + depth;
        if (depth <= 1)
        {
            return;
        }

        Fractal childA = CreateChild(Vector3.up, Quaternion.identity);
        Fractal childB = CreateChild(Vector3.right, Quaternion.Euler(0f, 0f, -90f));
        Fractal childC = CreateChild(Vector3.left, Quaternion.Euler(0f, 0f, 90f));
        Fractal childD = CreateChild(Vector3.forward, Quaternion.Euler(90f, 0f, 0f));
        Fractal childE = CreateChild(Vector3.back, Quaternion.Euler(-90f, 0f, 0f));

        childA.transform.SetParent(transform, false);
        childB.transform.SetParent(transform, false);
        childC.transform.SetParent(transform, false);
        childD.transform.SetParent(transform, false);
        childE.transform.SetParent(transform, false);
    }

    Fractal CreateChild(Vector3 direction, Quaternion rotation)
    {
        Fractal child = Instantiate(this);
        child.depth = depth - 1;
        child.transform.localPosition = 0.75f * direction;
        child.transform.localRotation = rotation;
        child.transform.localScale = 0.5f * Vector3.one;
        return child;
    }
    void Update()
    {
        transform.Rotate(0f, 22.5f * Time.deltaTime, 0f);
    }
    */
}
