#nullable enable

using CodeBrix.Platform.UI.TerminalView.Rendering;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.TerminalView.Tests;

//was previously: Lily.Shell.TerminalView.Tests.CellMetricsTests, namespace changed.

public class CellMetricsTests
{
    [Fact]
    public void measuring_the_default_font_yields_positive_cell_geometry()
    {
        //Act - null family = the engine's default font; proves the TextLayout
        //  plumbing runs headless in a plain test process
        var metrics = CellMetrics.Measure(null, 14f);

        //Assert
        metrics.Width.Should().BeGreaterThan(0f);
        metrics.Height.Should().BeGreaterThan(0f);
        metrics.Baseline.Should().BeGreaterThan(0f);
        metrics.Height.Should().BeGreaterThanOrEqualTo(metrics.Baseline);
    }

    [Fact]
    public void a_larger_font_size_yields_larger_cells()
    {
        //Act
        var small = CellMetrics.Measure(null, 10f);
        var large = CellMetrics.Measure(null, 20f);

        //Assert
        large.Width.Should().BeGreaterThan(small.Width);
        large.Height.Should().BeGreaterThan(small.Height);
    }
}
