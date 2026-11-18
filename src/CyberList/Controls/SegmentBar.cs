using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CyberList.Controls;

/// <summary>
/// A HUD-style progress bar made of separate blocks: ▮▮▮▮▮▯▯▯▯▯
/// </summary>
public class SegmentBar : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<SegmentBar, double>(nameof(Value), 0,
            coerce: (_, v) => Math.Clamp(v, 0, 1));

    public static readonly StyledProperty<int> SegmentsProperty =
        AvaloniaProperty.Register<SegmentBar, int>(nameof(Segments), 20,
            coerce: (_, v) => Math.Max(1, v));

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<SegmentBar, double>(nameof(Gap), 2);

    public static readonly StyledProperty<IBrush?> OnBrushProperty =
        AvaloniaProperty.Register<SegmentBar, IBrush?>(nameof(OnBrush));

    public static readonly StyledProperty<IBrush?> OffBrushProperty =
        AvaloniaProperty.Register<SegmentBar, IBrush?>(nameof(OffBrush));

    static SegmentBar()
    {
        AffectsRender<SegmentBar>(ValueProperty, SegmentsProperty, GapProperty,
                                  OnBrushProperty, OffBrushProperty);
    }

    /// <summary>Fraction done, 0..1 (values outside are clamped).</summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    /// <summary>Space between blocks, in pixels.</summary>
    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    public IBrush? OnBrush
    {
        get => GetValue(OnBrushProperty);
        set => SetValue(OnBrushProperty, value);
    }

    public IBrush? OffBrush
    {
        get => GetValue(OffBrushProperty);
        set => SetValue(OffBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var count = Segments;
        var segmentWidth = (Bounds.Width - Gap * (count - 1)) / count;
        if (segmentWidth <= 0)
            return; // too narrow to draw anything sensible

        var lit = (int)Math.Round(Value * count);
        var x = 0.0;

        for (var i = 0; i < count; i++)
        {
            var brush = i < lit ? OnBrush : OffBrush;
            if (brush is not null)
                context.DrawRectangle(brush, null, new Rect(x, 0, segmentWidth, Bounds.Height));
            x += segmentWidth + Gap;
        }
    }
}
