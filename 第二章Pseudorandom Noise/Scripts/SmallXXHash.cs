using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 定义了一个只读结构体 SmallXXHash
// 使用 readonly 是为了保证结构体不可变
public readonly struct SmallXXHash
{
    // 常量定义，用于哈希运算的几个质数 (primeA, primeB, primeC, primeD, primeE)
    // 这些质数在哈希计算中用于混合和扰乱数据，使得哈希结果更加均匀
    const uint primeA = 0b10011110001101110111100110110001;
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    // 只读字段 accumulator，存储当前哈希值的累积结果
    readonly uint accumulator;

    // 构造函数，接收一个初始的哈希值并赋值给 accumulator
    public SmallXXHash(uint accumulator)
    {
        this.accumulator = accumulator;
    }

    // 定义了从 SmallXXHash 隐式转换为 uint 的操作符
    // 这个操作符可以自动将 SmallXXHash 类型的对象转换为 uint 类型
    // 在转换过程中，哈希值会通过一系列位操作和质数乘法进行"雪崩效应"处理，以增加哈希结果的均匀性
    public static implicit operator uint(SmallXXHash hash)
    {
        uint avalanche = hash.accumulator; // 获取当前的累积哈希值
        avalanche ^= avalanche >> 15;      // 右移 15 位并与自身异或
        avalanche *= primeB;               // 乘以一个质数 primeB
        avalanche ^= avalanche >> 13;      // 再次右移 13 位并异或
        avalanche *= primeC;               // 乘以另一个质数 primeC
        avalanche ^= avalanche >> 16;      // 最后右移 16 位并异或
        return avalanche;                  // 返回处理后的哈希值
    }

    // 定义了从 uint 隐式转换为 SmallXXHash 的操作符
    // 允许通过直接赋值 uint 值来生成 SmallXXHash 对象
    public static implicit operator SmallXXHash(uint accumulator) =>
        new SmallXXHash(accumulator); // 返回新的 SmallXXHash 对象，值为 accumulator

    // 工厂方法，生成一个新的 SmallXXHash，基于给定的种子值
    // 将种子值与 primeE 相加来生成初始的 accumulator
    public static SmallXXHash Seed(int seed) => (uint)seed + primeE;

    // 静态方法，用于左旋转操作
    // 将 data 左移 steps 位，并将溢出的部分右移到右侧
    static uint RotateLeft(uint data, int steps) =>
        (data << steps) | (data >> 32 - steps);

    // Eat 方法，接收一个整数，将它混合到当前的哈希值中
    // 它通过质数 primeC 和 primeD 进行扰乱，并且使用了左旋操作
    public SmallXXHash Eat(int data) =>
        RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;

    // 重载的 Eat 方法，接收一个字节数据，将它混合到当前的哈希值中
    // 质数 primeE 和 primeA 用于数据扰乱和哈希累积
    public SmallXXHash Eat(byte data) =>
        RotateLeft(accumulator + data * primeE, 11) * primeA;
}
