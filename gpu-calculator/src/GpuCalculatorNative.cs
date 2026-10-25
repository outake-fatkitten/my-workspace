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
    public const long OpAdd = 0;
    public const long OpSub = 1;
    public const long OpMul = 2;
    public const long OpDiv = 3;
    public const long OpPow = 4;

    // 一元运算 op 码
    public const long OpSin = 0;
    public const long OpCos = 1;
    public const long OpTan = 2;
    public const long OpSqrt = 3;
    public const long OpLog = 4;
    public const long OpExp = 5;
    public const long OpAbs = 6;

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CalcInit();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CalcShutdown();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CalcBinary(long op, double a, double b, out double result, out double gpuMs);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CalcUnary(long op, double a, out double result, out double gpuMs);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern double CpuBinary(long op, double a, double b, out double cpuMs);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern double CpuUnary(long op, double a, out double cpuMs);
}
