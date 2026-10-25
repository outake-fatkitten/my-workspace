using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClickCounter;

public sealed class CounterForm : Form
{
    private readonly Label countLabel;
    private int count;

    public CounterForm()
    {
        Text = "Click Counter";
        ClientSize = new Size(340, 180);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        countLabel = new Label
        {
            Text = "Count: 0",
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 80,
        };

        var countButton = new Button
        {
            Text = "Count +1",
            Font = new Font("Segoe UI", 12F),
            Size = new Size(150, 48),
            Location = new Point(30, 110),
        };
        countButton.Click += (_, _) =>
        {
            count++;
            countLabel.Text = $"Count: {count}";
        };

        var resetButton = new Button
        {
            Text = "Reset",
            Font = new Font("Segoe UI", 12F),
            Size = new Size(150, 48),
            Location = new Point(190, 110),
        };
        resetButton.Click += (_, _) =>
        {
            count = 0;
            countLabel.Text = "Count: 0";
        };

        Controls.Add(countButton);
        Controls.Add(resetButton);
        Controls.Add(countLabel);
    }
}
