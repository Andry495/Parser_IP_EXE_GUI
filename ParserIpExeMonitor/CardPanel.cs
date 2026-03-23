using System.Drawing.Drawing2D;

namespace ParserIpExeMonitor;

/// <summary>Панель-карточка с мягким фоном и скруглённой рамкой.</summary>
internal sealed class CardPanel : Panel
{
    private readonly int _radius;

    public CardPanel(int radius = 10)
    {
        _radius = radius;
        DoubleBuffered = true;
        BackColor = AppTheme.BgElevated;
        Padding = new Padding(16);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        UpdateStyles();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = ClientRectangle;
        r.Width -= 1;
        r.Height -= 1;
        using var path = AppTheme.RoundedRect(r, _radius);
        using var fill = new SolidBrush(BackColor);
        g.FillPath(fill, path);
        using var pen = new Pen(AppTheme.BorderSubtle, 1f);
        g.DrawPath(pen, path);
    }
}
