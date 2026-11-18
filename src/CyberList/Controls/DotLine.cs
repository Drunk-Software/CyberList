using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CyberList.Controls;

/// <summary>A horizontal row of small square dots: ▪ ▪ ▪ ▪ ▪ ▪. Used as a text field's underline.</summary>
public class DotLine : Control
{
    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<DotLine, IBrush?>(nameof(Brush));

    public static readonly StyledProperty<double> DotSizeProperty =
        AvaloniaProperty.Register<DotLine, double>(nameof(DotSize), 2);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<DotLine, double>(nameof(Spacing), 5,
            coerce: (_, v) => Math.Max(1, v));

    static DotLine()
    {
        AffectsRender<DotLine>(BrushProperty, DotSizeProperty, SpacingProperty);
        AffectsMeasure<DotLine>(DotSizeProperty);
    }

    public IBrush? Brush { get => GetValue(BrushProperty); set => SetValue(BrushProperty, value); }
    /// <summary>Width and height of each dot.</summary>
    public double DotSize { get => GetValue(DotSizeProperty); set => SetValue(DotSizeProperty, value); }
    /// <summary>Distance from one dot's left edge to the next.</summary>
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    protected override Size MeasureOverride(Size availableSize) => new(0, DotSize);

    public override void Render(DrawingContext context)
    {
        if (Brush is not { } brush)
            return;
        for (var x = 0.0; x + DotSize <= Bounds.Width; x += Spacing)
            context.FillRectangle(brush, new Rect(x, 0, DotSize, DotSize));
    }
}
