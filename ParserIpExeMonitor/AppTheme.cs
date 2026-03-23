using System.Drawing.Drawing2D;

namespace ParserIpExeMonitor;

/// <summary>Цвета и шрифты для единого современного оформления.</summary>
internal static class AppTheme
{
    public static readonly Color BgDeep = Color.FromArgb(18, 18, 24);
    public static readonly Color BgSurface = Color.FromArgb(28, 28, 36);
    public static readonly Color BgElevated = Color.FromArgb(38, 38, 50);
    public static readonly Color BorderSubtle = Color.FromArgb(55, 55, 70);
    public static readonly Color TextPrimary = Color.FromArgb(238, 238, 245);
    public static readonly Color TextMuted = Color.FromArgb(150, 150, 168);
    public static readonly Color Accent = Color.FromArgb(99, 102, 241);
    public static readonly Color AccentHover = Color.FromArgb(129, 140, 248);
    public static readonly Color AccentDanger = Color.FromArgb(239, 68, 68);
    public static readonly Color AccentDangerHover = Color.FromArgb(248, 113, 113);
    public static readonly Color AccentSuccess = Color.FromArgb(34, 197, 94);

    public static Font UiFont(float sizeInPoints = 10f, FontStyle style = FontStyle.Regular) =>
        new("Segoe UI", sizeInPoints, style, GraphicsUnit.Point);

    public static Font TitleFont() => new("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point);

    public static Font CaptionFont() => new("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);

    /// <summary>Закруглённая «карточка» для панелей.</summary>
    public static void PaintCardBorder(Graphics g, Rectangle bounds, Color borderColor, int radius = 10)
    {
        using var path = RoundedRect(bounds, radius);
        using var pen = new Pen(borderColor, 1f);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.DrawPath(pen, path);
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
