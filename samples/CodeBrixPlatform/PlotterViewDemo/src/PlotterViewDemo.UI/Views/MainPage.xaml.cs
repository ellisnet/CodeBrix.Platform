using System;
using CodeBrix.Plotter;
using CodeBrix.Plotter.Axes;
using CodeBrix.Plotter.Legends;
using CodeBrix.Plotter.Series;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PlotterViewDemo.Views;

public sealed partial class MainPage : Page
{
    private const double StreamWindowSeconds = 10.0;
    private const double StreamSampleHz = 200.0;
    private const int StreamSamplesPerTick = 7;

    private readonly PlotModel[] _models;
    private readonly string[] _descriptions;
    private readonly PlotModel _streamingModel;
    private readonly LineSeries _streamA;
    private readonly LineSeries _streamB;
    private readonly LinearAxis _streamTimeAxis;
    private readonly DispatcherTimer _streamTimer;
    private readonly Random _noise = new(12345);
    private double _streamClock;

    public MainPage()
    {
        this.InitializeComponent();

        _streamingModel = BuildStreamingModel(out _streamA, out _streamB, out _streamTimeAxis);
        _models =
        [
            _streamingModel,
            BuildFunctionModel(),
            BuildScatterModel(),
            BuildBarModel(),
            BuildPieModel(),
            BuildHeatMapModel(),
        ];
        _descriptions =
        [
            "A synthesized two-channel stream: the model mutates and calls InvalidatePlot, the control repaints.",
            "Two FunctionSeries sampled from lambdas. Pan and zoom to explore them.",
            "Three ScatterSeries clusters, 300 points each. Left-click a point for the tracker.",
            "A BarSeries against a CategoryAxis - one bar per platform head.",
            "A PieSeries. The tracker reports a slice's label and value.",
            "A HeatMapSeries over an 80x80 grid through the Viridis palette, with its color axis on the right.",
        ];

        //~200 samples/s arriving in ~30 ms batches, scope-style
        _streamTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 * StreamSamplesPerTick / StreamSampleHz) };
        _streamTimer.Tick += (_, _) => AdvanceStream();

        //Start on the streaming chart (fires ChartCombo_SelectionChanged)
        ChartCombo.SelectedIndex = 0;

        Loaded += (_, _) => Plotter.Focus(FocusState.Programmatic);
        Unloaded += (_, _) => _streamTimer.Stop();
    }

    private void ChartCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = ChartCombo.SelectedIndex;
        if (index < 0 || index >= _models.Length)
        {
            return;
        }

        //The Model setter detaches the previous model and attaches this one, so the
        //cached models stay reusable across switches
        Plotter.Model = _models[index];
        StatusText.Text = _descriptions[index];
        LiveText.Text = string.Empty;

        if (ReferenceEquals(_models[index], _streamingModel))
        {
            _streamTimer.Start();
        }
        else
        {
            _streamTimer.Stop();
        }

        Plotter.Focus(FocusState.Programmatic);
    }

    private void ResetView_Click(object sender, RoutedEventArgs e)
    {
        //The same effect as the controller's reset bindings (double-middle-click, A, Home)
        Plotter.ActualModel?.ResetAllAxes();
        Plotter.InvalidatePlot(false);
        Plotter.Focus(FocusState.Programmatic);
    }

    private void AdvanceStream()
    {
        for (var i = 0; i < StreamSamplesPerTick; i++)
        {
            _streamClock += 1.0 / StreamSampleHz;
            var noise = (_noise.NextDouble() - 0.5) * 0.08;
            _streamA.Points.Add(new DataPoint(
                _streamClock,
                Math.Sin(2 * Math.PI * _streamClock)
                    + (0.35 * Math.Sin((2 * Math.PI * 3 * _streamClock) + 0.6))
                    + noise));
            _streamB.Points.Add(new DataPoint(
                _streamClock,
                (0.6 * Math.Sin((2 * Math.PI * 0.5 * _streamClock) + 1.1)) + noise));
        }

        //A scope-style scrolling window: drop what fell out, slide the time axis
        var cutoff = _streamClock - StreamWindowSeconds;
        _streamA.Points.RemoveAll(p => p.X < cutoff);
        _streamB.Points.RemoveAll(p => p.X < cutoff);
        _streamTimeAxis.Minimum = Math.Max(0, cutoff);
        _streamTimeAxis.Maximum = Math.Max(StreamWindowSeconds, _streamClock);

        _streamingModel.InvalidatePlot(true);
        LiveText.Text = $"{_streamA.Points.Count + _streamB.Points.Count} live points";
    }

    private static PlotModel BuildStreamingModel(
        out LineSeries channelA, out LineSeries channelB, out LinearAxis timeAxis)
    {
        var model = new PlotModel
        {
            Title = "Live Signal",
            Subtitle = "two synthesized channels, 200 samples per second",
        };

        timeAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Seconds",
            Minimum = 0,
            Maximum = StreamWindowSeconds,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
        };
        model.Axes.Add(timeAxis);
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Volts",
            Minimum = -1.8,
            Maximum = 1.8,
            MajorGridlineStyle = LineStyle.Solid,
        });

        channelA = new LineSeries { Title = "Channel A" };
        channelB = new LineSeries { Title = "Channel B" };
        model.Series.Add(channelA);
        model.Series.Add(channelB);

        model.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.TopRight,
            LegendPlacement = LegendPlacement.Inside,
        });
        return model;
    }

    private static PlotModel BuildFunctionModel()
    {
        var model = new PlotModel { Title = "Function Series" };
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            MajorGridlineStyle = LineStyle.Solid,
        });
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            MajorGridlineStyle = LineStyle.Solid,
        });
        model.Series.Add(new FunctionSeries(x => Math.Sin(x) / x, -20, 20, 0.05, "sin(x) / x"));
        model.Series.Add(new FunctionSeries(
            x => Math.Cos(x) * Math.Exp(-Math.Abs(x) / 8.0), -20, 20, 0.05, "cos(x) * e^(-|x|/8)"));
        model.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.TopRight,
            LegendPlacement = LegendPlacement.Inside,
        });
        return model;
    }

    private static PlotModel BuildScatterModel()
    {
        var model = new PlotModel { Title = "Scatter", Subtitle = "three clusters, 300 points each" };
        var random = new Random(4711);

        (string Title, double CenterX, double CenterY, double Spread)[] clusters =
        [
            ("Alpha", -2.0, 1.5, 0.8),
            ("Beta", 1.8, 2.2, 0.5),
            ("Gamma", 0.4, -1.6, 1.1),
        ];

        foreach (var (title, centerX, centerY, spread) in clusters)
        {
            var series = new ScatterSeries { Title = title, MarkerType = MarkerType.Circle, MarkerSize = 3 };
            for (var i = 0; i < 300; i++)
            {
                //Box-Muller, for round clusters rather than square ones
                var radius = spread * Math.Sqrt(-2.0 * Math.Log(1.0 - random.NextDouble()));
                var angle = 2.0 * Math.PI * random.NextDouble();
                series.Points.Add(new ScatterPoint(
                    centerX + (radius * Math.Cos(angle)),
                    centerY + (radius * Math.Sin(angle))));
            }

            model.Series.Add(series);
        }

        model.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.TopRight,
            LegendPlacement = LegendPlacement.Inside,
        });
        return model;
    }

    private static PlotModel BuildBarModel()
    {
        var model = new PlotModel { Title = "Bar Chart", Subtitle = "platform heads by demo sessions" };

        var categoryAxis = new CategoryAxis { Position = AxisPosition.Left };
        categoryAxis.Labels.AddRange(
            ["Linux X11", "Linux Wayland", "Frame Buffer", "Windows Win32", "Windows WPF", "macOS"]);
        model.Axes.Add(categoryAxis);
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            MinimumPadding = 0,
            AbsoluteMinimum = 0,
            MajorGridlineStyle = LineStyle.Solid,
        });

        var series = new BarSeries();
        series.Items.AddRange(
        [
            new BarItem { Value = 42 },
            new BarItem { Value = 31 },
            new BarItem { Value = 17 },
            new BarItem { Value = 38 },
            new BarItem { Value = 26 },
            new BarItem { Value = 22 },
        ]);
        model.Series.Add(series);
        return model;
    }

    private static PlotModel BuildPieModel()
    {
        var model = new PlotModel { Title = "Pie" };
        var series = new PieSeries { StrokeThickness = 2, InsideLabelPosition = 0.7 };
        series.Slices.Add(new PieSlice("Line", 34));
        series.Slices.Add(new PieSlice("Scatter", 22));
        series.Slices.Add(new PieSlice("Bar", 18));
        series.Slices.Add(new PieSlice("Heat map", 15));
        series.Slices.Add(new PieSlice("Pie", 11));
        model.Series.Add(series);
        return model;
    }

    private static PlotModel BuildHeatMapModel()
    {
        var model = new PlotModel { Title = "Heat Map", Subtitle = "peaks(x, y)" };
        model.Axes.Add(new LinearColorAxis
        {
            Position = AxisPosition.Right,
            Palette = PlotterPalettes.Viridis(200),
        });

        const int size = 80;
        var data = new double[size, size];
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                var x = -3.0 + (6.0 * i / (size - 1));
                var y = -3.0 + (6.0 * j / (size - 1));

                //MATLAB's classic peaks() surface
                data[i, j] =
                    (3.0 * (1 - x) * (1 - x) * Math.Exp(-(x * x) - ((y + 1) * (y + 1))))
                    - (10.0 * ((x / 5.0) - (x * x * x) - (y * y * y * y * y)) * Math.Exp(-(x * x) - (y * y)))
                    - (Math.Exp(-((x + 1) * (x + 1)) - (y * y)) / 3.0);
            }
        }

        model.Series.Add(new HeatMapSeries
        {
            X0 = -3,
            X1 = 3,
            Y0 = -3,
            Y1 = 3,
            Data = data,
            Interpolate = true,
        });
        return model;
    }
}
