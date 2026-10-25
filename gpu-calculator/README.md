---
AIGC:
    Label: "1"
    ContentProducer: 001191440300708461136T1XGW3
    ProduceID: 723ed3197ce4c60a0f4dc5c508935f55_0f0f5774d04311f1a443525400393706
    ReservedCode1: acSUajazErsSKDl6KA5Iirgov4isVBfayu259VwR4+qDXgP8WtsaNC1Wn4u9bvb5ckFCyy9BxQ6TREWUGG3JK1WiVl6qRVtj4BgjuMVAWa9ujlKTdiDTshO8exK10mwHFrfGirbVev+7lyVihf5NX7qP+TqB1U7zFIhYEx8DKS7WzUfHxPt1wiuSHi0=
    ContentPropagator: 001191440300708461136T1XGW3
    PropagateID: 723ed3197ce4c60a0f4dc5c508935f55_0f0f5774d04311f1a443525400393706
    ReservedCode2: acSUajazErsSKDl6KA5Iirgov4isVBfayu259VwR4+qDXgP8WtsaNC1Wn4u9bvb5ckFCyy9BxQ6TREWUGG3JK1WiVl6qRVtj4BgjuMVAWa9ujlKTdiDTshO8exK10mwHFrfGirbVev+7lyVihf5NX7qP+TqB1U7zFIhYEx8DKS7WzUfHxPt1wiuSHi0=
---

# gpu-calculator — GPU 科学计算器

纯传统科学计算器：**CUDA C++ kernel 编译为 DLL** + **C# WinForms (.NET 8) P/Invoke** 调用。
每一次运算都启动一次 CUDA kernel 在 GPU 上执行，界面同时展示 **GPU 与 CPU 完成同一运算的耗时对比**。

## 功能

- 四则运算：`+` `−` `×` `÷`
- 科学函数：`sin` `cos` `tan` `sqrt` `log` `exp` `pow` `±`
- 每次运算 = 一次 GPU kernel 调用（1x1 网格在 RTX GPU 上执行）
- 界面显示：当前输入 / 表达式结果 / 运算历史 / **GPU vs CPU 耗时与加速比**

## 目录结构

```
gpu-calculator/
├── cuda/
│   ├── gpu_calculator.cu      # CUDA kernel + 导出 C 接口（二元/一元运算，GPU 计时）
│   └── build_cuda.bat         # CUDA DLL 编译脚本（自动初始化 vcvars64 + nvcc）
├── src/
│   ├── GpuCalculator.csproj   # .NET 8 WinForms 项目（自动拷贝 native DLL）
│   ├── Program.cs
│   ├── MainForm.cs            # 计算器界面 + GPU/CPU 耗时对比面板
│   └── GpuCalculatorNative.cs # P/Invoke 封装层
└── README.md
```

## 环境要求

| 组件 | 版本 / 路径 |
|---|---|
| CUDA Toolkit | `D:\CUDA\v12.9`（nvcc 12.9.41，支持 sm_120） |
| MSVC | `D:\Program Files\Microsoft\VisualStudio`（vcvars64.bat） |
| .NET SDK | 8.0 或更高（本机 9.0 可编译 net8.0-windows） |
| GPU | NVIDIA RTX 5070 Ti（Compute Capability 12.0） |

## 构建步骤

### 1. 编译 CUDA DLL

```bat
cd gpu-calculator\cuda
build_cuda.bat
```

脚本会依次：初始化 MSVC 环境（`call vcvars64.bat`）→ 调用 `nvcc -arch=sm_120 -shared` 编译
`gpu_calculator.cu` → 输出 `gpu-calculator\native\gpu_calculator_cuda.dll`。

> 其他架构 GPU 请把 `build_cuda.bat` 中的 `-arch=sm_120` 改为对应算力（如 `sm_75` / `sm_100`）。

### 2. 构建并运行 C# 界面

```bat
cd gpu-calculator\src
dotnet build -c Debug
dotnet run --project GpuCalculator.csproj
```

`GpuCalculator.csproj` 检测到 `..\native\gpu_calculator_cuda.dll` 存在时会自动将其拷贝到输出目录；
运行前请确保已先执行步骤 1，且系统 PATH 含 CUDA 运行时（`D:\CUDA\v12.9\bin`，含 `cudart64_12.dll`）。

## 技术要点

- **P/Invoke 封装**：`GpuCalculatorNative.cs` 通过 `[DllImport("gpu_calculator_cuda.dll", CallingConvention = Cdecl)]`
  暴露 `CalcInit / CalcBinary / CalcUnary / CpuBinary / CpuUnary / CalcShutdown`。
- **GPU 计时**：kernel 前后用 `cudaEventRecord` + `cudaEventElapsedTime` 精确测量单次 kernel 耗时。
- **CPU 对比**：DLL 内使用与 kernel 完全相同的计算逻辑在宿主端执行，`std::chrono::steady_clock` 计时。
- **一次运算一次 kernel**：`kernel_binary<<<1,1>>>` / `kernel_unary<<<1,1>>>`，结果经 `cudaMemcpy` 回传 C#。
*（内容由AI生成，仅供参考）*
