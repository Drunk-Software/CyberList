using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CyberList.Controls;

/// <summary>
/// The left part of a tree row: draws the connector lines (verticals for each ancestor level,
/// the elbow into this row, the line down to an open folder's children) and a dot for leaves.
/// Its single child, the +/- expander, is placed on this row's node position.
///
///   level:  0    1    2
///           │    ├────■   ← this row (Depth 2): elbow at level 1, node at level 2
///           │    │
/// </summary>
public class TreeGuides : Decorator
{
    public static readonly StyledProperty<int> DepthProperty =
        AvaloniaProperty.Register<TreeGuides, int>(nameof(Depth));

    public static readonly StyledProperty<double> IndentProperty =
        AvaloniaProperty.Register<TreeGuides, double>(nameof(Indent), 20);

    public static readonly StyledProperty<IReadOnlyList<bool>?> LinesProperty =
        AvaloniaProperty.Register<TreeGuides, IReadOnlyList<bool>?>(nameof(Lines));

    public static readonly StyledProperty<bool> IsLastChildProperty =
        AvaloniaProperty.Register<TreeGuides, bool>(nameof(IsLastChild));

    public static readonly StyledProperty<bool> ShowDownLineProperty =
        AvaloniaProperty.Register<TreeGuides, bool>(nameof(ShowDownLine));

    public static readonly StyledProperty<bool> IsLeafProperty =
        AvaloniaProperty.Register<TreeGuides, bool>(nameof(IsLeaf));

    public static readonly StyledProperty<bool> IsDoneProperty =
        AvaloniaProperty.Register<TreeGuides, bool>(nameof(IsDone));

    public static readonly StyledProperty<IBrush?> LineBrushProperty =
        AvaloniaProperty.Register<TreeGuides, IBrush?>(nameof(LineBrush));

    public static readonly StyledProperty<IBrush?> NodeBrushProperty =
        AvaloniaProperty.Register<TreeGuides, IBrush?>(nameof(NodeBrush));

    public static readonly StyledProperty<IBrush?> DoneNodeBrushProperty =
        AvaloniaProperty.Register<TreeGuides, IBrush?>(nameof(DoneNodeBrush));

    static TreeGuides()
    {
        AffectsMeasure<TreeGuides>(DepthProperty, IndentProperty);
        AffectsRender<TreeGuides>(DepthProperty, IndentProperty, LinesProperty, IsLastChildProperty,
            ShowDownLineProperty, IsLeafProperty, IsDoneProperty,
            LineBrushProperty, NodeBrushProperty, DoneNodeBrushProperty);
    }

    public int Depth { get => GetValue(DepthProperty); set => SetValue(DepthProperty, value); }
    public double Indent { get => GetValue(IndentProperty); set => SetValue(IndentProperty, value); }
    public IReadOnlyList<bool>? Lines { get => GetValue(LinesProperty); set => SetValue(LinesProperty, value); }
    public bool IsLastChild { get => GetValue(IsLastChildProperty); set => SetValue(IsLastChildProperty, value); }
    public bool ShowDownLine { get => GetValue(ShowDownLineProperty); set => SetValue(ShowDownLineProperty, value); }
    public bool IsLeaf { get => GetValue(IsLeafProperty); set => SetValue(IsLeafProperty, value); }
    public bool IsDone { get => GetValue(IsDoneProperty); set => SetValue(IsDoneProperty, value); }
    public IBrush? LineBrush { get => GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }
    public IBrush? NodeBrush { get => GetValue(NodeBrushProperty); set => SetValue(NodeBrushProperty, value); }
    public IBrush? DoneNodeBrush { get => GetValue(DoneNodeBrushProperty); set => SetValue(DoneNodeBrushProperty, value); }

    /// <summary>Centre of indentation column <paramref name="level"/>, on a half pixel so 1px lines stay sharp.</summary>
    private double ColumnX(int level) => Math.Floor(level * Indent + Indent / 2) + 0.5;

    // Layout: one Indent-wide column per level, plus this row's own node column.
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        var childHeight = Child?.DesiredSize.Height ?? 0;
        return new Size((Depth + 1) * Indent, childHeight);
    }

    // Put the expander (our Child) centred on this row's node position.
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            var size = child.DesiredSize;
            var x = Depth * Indent + (Indent - size.Width) / 2;
            var y = (finalSize.Height - size.Height) / 2;
            child.Arrange(new Rect(new Point(x, y), size));
        }
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        var h = Bounds.Height;
        var mid = Math.Floor(h / 2) + 0.5;
        var pen = LineBrush is null ? null : new Pen(LineBrush, 1);

        // Half the size of this row's node, so lines stop at its edge instead of crossing it.
        var nodeHalf = IsLeaf ? 3.5 : (Child?.Bounds.Width ?? 13) / 2;

        if (pen is not null)
        {
            // 1. Pass-through verticals for ancestors that still have siblings further down.
            var lines = Lines;
            for (var level = 0; level < Depth - 1; level++)
            {
                if (lines is not null && level < lines.Count && lines[level])
                    context.DrawLine(pen, new Point(ColumnX(level), 0), new Point(ColumnX(level), h));
            }

            // 2. The elbow from the parent's column into this row's node.
            if (Depth > 0)
            {
                var x = ColumnX(Depth - 1);
                context.DrawLine(pen, new Point(x, 0), new Point(x, IsLastChild ? mid : h));
                context.DrawLine(pen, new Point(x, mid), new Point(ColumnX(Depth) - nodeHalf, mid));

                // A tiny joint dot where the elbow turns, like the junctions in a network map.
                if (LineBrush is not null)
                    context.DrawRectangle(LineBrush, null, new Rect(x - 1.5, mid - 1.5, 3, 3));
            }

            // 3. An open folder: line from the bottom of its node down to its first child.
            if (ShowDownLine)
            {
                var x = ColumnX(Depth);
                context.DrawLine(pen, new Point(x, mid + nodeHalf), new Point(x, h));
            }
        }

        // 4. Leaves get a small square node: hot when pending, dim when done.
        if (IsLeaf)
        {
            var brush = IsDone ? DoneNodeBrush : NodeBrush;
            if (brush is not null)
            {
                var x = ColumnX(Depth);
                context.DrawRectangle(brush, null, new Rect(x - 3.5, mid - 3.5, 7, 7));
            }
        }
    }
}
