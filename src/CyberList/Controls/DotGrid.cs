using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CyberList.Controls;

/// <summary>
/// A faint grid of dots with a small "+" crosshair every few cells, like a HUD map background.
/// Skips the cut corners so it matches a CutBorder with the same CutSize and Corners.
/// </summary>
public class DotGrid : Control
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<DotGrid, double>(nameof(Spacing), 18,
            coerce: (_, v) => Math.Max(4, v));

    public static readonly StyledProperty<double> CutSizeProperty =
        AvaloniaProperty.Register<DotGrid, double>(nameof(CutSize));

    public static readonly StyledProperty<CutCorners> CornersProperty =
        AvaloniaProperty.Register<DotGrid, CutCorners>(nameof(Corners), CutCorners.TopLeft | CutCorners.BottomRight);

    public static readonly StyledProperty<IBrush?> DotBrushProperty =
        AvaloniaProperty.Register<DotGrid, IBrush?>(nameof(DotBrush));

    public static readonly StyledProperty<IBrush?> CrossBrushProperty =
        AvaloniaProperty.Register<DotGrid, IBrush?>(nameof(CrossBrush));

    /// <summary>Draw a crosshair on every Nth grid point (in both directions). 0 = no crosshairs.</summary>
    public static readonly StyledProperty<int> CrossEveryProperty =
        AvaloniaProperty.Register<DotGrid, int>(nameof(CrossEvery));

    static DotGrid()
    {
        AffectsRender<DotGrid>(SpacingProperty, CutSizeProperty, CornersProperty,
                               DotBrushProperty, CrossBrushProperty, CrossEveryProperty);
    }

    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public double CutSize { get => GetValue(CutSizeProperty); set => SetValue(CutSizeProperty, value); }
    public CutCorners Corners { get => GetValue(CornersProperty); set => SetValue(CornersProperty, value); }
    public IBrush? DotBrush { get => GetValue(DotBrushProperty); set => SetValue(DotBrushProperty, value); }
    public IBrush? CrossBrush { get => GetValue(CrossBrushProperty); set => SetValue(CrossBrushProperty, value); }
    public int CrossEvery { get => GetValue(CrossEveryProperty); set => SetValue(CrossEveryProperty, value); }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var cut = CutSize + Spacing / 2; // keep a little clear of the diagonal edges
        var corners = Corners;

        bool InCut(double x, double y) =>
            (corners.HasFlag(CutCorners.TopLeft) && x + y < cut) ||
            (corners.HasFlag(CutCorners.TopRight) && (w - x) + y < cut) ||
            (corners.HasFlag(CutCorners.BottomRight) && (w - x) + (h - y) < cut) ||
            (corners.HasFlag(CutCorners.BottomLeft) && x + (h - y) < cut);

        var row = 0;
        for (var y = Spacing / 2; y < h; y += Spacing, row++)
        {
            var col = 0;
            for (var x = Spacing / 2; x < w; x += Spacing, col++)
            {
                if (InCut(x, y))
                    continue;

                var px = Math.Floor(x);
                var py = Math.Floor(y);
                var isCross = CrossEvery > 0 && row % CrossEvery == CrossEvery / 2 && col % CrossEvery == CrossEvery / 2;

                if (isCross && CrossBrush is { } cross)
                {
                    // A 7px "+": one horizontal and one vertical 1px bar through the grid point.
                    context.FillRectangle(cross, new Rect(px - 3, py, 7, 1));
                    context.FillRectangle(cross, new Rect(px, py - 3, 1, 7));
                }
                else if (DotBrush is { } dot)
                {
                    context.FillRectangle(dot, new Rect(px, py, 1, 1));
                }
            }
        }
    }
}
