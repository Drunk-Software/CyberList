using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CyberList.Controls;

/// <summary>A Border-like container with two diagonally cut corners.</summary>
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

    // 2. Tell Avalonia: when any of these change, redraw
    static CutBorder()
    {
        AffectsRender<CutBorder>(BackgroundProperty, BorderBrushProperty,
                                 StrokeThicknessProperty, CutSizeProperty);
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

    // 4. The drawing
    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var half = StrokeThickness / 2;
        var cut = Math.Min(CutSize, Math.Min(w, h) / 2);

        var shape = new StreamGeometry();
        using (var g = shape.Open())
        {
            g.BeginFigure(new Point(half + cut, half), isFilled: true); // top edge, after the cut
            g.LineTo(new Point(w - half, half));                        // top-right corner
            g.LineTo(new Point(w - half, h - half - cut));              // down to the bottom-right cut
            g.LineTo(new Point(w - half - cut, h - half));              // diagonal
            g.LineTo(new Point(half, h - half));                        // bottom-left corner
            g.LineTo(new Point(half, half + cut));                      // up to the top-left cut
            g.EndFigure(isClosed: true);                                // diagonal back to start
        }

        var pen = BorderBrush is null ? null : new Pen(BorderBrush, StrokeThickness);
        context.DrawGeometry(Background, pen, shape);
    }
}