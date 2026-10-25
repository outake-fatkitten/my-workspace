@echo off
setlocal
rem ============================================================
rem  Build script for gpu_calculator_cuda.dll
rem  Requires: CUDA Toolkit at D:\CUDA\v12.9, MSVC vcvars64.bat
rem  Output:    ..\native\gpu_calculator_cuda.dll
rem ============================================================
set "ROOT=%~dp0.."
set "CUDA_BIN=D:\CUDA\v12.9\bin"
set "VCVARS=D:\Program Files\Microsoft\VisualStudio\VC\Auxiliary\Build\vcvars64.bat"

if not exist "%CUDA_BIN%\nvcc.exe" (
    echo [ERROR] nvcc.exe not found at %CUDA_BIN%\nvcc.exe
    exit /b 1
)
if not exist "%VCVARS%" (
    echo [ERROR] vcvars64.bat not found at %VCVARS%
    exit /b 1
)

call "%VCVARS%" >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Failed to initialize MSVC environment.
    exit /b 1
)

if not exist "%ROOT%\native" mkdir "%ROOT%\native"

echo [INFO] Building gpu_calculator_cuda.dll (sm_120) ...
"%CUDA_BIN%\nvcc.exe" -arch=sm_120 -shared -o "%ROOT%\native\gpu_calculator_cuda.dll" "%~dp0gpu_calculator.cu" -Xcompiler "/MD" -Xcompiler "/O2" -Xcompiler "/utf-8"
if errorlevel 1 (
    echo [ERROR] nvcc build failed.
    exit /b 1
)

echo [OK] Built %ROOT%\native\gpu_calculator_cuda.dll
endlocal
