using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CyberList.Controls;

/// <summary>Which corners of a <see cref="CutBorder"/> are cut diagonally. Combine with commas in XAML: "BottomLeft,BottomRight".</summary>
[Flags]
public enum CutCorners
{
    None = 0,
    TopLeft = 1,
    TopRight = 2,
    BottomRight = 4,
    BottomLeft = 8,
}

/// <summary>A Border-like container with diagonally cut (chamfered) corners.</summary>
public class CutBorder : Decorator
{
    // 1. Declare the properties (so they can be set in XAML, styled and bound)
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<CutBorder, IBrush?>(nameof(Background));

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<CutBorder, IBrush?>(nameof(BorderBrush));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<CutBorder, double>(nameof(StrokeThickness), 1);

    public static readonly StyledProperty<double> CutSizeProperty =
        AvaloniaProperty.Register<CutBorder, double>(nameof(CutSize), 10);

    public static readonly StyledProperty<CutCorners> CornersProperty =
        AvaloniaProperty.Register<CutBorder, CutCorners>(nameof(Corners), CutCorners.TopLeft | CutCorners.BottomRight);

    // 2. Tell Avalonia: when any of these change, redraw
    static CutBorder()
    {
        AffectsRender<CutBorder>(BackgroundProperty, BorderBrushProperty,
                                 StrokeThicknessProperty, CutSizeProperty, CornersProperty);
    }

    // 3. Normal C# wrappers around the styled properties
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double CutSize
    {
        get => GetValue(CutSizeProperty);
        set => SetValue(CutSizeProperty, value);
    }

    public CutCorners Corners
    {
        get => GetValue(CornersProperty);
        set => SetValue(CornersProperty, value);
    }

    // 4. The drawing
    public override void Render(DrawingContext context)
    {
        var pen = BorderBrush is null ? null : new Pen(BorderBrush, StrokeThickness);
        var shape = Build(new Rect(Bounds.Size), StrokeThickness, CutSize, Corners);
        context.DrawGeometry(Background, pen, shape);
    }

    /// <summary>
    /// The outline of a rectangle with some corners chamfered, inset by half the stroke so the
    /// stroke stays inside the bounds. Shared with <see cref="HudFrame"/>.
    /// </summary>
    public static StreamGeometry Build(Rect r, double stroke, double cutSize, CutCorners corners)
    {
        var half = stroke / 2;
        var (l, t, rt, b) = (r.Left + half, r.Top + half, r.Right - half, r.Bottom - half);
        var cut = Math.Max(0, Math.Min(cutSize, Math.Min(r.Width, r.Height) / 2));
        double C(CutCorners c) => corners.HasFlag(c) ? cut : 0;

        var shape = new StreamGeometry();
        using var g = shape.Open();
        // Clockwise from the top-left corner. A corner with cut 0 just puts two points on top of each other.
        g.BeginFigure(new Point(l + C(CutCorners.TopLeft), t), isFilled: true);
        g.LineTo(new Point(rt - C(CutCorners.TopRight), t));
        g.LineTo(new Point(rt, t + C(CutCorners.TopRight)));
        g.LineTo(new Point(rt, b - C(CutCorners.BottomRight)));
        g.LineTo(new Point(rt - C(CutCorners.BottomRight), b));
        g.LineTo(new Point(l + C(CutCorners.BottomLeft), b));
        g.LineTo(new Point(l, b - C(CutCorners.BottomLeft)));
        g.LineTo(new Point(l, t + C(CutCorners.TopLeft)));
        g.EndFigure(isClosed: true);
        return shape;
    }
}
