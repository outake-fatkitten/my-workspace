// gpu_calculator.cu
// GPU scientific calculator core:
//   one calculation = one CUDA kernel launch, exports C ABI for C# P/Invoke.
#include <cuda_runtime.h>
#include <cmath>
#include <chrono>

#define CALC_OK          0
#define CALC_ERR_CUDA    1
#define CALC_ERR_INVALID 2

// Binary op codes (keep in sync with C# GpuCalculatorNative)
#define BIN_OP_ADD 0
#define BIN_OP_SUB 1
#define BIN_OP_MUL 2
#define BIN_OP_DIV 3
#define BIN_OP_POW 4

// Unary op codes
#define UN_OP_SIN  0
#define UN_OP_COS  1
#define UN_OP_TAN  2
#define UN_OP_SQRT 3
#define UN_OP_LOG  4
#define UN_OP_EXP  5
#define UN_OP_ABS  6

// Shared by kernel (device) and CPU fallback (host)
__host__ __device__ double calc_binary(int op, double a, double b) {
    switch (op) {
        case BIN_OP_ADD: return a + b;
        case BIN_OP_SUB: return a - b;
        case BIN_OP_MUL: return a * b;
        case BIN_OP_DIV: return (b == 0.0) ? NAN : a / b;
        case BIN_OP_POW: return pow(a, b);
        default:         return NAN;
    }
}

__host__ __device__ double calc_unary(int op, double a) {
    switch (op) {
        case UN_OP_SIN:  return sin(a);
        case UN_OP_COS:  return cos(a);
        case UN_OP_TAN:  return tan(a);
        case UN_OP_SQRT: return (a < 0.0) ? NAN : sqrt(a);
        case UN_OP_LOG:  return (a <= 0.0) ? NAN : log(a);
        case UN_OP_EXP:  return exp(a);
        case UN_OP_ABS:  return fabs(a);
        default:         return NAN;
    }
}

__global__ void kernel_binary(int op, double a, double b, double* out) {
    *out = calc_binary(op, a, b);
}

__global__ void kernel_unary(int op, double a, double* out) {
    *out = calc_unary(op, a);
}

static cudaStream_t g_stream = nullptr;
static double* g_dev_out = nullptr;

static int ensure_ready() {
    if (g_stream != nullptr && g_dev_out != nullptr)
        return CALC_OK;
    cudaError_t err = cudaSetDevice(0);
    if (err != cudaSuccess) return CALC_ERR_CUDA;
    err = cudaMalloc(&g_dev_out, sizeof(double));
    if (err != cudaSuccess) return CALC_ERR_CUDA;
    err = cudaStreamCreate(&g_stream);
    if (err != cudaSuccess) return CALC_ERR_CUDA;
    return CALC_OK;
}

extern "C" {

// Initialize CUDA context (also auto-initialized on first compute call)
__declspec(dllexport) int CalcInit() {
    return ensure_ready();
}

__declspec(dllexport) void CalcShutdown() {
    if (g_stream)  { cudaStreamDestroy(g_stream);  g_stream = nullptr; }
    if (g_dev_out) { cudaFree(g_dev_out);          g_dev_out = nullptr; }
    cudaDeviceReset();
}

// GPU binary op: a <op> b -> *out, *gpuMs = GPU kernel time in ms
__declspec(dllexport) int CalcBinary(int op, double a, double b, double* out, double* gpuMs) {
    int rc = ensure_ready();
    if (rc != CALC_OK) return rc;
    if (op < BIN_OP_ADD || op > BIN_OP_POW) return CALC_ERR_INVALID;

    cudaEvent_t start, stop;
    cudaEventCreate(&start);
    cudaEventCreate(&stop);

    cudaEventRecord(start, g_stream);
    kernel_binary<<<1, 1, 0, g_stream>>>(op, a, b, g_dev_out);
    cudaEventRecord(stop, g_stream);
    cudaError_t err = cudaEventSynchronize(stop);
    if (err != cudaSuccess) { cudaEventDestroy(start); cudaEventDestroy(stop); return CALC_ERR_CUDA; }

    float ms = 0.0f;
    cudaEventElapsedTime(&ms, start, stop);
    *gpuMs = static_cast<double>(ms);

    err = cudaMemcpy(out, g_dev_out, sizeof(double), cudaMemcpyDeviceToHost);
    cudaEventDestroy(start);
    cudaEventDestroy(stop);
    return (err == cudaSuccess) ? CALC_OK : CALC_ERR_CUDA;
}

// GPU unary op: <op>(a) -> *out, *gpuMs = GPU kernel time in ms
__declspec(dllexport) int CalcUnary(int op, double a, double* out, double* gpuMs) {
    int rc = ensure_ready();
    if (rc != CALC_OK) return rc;
    if (op < UN_OP_SIN || op > UN_OP_ABS) return CALC_ERR_INVALID;

    cudaEvent_t start, stop;
    cudaEventCreate(&start);
    cudaEventCreate(&stop);

    cudaEventRecord(start, g_stream);
    kernel_unary<<<1, 1, 0, g_stream>>>(op, a, g_dev_out);
    cudaEventRecord(stop, g_stream);
    cudaError_t err = cudaEventSynchronize(stop);
    if (err != cudaSuccess) { cudaEventDestroy(start); cudaEventDestroy(stop); return CALC_ERR_CUDA; }

    float ms = 0.0f;
    cudaEventElapsedTime(&ms, start, stop);
    *gpuMs = static_cast<double>(ms);

    err = cudaMemcpy(out, g_dev_out, sizeof(double), cudaMemcpyDeviceToHost);
    cudaEventDestroy(start);
    cudaEventDestroy(stop);
    return (err == cudaSuccess) ? CALC_OK : CALC_ERR_CUDA;
}

// CPU comparison: same math as kernel, executed on host
__declspec(dllexport) double CpuBinary(int op, double a, double b, double* cpuMs) {
    auto t0 = std::chrono::steady_clock::now();
    double r = calc_binary(op, a, b);
    auto t1 = std::chrono::steady_clock::now();
    *cpuMs = std::chrono::duration<double, std::milli>(t1 - t0).count();
    return r;
}

__declspec(dllexport) double CpuUnary(int op, double a, double* cpuMs) {
    auto t0 = std::chrono::steady_clock::now();
    double r = calc_unary(op, a);
    auto t1 = std::chrono::steady_clock::now();
    *cpuMs = std::chrono::duration<double, std::milli>(t1 - t0).count();
    return r;
}

} // extern "C"
