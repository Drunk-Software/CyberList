using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CyberList.Controls;

/// <summary>
/// Pure decoration: the thin outer bracket around a panel, with the little extras that make
/// the reference look "engineered" — bright corner ticks, grey tabs clipped onto the edges,
/// tiny amber markers and one red tick. Draws nothing it could be clicked through for.
///
///    ┌─            ▬▬            ─┐ ▪▪
///    ▌                            │
///    │                            ▌
///    └──╲________________________╱─┘
/// </summary>
public class HudFrame : Control
{
    public static readonly StyledProperty<IBrush?> LineBrushProperty =
        AvaloniaProperty.Register<HudFrame, IBrush?>(nameof(LineBrush));

    public static readonly StyledProperty<IBrush?> TickBrushProperty =
        AvaloniaProperty.Register<HudFrame, IBrush?>(nameof(TickBrush));

    public static readonly StyledProperty<IBrush?> MarkerBrushProperty =
        AvaloniaProperty.Register<HudFrame, IBrush?>(nameof(MarkerBrush));

    public static readonly StyledProperty<IBrush?> AccentBrushProperty =
        AvaloniaProperty.Register<HudFrame, IBrush?>(nameof(AccentBrush));

    public static readonly StyledProperty<double> CutSizeProperty =
        AvaloniaProperty.Register<HudFrame, double>(nameof(CutSize), 20);

    static HudFrame()
    {
        AffectsRender<HudFrame>(LineBrushProperty, TickBrushProperty, MarkerBrushProperty,
                                AccentBrushProperty, CutSizeProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<HudFrame>(false);
    }

    /// <summary>The bracket outline itself.</summary>
    public IBrush? LineBrush { get => GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }
    /// <summary>Bright corner ticks and edge tabs.</summary>
    public IBrush? TickBrush { get => GetValue(TickBrushProperty); set => SetValue(TickBrushProperty, value); }
    /// <summary>The tiny amber squares.</summary>
    public IBrush? MarkerBrush { get => GetValue(MarkerBrushProperty); set => SetValue(MarkerBrushProperty, value); }
    /// <summary>The single red tick.</summary>
    public IBrush? AccentBrush { get => GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }
    public double CutSize { get => GetValue(CutSizeProperty); set => SetValue(CutSizeProperty, value); }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w < 80 || h < 80)
            return;

        // 1. The outline: chamfered at the two bottom corners, like the reference panels.
        if (LineBrush is { } line)
        {
            var outline = CutBorder.Build(new Rect(Bounds.Size), 1, CutSize,
                                          CutCorners.BottomLeft | CutCorners.BottomRight);
            context.DrawGeometry(null, new Pen(line, 1), outline);
        }

        // 2. Bright L-shaped ticks on the two square (top) corners: 12px arms, 2px thick.
        if (TickBrush is { } tick)
        {
            const double arm = 12, t = 2;
            context.FillRectangle(tick, new Rect(0, 0, arm, t));
            context.FillRectangle(tick, new Rect(0, 0, t, arm));
            context.FillRectangle(tick, new Rect(w - arm, 0, arm, t));
            context.FillRectangle(tick, new Rect(w - t, 0, t, arm));

            // Tabs "clipped" onto the edges: left edge high up, right edge low down, one on top.
            context.FillRectangle(tick, new Rect(-1, 56, 3, 28));
            context.FillRectangle(tick, new Rect(w - 2, h - 120, 3, 36));
            context.FillRectangle(tick, new Rect(w * 0.62, -1, 34, 3));

            // A run of short ticks along the bottom edge, like a ruler.
            for (var i = 0; i < 6; i++)
                context.FillRectangle(tick, new Rect(CutSize + 18 + i * 6, h - 4, 1, 4));
        }

        // 3. Amber: two little squares off the top-right corner, and one bar bottom-left.
        if (MarkerBrush is { } marker)
        {
            context.FillRectangle(marker, new Rect(w - 30, -8, 4, 4));
            context.FillRectangle(marker, new Rect(w - 22, -8, 4, 4));
            context.FillRectangle(marker, new Rect(-8, h - CutSize - 40, 3, 14));
        }

        // 4. One red tick on the bottom edge, just before the right chamfer.
        if (AccentBrush is { } accent)
            context.FillRectangle(accent, new Rect(w - CutSize - 46, h - 2, 26, 2));
    }
}
