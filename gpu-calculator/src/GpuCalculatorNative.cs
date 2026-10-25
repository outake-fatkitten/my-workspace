using System;
using System.Runtime.InteropServices;

namespace GpuCalculator;

/// <summary>
/// P/Invoke 封装层：桥接 CUDA C++ DLL（gpu_calculator_cuda.dll）。
/// op 码与 cuda/gpu_calculator.cu 中的常量一一对应。
/// </summary>
internal static class GpuCalculatorNative
{
    private const string Dll = "gpu_calculator_cuda.dll";

    // 二元运算 op 码
    public const int OpAdd = 0;
    public const int OpSub = 1;
    public const int OpMul = 2;
    public const int OpDiv = 3;
    public const int OpPow = 4;

    // 一元运算 op 码
    public const int OpSin = 0;
    public const int OpCos = 1;
    public const int OpTan = 2;
    public const int OpSqrt = 3;
    public const int OpLog = 4;
    public const int OpExp = 5;
    public const int OpAbs = 6;

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CalcInit();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CalcShutdown();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CalcBinary(int op, double a, double b, out double result, out double gpuMs);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CalcUnary(int op, double a, out double result, out double gpuMs);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern double CpuBinary(int op, double a, double b, out double cpuMs);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern double CpuUnary(int op, double a, out double cpuMs);
}
