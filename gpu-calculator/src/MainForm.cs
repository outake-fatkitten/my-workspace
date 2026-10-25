using System;
using System.Globalization;
using System.Windows.Forms;

namespace GpuCalculator;

/// <summary>
/// 纯传统科学计算器主窗体：
/// - 每次运算通过 P/Invoke 调用 CUDA DLL，在 GPU 上执行对应 kernel；
/// - 界面实时显示 GPU 与 CPU 完成同一运算的耗时对比。
/// </summary>
public sealed class MainForm : Form
{
    private readonly Label _display = new();
    private readonly Label _timing = new();
    private readonly TextBox _history = new();

    private string _current = "0";
    private double? _accumulator;
    private int? _pendingOp;
    private bool _freshResult;

    public MainForm()
    {
        Text = "GPU 科学计算器 (CUDA + P/Invoke)";
        Width = 420;
        Height = 620;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        Load += (_, _) => InitNative();
        FormClosed += (_, _) => GpuCalculatorNative.CalcShutdown();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _history.Dock = DockStyle.Fill;
        _history.Multiline = true;
        _history.ReadOnly = true;
        _history.ScrollBars = ScrollBars.Vertical;
        _history.Font = new System.Drawing.Font("Consolas", 9F);
        _history.Text = "运算历史（每行末为该次 GPU/CPU 耗时）";

        var top = new Panel { Dock = DockStyle.Fill, Height = 96 };
        _display.Dock = DockStyle.Fill;
        _display.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        _display.Font = new System.Drawing.Font("Consolas", 26F, System.Drawing.FontStyle.Bold);
        _display.Text = "0";

        _timing.Dock = DockStyle.Fill;
        _timing.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        _timing.Font = new System.Drawing.Font("Consolas", 10F);
        _timing.ForeColor = System.Drawing.Color.DimGray;
        _timing.Text = "GPU: -- ms | CPU: -- ms | Speedup: --x";

        top.Controls.Add(_display);
        top.Controls.Add(_timing);
        _display.BringToFront();

        root.Controls.Add(top, 0, 0);
        root.Controls.Add(_timing, 0, 1);
        root.Controls.Add(_history, 0, 2);

        var keys = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 6
        };
        for (var c = 0; c < 6; c++)
            keys.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6f));
        for (var r = 0; r < 6; r++)
            keys.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6f));

        AddKey(keys, "C", 0, 0, _ => Reset());
        AddKey(keys, "⌫", 0, 1, _ => Backspace());
        AddKey(keys, "sin", 0, 2, _ => Unary(GpuCalculatorNative.OpSin, "sin"));
        AddKey(keys, "cos", 0, 3, _ => Unary(GpuCalculatorNative.OpCos, "cos"));
        AddKey(keys, "tan", 0, 4, _ => Unary(GpuCalculatorNative.OpTan, "tan"));
        AddKey(keys, "sqrt", 0, 5, _ => Unary(GpuCalculatorNative.OpSqrt, "√"));

        AddKey(keys, "7", 1, 0, _ => Digit("7"));
        AddKey(keys, "8", 1, 1, _ => Digit("8"));
        AddKey(keys, "9", 1, 2, _ => Digit("9"));
        AddKey(keys, "÷", 1, 3, _ => Binary(GpuCalculatorNative.OpDiv, "÷"));
        AddKey(keys, "log", 1, 4, _ => Unary(GpuCalculatorNative.OpLog, "log"));
        AddKey(keys, "exp", 1, 5, _ => Unary(GpuCalculatorNative.OpExp, "exp"));

        AddKey(keys, "4", 2, 0, _ => Digit("4"));
        AddKey(keys, "5", 2, 1, _ => Digit("5"));
        AddKey(keys, "6", 2, 2, _ => Digit("6"));
        AddKey(keys, "×", 2, 3, _ => Binary(GpuCalculatorNative.OpMul, "×"));
        AddKey(keys, "±", 2, 4, _ => ToggleSign());
        AddKey(keys, "pow", 2, 5, _ => Binary(GpuCalculatorNative.OpPow, "^"));

        AddKey(keys, "1", 3, 0, _ => Digit("1"));
        AddKey(keys, "2", 3, 1, _ => Digit("2"));
        AddKey(keys, "3", 3, 2, _ => Digit("3"));
        AddKey(keys, "−", 3, 3, _ => Binary(GpuCalculatorNative.OpSub, "−"));
        AddKey(keys, "(", 3, 4, _ => { /* 保留位 */ });
        AddKey(keys, ")", 3, 5, _ => { /* 保留位 */ });

        AddKey(keys, "0", 4, 0, _ => Digit("0"));
        AddKey(keys, ".", 4, 1, _ => Dot());
        AddKey(keys, "=", 4, 2, _ => Evaluate());
        AddKey(keys, "+", 4, 3, _ => Binary(GpuCalculatorNative.OpAdd, "+"));
        AddKey(keys, "Ans", 4, 4, _ => { /* 保留位 */ });
        AddKey(keys, "CE", 4, 5, _ => { _current = "0"; RefreshDisplay(); });

        root.Controls.Add(keys, 0, 2);
        Controls.Add(root);
    }

    private void AddKey(TableLayoutPanel panel, string text, int row, int col, Action<object> onClick)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new System.Drawing.Font("Microsoft YaHei UI", 11F),
            FlatStyle = FlatStyle.Flat,
            Tag = text
        };
        btn.Click += (s, e) => onClick(s!);
        panel.Controls.Add(btn, col, row);
    }

    private void InitNative()
    {
        try
        {
            var rc = GpuCalculatorNative.CalcInit();
            _timing.Text = rc == 0 ? "GPU 就绪" : $"GPU 初始化失败 (rc={rc})";
        }
        catch (DllNotFoundException)
        {
            _timing.Text = "未找到 gpu_calculator_cuda.dll（请先运行 cuda/build_cuda.bat）";
        }
        catch (Exception ex)
        {
            _timing.Text = "GPU 初始化异常: " + ex.Message;
        }
    }

    private void RefreshDisplay() => _display.Text = _current;

    private void Reset()
    {
        _current = "0";
        _accumulator = null;
        _pendingOp = null;
        _freshResult = false;
        RefreshDisplay();
    }

    private void Backspace()
    {
        if (_freshResult) return;
        if (_current.Length > 1)
            _current = _current[..^1];
        else
            _current = "0";
        RefreshDisplay();
    }

    private void Digit(string d)
    {
        if (_freshResult) { _current = "0"; _freshResult = false; }
        if (_current == "0")
            _current = d;
        else
            _current += d;
        RefreshDisplay();
    }

    private void Dot()
    {
        if (_freshResult) { _current = "0"; _freshResult = false; }
        if (!_current.Contains('.'))
            _current += ".";
        RefreshDisplay();
    }

    private void ToggleSign()
    {
        if (_freshResult) { _freshResult = false; }
        if (_current.StartsWith('-'))
            _current = _current[1..];
        else if (_current != "0")
            _current = "-" + _current;
        RefreshDisplay();
    }

    private bool TryParseCurrent(out double v) =>
        double.TryParse(_current, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    private void Binary(int op, string symbol)
    {
        if (!TryParseCurrent(out var a)) return;
        if (_pendingOp is int p && _accumulator is double acc)
        {
            // 连续运算：先结算前一个
            _accumulator = RunBinary(p, acc, a, symbol);
            _pendingOp = op;
        }
        else
        {
            _accumulator = a;
            _pendingOp = op;
        }
        _freshResult = false;
    }

    private void Evaluate()
    {
        if (_pendingOp is not int op || _accumulator is not double acc) return;
        if (!TryParseCurrent(out var b)) return;
        var symbol = OpSymbol(op);
        _accumulator = RunBinary(op, acc, b, symbol);
        _pendingOp = null;
        _freshResult = true;
    }

    private double RunBinary(int op, double a, double b, string symbol)
    {
        double gpuMs = 0, cpuMs = 0;
        double gpuResult = double.NaN, cpuResult;
        var rc = GpuCalculatorNative.CalcBinary(op, a, b, out gpuResult, out gpuMs);
        cpuResult = GpuCalculatorNative.CpuBinary(op, a, b, out cpuMs);

        var gpuOk = rc == 0;
        var result = gpuOk ? gpuResult : cpuResult;
        _current = Format(result);
        _timing.Text = $"GPU: {(gpuOk ? gpuMs.ToString("F4") : "ERR")} ms | CPU: {cpuMs.ToString("F4")} ms | Speedup: {SpeedupText(gpuMs, cpuMs, gpuOk)}";
        _history.AppendText($"{Format(a)} {symbol} {Format(b)} = {Format(result)}   (GPU {gpuMs:F4}ms / CPU {cpuMs:F4}ms)\r\n");
        RefreshDisplay();
        return result;
    }

    private void Unary(int op, string name)
    {
        if (!TryParseCurrent(out var a)) return;
        double gpuMs = 0, cpuMs = 0;
        double gpuResult = double.NaN, cpuResult;
        var rc = GpuCalculatorNative.CalcUnary(op, a, out gpuResult, out gpuMs);
        cpuResult = GpuCalculatorNative.CpuUnary(op, a, out cpuMs);

        var gpuOk = rc == 0;
        var result = gpuOk ? gpuResult : cpuResult;
        _current = Format(result);
        _timing.Text = $"GPU: {(gpuOk ? gpuMs.ToString("F4") : "ERR")} ms | CPU: {cpuMs.ToString("F4")} ms | Speedup: {SpeedupText(gpuMs, cpuMs, gpuOk)}";
        _history.AppendText($"{name}({Format(a)}) = {Format(result)}   (GPU {gpuMs:F4}ms / CPU {cpuMs:F4}ms)\r\n");
        _freshResult = true;
        RefreshDisplay();
    }

    private static string SpeedupText(double gpuMs, double cpuMs, bool gpuOk)
    {
        if (!gpuOk || gpuMs <= 0 || cpuMs <= 0) return "N/A";
        return (cpuMs / gpuMs).ToString("F2") + "x";
    }

    private static string OpSymbol(int op) => op switch
    {
        GpuCalculatorNative.OpAdd => "+",
        GpuCalculatorNative.OpSub => "−",
        GpuCalculatorNative.OpMul => "×",
        GpuCalculatorNative.OpDiv => "÷",
        GpuCalculatorNative.OpPow => "^",
        _ => "?"
    };

    private static string Format(double v)
    {
        if (double.IsNaN(v)) return "NaN";
        if (double.IsInfinity(v)) return v > 0 ? "∞" : "-∞";
        return v.ToString("G15", CultureInfo.InvariantCulture);
    }
}
