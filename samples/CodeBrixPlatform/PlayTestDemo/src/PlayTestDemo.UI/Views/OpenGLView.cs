using System;
using CodeBrix.Platform.OpenGL;
using CodeBrix.Platform.WinUI.Graphics3DGL;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace PlayTestDemo.Views;

/// <summary>OpenGL content: a lit square drawn with raw OpenGL, a circle drawn with GPU Skia, and the
/// message the application shows when the machine gives it no OpenGL.</summary>
public sealed class OpenGLView : Grid
{
    public LitSquareCanvas Square { get; } = new() { Width = 360, Height = 360 };
    public SkiaGLCanvasElement Circle { get; } = new() { Width = 360, Height = 360 };
    public TextBlock GLStatus { get; } = new();
    public TextBlock SkiaStatus { get; } = new();
    public TextBlock Fallback { get; } = new() { Text = "3D preview unavailable on this machine", Visibility = Visibility.Collapsed };

    public OpenGLView()
    {
        Padding = new Thickness(24);
        RowSpacing = 12;
        for (var i = 0; i < 4; i++) RowDefinitions.Add(new() { Height = GridLength.Auto });

        Children.Add(new TextBlock { Text = "OpenGL", FontSize = 32 });

        var statuses = new StackPanel { Spacing = 4 };
        AutomationProperties.SetAutomationId(GLStatus, "GLStatus");
        AutomationProperties.SetAutomationId(SkiaStatus, "SkiaGLStatus");
        AutomationProperties.SetAutomationId(Fallback, "GLFallback");
        statuses.Children.Add(GLStatus);
        statuses.Children.Add(SkiaStatus);
        statuses.Children.Add(Fallback);
        SetRow(statuses, 1);
        Children.Add(statuses);

        var rotate = new Button { Content = "Rotate the square" };
        rotate.Click += (_, _) =>
        {
            Square.Angle += 30;
            Square.Invalidate();
        };
        SetRow(rotate, 2);
        Children.Add(rotate);

        var canvases = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24 };
        AutomationProperties.SetAutomationId(Square, "GLCanvas");
        AutomationProperties.SetAutomationId(Circle, "SkiaGLCanvas");
        canvases.Children.Add(Square);
        canvases.Children.Add(Circle);
        SetRow(canvases, 3);
        Children.Add(canvases);

        // The elements set their state in their own Loaded handlers, which run before these.
        Square.Loaded += (_, _) =>
        {
            var state = Square.GetGLInitializationState();
            var ready = state.Status == GLInitializationStatus.Initialized;
            GLStatus.Text = ready ? "OpenGL: initialized" : "OpenGL unavailable: " + state.FailedReason;
            Fallback.Visibility = ready ? Visibility.Collapsed : Visibility.Visible;
        };
        Circle.Loaded += (_, _) => SkiaStatus.Text = Circle.IsGpuInitialized == true ? "GPU Skia: initialized" : "GPU Skia unavailable";
        Circle.PaintSurface += (_, e) =>
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);
            using var paint = new SKPaint { Color = new SKColor(0, 0x50, 0xC8), IsAntialias = true };
            canvas.DrawCircle(e.Info.Width / 2f, e.Info.Height / 2f, e.Info.Width / 4f, paint);
        };
    }
}

/// <summary>A flat orange square, lit from the left, on a dark background; <see cref="Angle"/> turns it.</summary>
public sealed class LitSquareCanvas : GLCanvasElement
{
    // "#version 300 es" compiles on the OpenGL ES heads and on desktop OpenGL (ARB_ES3_compatibility).
    private const string VertexSource = """
        #version 300 es
        precision highp float;
        uniform float uAngle;
        out float vX;
        // Half the canvas on each side, as two triangles: the square covers a quarter of it.
        const vec2 corners[6] = vec2[6](vec2(-0.5, -0.5), vec2(0.5, -0.5), vec2(0.5, 0.5),
                                        vec2(-0.5, -0.5), vec2(0.5, 0.5), vec2(-0.5, 0.5));
        void main()
        {
            vec2 a = corners[gl_VertexID];
            float c = cos(uAngle);
            float s = sin(uAngle);
            vec2 p = vec2(c * a.x - s * a.y, s * a.x + c * a.y);
            vX = p.x;
            gl_Position = vec4(p, 0.0, 1.0);
        }
        """;

    // The light comes from the left: brightness falls from full at the left edge to under a third at the right.
    private const string FragmentSource = """
        #version 300 es
        precision highp float;
        in float vX;
        out vec4 fragColor;
        void main()
        {
            float light = clamp(0.65 - 0.5 * vX, 0.15, 1.0);
            fragColor = vec4(vec3(1.0, 0.5, 0.0) * light, 1.0);
        }
        """;

    private uint _program;
    private uint _vertexArray;
    private int _angleLocation;

    public LitSquareCanvas() : base(null) { }

    /// <summary>The square's rotation in degrees, counter-clockwise.</summary>
    public double Angle { get; set; }

    protected override void Init(GL gl)
    {
        _program = gl.CreateProgram();
        var vertex = Compile(gl, ShaderType.VertexShader, VertexSource);
        var fragment = Compile(gl, ShaderType.FragmentShader, FragmentSource);
        gl.AttachShader(_program, vertex);
        gl.AttachShader(_program, fragment);
        gl.LinkProgram(_program);
        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);
        gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out var linked);
        if (linked == 0) throw new InvalidOperationException("The square's shaders did not link: " + gl.GetProgramInfoLog(_program));
        _angleLocation = gl.GetUniformLocation(_program, "uAngle");

        // The corners come from gl_VertexID; desktop core profiles still need a vertex array bound to draw.
        _vertexArray = gl.GenVertexArray();
    }

    protected override void RenderOverride(GL gl)
    {
        gl.ClearColor(0.1f, 0.1f, 0.1f, 1f);
        gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);
        gl.UseProgram(_program);
        gl.Uniform1(_angleLocation, (float)(Angle * Math.PI / 180));
        gl.BindVertexArray(_vertexArray);
        gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        // Leave nothing bound, as the add-in asks of every subclass.
        gl.BindVertexArray(0);
        gl.UseProgram(0);
    }

    protected override void OnDestroy(GL gl)
    {
        gl.DeleteVertexArray(_vertexArray);
        gl.DeleteProgram(_program);
    }

    private static uint Compile(GL gl, ShaderType type, string source)
    {
        var shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compiled);
        if (compiled == 0) throw new InvalidOperationException($"The square's {type} did not compile: {gl.GetShaderInfoLog(shader)}");
        return shader;
    }
}
