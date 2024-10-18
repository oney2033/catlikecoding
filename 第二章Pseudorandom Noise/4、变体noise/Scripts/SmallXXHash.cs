using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

// ������һ��ֻ���ṹ�� SmallXXHash
// ʹ�� readonly ��Ϊ�˱�֤�ṹ�岻�ɱ�
public readonly struct SmallXXHash
{
    // �������壬���ڹ�ϣ����ļ������� (primeA, primeB, primeC, primeD, primeE)
    // ��Щ�����ڹ�ϣ���������ڻ�Ϻ��������ݣ�ʹ�ù�ϣ������Ӿ���
    const uint primeA = 0b10011110001101110111100110110001;
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    // ֻ���ֶ� accumulator���洢��ǰ��ϣֵ���ۻ����
    readonly uint accumulator;

    // ���캯��������һ����ʼ�Ĺ�ϣֵ����ֵ�� accumulator
    public SmallXXHash(uint accumulator)
    {
        this.accumulator = accumulator;
    }

    // �����˴� SmallXXHash ��ʽת��Ϊ uint �Ĳ�����
    // ��������������Զ��� SmallXXHash ���͵Ķ���ת��Ϊ uint ����
    // ��ת�������У���ϣֵ��ͨ��һϵ��λ�����������˷�����"ѩ��ЧӦ"���������ӹ�ϣ����ľ�����
    public static implicit operator uint(SmallXXHash hash)
    {
        uint avalanche = hash.accumulator; // ��ȡ��ǰ���ۻ���ϣֵ
        avalanche ^= avalanche >> 15;      // ���� 15 λ�����������
        avalanche *= primeB;               // ����һ������ primeB
        avalanche ^= avalanche >> 13;      // �ٴ����� 13 λ�����
        avalanche *= primeC;               // ������һ������ primeC
        avalanche ^= avalanche >> 16;      // ������� 16 λ�����
        return avalanche;                  // ���ش����Ĺ�ϣֵ
    }

    // �����˴� uint ��ʽת��Ϊ SmallXXHash �Ĳ�����
    // ����ͨ��ֱ�Ӹ�ֵ uint ֵ������ SmallXXHash ����
    public static implicit operator SmallXXHash(uint accumulator) =>
        new SmallXXHash(accumulator); // �����µ� SmallXXHash ����ֵΪ accumulator

    // ��������������һ���µ� SmallXXHash�����ڸ���������ֵ
    // ������ֵ�� primeE ��������ɳ�ʼ�� accumulator
    public static SmallXXHash Seed(int seed) => (uint)seed + primeE;

    // ��̬��������������ת����
    // �� data ���� steps λ����������Ĳ������Ƶ��Ҳ�
    static uint RotateLeft(uint data, int steps) =>
        (data << steps) | (data >> 32 - steps);

    // Eat ����������һ��������������ϵ���ǰ�Ĺ�ϣֵ��
    // ��ͨ������ primeC �� primeD �������ң�����ʹ������������
    public SmallXXHash Eat(int data) =>
        RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;

    // ���ص� Eat ����������һ���ֽ����ݣ�������ϵ���ǰ�Ĺ�ϣֵ��
    // ���� primeE �� primeA �����������Һ͹�ϣ�ۻ�
    public SmallXXHash Eat(byte data) =>
        RotateLeft(accumulator + data * primeE, 11) * primeA;

    public static implicit operator SmallXXHash4(SmallXXHash hash) =>
        new SmallXXHash4(hash.accumulator);
}

public readonly struct SmallXXHash4
{
    // �������壬���ڹ�ϣ����ļ������� (primeA, primeB, primeC, primeD, primeE)
    // ��Щ�����ڹ�ϣ���������ڻ�Ϻ��������ݣ�ʹ�ù�ϣ������Ӿ���
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    // ֻ���ֶ� accumulator���洢��ǰ��ϣֵ���ۻ����
    readonly uint4 accumulator;

    // ���캯��������һ����ʼ�Ĺ�ϣֵ����ֵ�� accumulator
    public SmallXXHash4(uint4 accumulator)
    {
        this.accumulator = accumulator;
    }
    public uint4 BytesA => (uint4)this & 255; // ��ȡ uint4 ������ֽڣ�0-7λ��
    public uint4 BytesB => ((uint4)this >> 8) & 255; // ��ȡ uint4 �ĵڶ����ֽڣ�8-15λ��
    public uint4 BytesC => ((uint4)this >> 16) & 255; // ��ȡ uint4 �ĵ������ֽڣ�16-23λ��
    public uint4 BytesD => (uint4)this >> 24; // ��ȡ uint4 ������ֽڣ�24-31λ��

    // �� BytesA ��ֵ�� [0, 255] ӳ�䵽 [0, 1] �ĸ�����
    public float4 Floats01A => (float4)BytesA * (1f / 255f);
    // �� BytesB ��ֵ�� [0, 255] ӳ�䵽 [0, 1] �ĸ�����
    public float4 Floats01B => (float4)BytesB * (1f / 255f);
    // �� BytesC ��ֵ�� [0, 255] ӳ�䵽 [0, 1] �ĸ�����
    public float4 Floats01C => (float4)BytesC * (1f / 255f);
    // �� BytesD ��ֵ�� [0, 255] ӳ�䵽 [0, 1] �ĸ�����
    public float4 Floats01D => (float4)BytesD * (1f / 255f);


    // �����˴� uint ��ʽת��Ϊ SmallXXHash �Ĳ�����
    // ����ͨ��ֱ�Ӹ�ֵ uint ֵ������ SmallXXHash ����
    public static implicit operator SmallXXHash4(uint4 accumulator) =>
        new SmallXXHash4(accumulator); // �����µ� SmallXXHash ����ֵΪ accumulator

    // ��������������һ���µ� SmallXXHash�����ڸ���������ֵ
    // ������ֵ�� primeE ��������ɳ�ʼ�� accumulator
    public static SmallXXHash4 Seed(int4 seed) => (uint4)seed + primeE;

    // ��̬��������������ת����
    // �� data ���� steps λ����������Ĳ������Ƶ��Ҳ�
    static uint4 RotateLeft(uint4 data, int steps) =>
        (data << steps) | (data >> 32 - steps);

    // Eat ����������һ��������������ϵ���ǰ�Ĺ�ϣֵ��
    // ��ͨ������ primeC �� primeD �������ң�����ʹ������������
    public SmallXXHash4 Eat(int4 data) =>
        RotateLeft(accumulator + (uint4)data * primeC, 17) * primeD;

    // �����˴� SmallXXHash ��ʽת��Ϊ uint �Ĳ�����
    // ��������������Զ��� SmallXXHash ���͵Ķ���ת��Ϊ uint ����
    // ��ת�������У���ϣֵ��ͨ��һϵ��λ�����������˷�����"ѩ��ЧӦ"���������ӹ�ϣ����ľ�����
    public static implicit operator uint4(SmallXXHash4 hash)
    {
        uint4 avalanche = hash.accumulator; // ��ȡ��ǰ���ۻ���ϣֵ
        avalanche ^= avalanche >> 15;      // ���� 15 λ�����������
        avalanche *= primeB;               // ����һ������ primeB
        avalanche ^= avalanche >> 13;      // �ٴ����� 13 λ�����
        avalanche *= primeC;               // ������һ������ primeC
        avalanche ^= avalanche >> 16;      // ������� 16 λ�����
        return avalanche;                  // ���ش����Ĺ�ϣֵ
    }

    public static SmallXXHash4 operator +(SmallXXHash4 h, int v) =>
        h.accumulator + (uint)v;
}
