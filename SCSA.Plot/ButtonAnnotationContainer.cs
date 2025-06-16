using OxyPlot;

namespace SCSA.Plot;

/// <summary>
///     A container that holds multiple ButtonAnnotation instances and positions them in a row or column.
/// </summary>
public class ButtonAnnotationContainer
{
    private readonly List<TextAnnotation> _buttons = new();

    private double offsetAcc;

    public PlotLength OffsetX = new(0, PlotLengthUnit.ScreenUnits);
    public PlotLength OffsetY = new(0, PlotLengthUnit.ScreenUnits);

    /// <summary>Spacing between buttons in screen units.</summary>
    public double Spacing { get; set; } = 5;

    /// <summary>If true, layout horizontally; otherwise vertically.</summary>
    public bool Horizontal { get; set; } = true;

    /// <summary>Starting position in screen units relative to plot area.</summary>
    public PlotLength StartX { get; set; } = new(0, PlotLengthUnit.RelativeToPlotArea);

    public PlotLength StartY { get; set; } = new(0, PlotLengthUnit.RelativeToPlotArea);

    /// <summary>Adds a button to the container.</summary>
    public void Add(TextAnnotation btn)
    {
        _buttons.Add(btn);
    }

    /// <summary>Layouts buttons and adds them to the PlotModel.Annotations.</summary>
    public void Apply(PlotModel model)
    {
        offsetAcc = Horizontal ? OffsetX.Value : OffsetY.Value;


        foreach (var btn in _buttons)
        {
            // set offsets
            if (Horizontal)
            {
                btn.X = StartX;
                btn.Y = StartY;
                btn.OffsetX = new PlotLength(offsetAcc + btn.OffsetX.Value, PlotLengthUnit.ScreenUnits);
                btn.OffsetY = OffsetY;
                offsetAcc += btn.Width.Value + Spacing;
            }
            else
            {
                btn.X = StartX;
                btn.Y = StartY;
                btn.OffsetX = new PlotLength(0, PlotLengthUnit.ScreenUnits);
                btn.OffsetY = new PlotLength(offsetAcc + btn.OffsetY.Value, PlotLengthUnit.ScreenUnits);
                offsetAcc += btn.Height.Value + Spacing;
            }

            model.Annotations.Add(btn);

            model.InvalidatePlot(false);
        }
    }
}