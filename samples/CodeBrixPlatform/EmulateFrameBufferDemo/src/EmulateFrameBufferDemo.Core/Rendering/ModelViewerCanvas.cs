using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using CodeBrix.Platform.OpenGL;
using CodeBrix.Platform.WinUI.Graphics3DGL;

namespace EmulateFrameBufferDemo.Rendering;

/// <summary>
/// The 3D pane: a bundled glTF model on a turntable, drawn with OpenGL through
/// the Graphics3DGL add-in's <see cref="GLCanvasElement"/>. Deliberately small —
/// one textured, diffusely lit mesh — because its job in this sample is to prove
/// that GL rendering behaves the same in the emulator as on a real head, not to
/// be a full model viewer.
/// </summary>
public sealed class ModelViewerCanvas : GLCanvasElement
{
    const string VertexShaderSource = """
        #version 300 es
        precision highp float;
        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoord;
        uniform mat4 uModelViewProjection;
        uniform mat4 uModel;
        out vec3 vNormal;
        out vec2 vTexCoord;
        void main()
        {
            gl_Position = uModelViewProjection * vec4(aPosition, 1.0);
            vNormal = mat3(uModel) * aNormal;
            vTexCoord = aTexCoord;
        }
        """;

    const string FragmentShaderSource = """
        #version 300 es
        precision highp float;
        in vec3 vNormal;
        in vec2 vTexCoord;
        uniform sampler2D uBaseColorTexture;
        uniform vec4 uBaseColorFactor;
        uniform int uHasTexture;
        uniform vec3 uLightDirection;
        out vec4 fragColor;
        void main()
        {
            vec4 baseColor = uHasTexture == 1
                ? texture(uBaseColorTexture, vTexCoord) * uBaseColorFactor
                : uBaseColorFactor;
            vec3 normal = normalize(vNormal);
            // Key light plus a dim fill from the opposite side, so the shaded
            // half never goes fully black.
            float key = max(dot(normal, normalize(uLightDirection)), 0.0);
            float fill = max(dot(normal, normalize(-uLightDirection)), 0.0) * 0.25;
            float ambient = 0.28;
            fragColor = vec4(baseColor.rgb * (ambient + key * 0.85 + fill), baseColor.a);
        }
        """;

    sealed class Piece
    {
        public uint VertexArray;
        public uint VertexBuffer;
        public uint IndexBuffer;
        public uint Texture;
        public uint IndexCount;
        public Vector4 BaseColorFactor;
        public bool HasTexture;
    }

    readonly List<Piece> pieces = new();
    readonly Stopwatch clock = Stopwatch.StartNew();
    readonly string modelPath;

    uint program;
    int modelViewProjectionLocation;
    int modelLocation;
    int baseColorTextureLocation;
    int baseColorFactorLocation;
    int hasTextureLocation;
    int lightDirectionLocation;
    string? loadError;

    /// <summary>Creates the pane for the sample's bundled model.</summary>
    public ModelViewerCanvas()
        : this(Path.Combine(AppContext.BaseDirectory,
            "Assets", "Models", "food_apple_01", "food_apple_01_2k.gltf"))
    {
    }

    /// <summary>Creates the pane for the glTF file at the given path.</summary>
    // The base class wants a function returning the owning Window; that is a
    // WinUI-only concern, and null is the documented value on CodeBrix.Platform.
    public ModelViewerCanvas(string modelPath)
        : base(null)
    {
        this.modelPath = modelPath;
    }

    /// <summary>The turntable angle in radians; reset by the Reset Model button.</summary>
    public float Rotation { get; private set; }

    /// <summary>Whether the turntable is turning.</summary>
    public bool IsSpinning { get; private set; } = true;

    /// <summary>Starts or stops the turntable.</summary>
    public void ToggleSpin() => IsSpinning = !IsSpinning;

    /// <summary>Returns the model to its starting angle and resumes spinning.</summary>
    public void ResetView()
    {
        Rotation = 0f;
        IsSpinning = true;
        clock.Restart();
    }

    /// <inheritdoc/>
    protected override unsafe void Init(GL gl)
    {
        program = BuildProgram(gl);
        modelViewProjectionLocation = gl.GetUniformLocation(program, "uModelViewProjection");
        modelLocation = gl.GetUniformLocation(program, "uModel");
        baseColorTextureLocation = gl.GetUniformLocation(program, "uBaseColorTexture");
        baseColorFactorLocation = gl.GetUniformLocation(program, "uBaseColorFactor");
        hasTextureLocation = gl.GetUniformLocation(program, "uHasTexture");
        lightDirectionLocation = gl.GetUniformLocation(program, "uLightDirection");

        GltfModel model;
        try
        {
            model = GltfModel.Load(modelPath);
        }
        catch (Exception ex)
        {
            // A missing or broken asset must not take the whole sample down;
            // the pane just stays empty.
            loadError = ex.Message;
            Debug.WriteLine($"Model load failed: {ex}");
            return;
        }

        foreach (var primitive in model.Primitives)
            pieces.Add(Upload(gl, primitive));
    }

    /// <inheritdoc/>
    protected override unsafe void RenderOverride(GL gl)
    {
        gl.ClearColor(0.09f, 0.10f, 0.13f, 1f);
        gl.Clear((uint) (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        gl.Enable(EnableCap.DepthTest);

        if (pieces.Count == 0 || loadError != null)
            return;

        if (IsSpinning)
            Rotation = (float) (clock.Elapsed.TotalSeconds * 0.6);

        var width = (float) Math.Max(1, RenderSize.Width);
        var height = (float) Math.Max(1, RenderSize.Height);

        // The model is normalized to a unit cube, so a fixed camera frames any
        // model the same way.
        // Tipped forward so the viewer looks slightly down onto the model — for
        // the bundled apple that keeps the stem in view all the way round.
        var world = Matrix4x4.CreateRotationY(Rotation) * Matrix4x4.CreateRotationX(0.35f);
        var view = Matrix4x4.CreateLookAt(new Vector3(0, 0, 2.1f), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4f, width / height, 0.05f, 50f);
        var modelViewProjection = world * view * projection;

        gl.UseProgram(program);
        gl.UniformMatrix4(modelViewProjectionLocation, 1, false, (float*) &modelViewProjection);
        gl.UniformMatrix4(modelLocation, 1, false, (float*) &world);
        gl.Uniform3(lightDirectionLocation, 0.45f, 0.75f, 0.9f);

        foreach (var piece in pieces)
        {
            gl.Uniform4(baseColorFactorLocation, piece.BaseColorFactor.X, piece.BaseColorFactor.Y,
                piece.BaseColorFactor.Z, piece.BaseColorFactor.W);
            gl.Uniform1(hasTextureLocation, piece.HasTexture ? 1 : 0);
            if (piece.HasTexture)
            {
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, piece.Texture);
                gl.Uniform1(baseColorTextureLocation, 0);
            }

            gl.BindVertexArray(piece.VertexArray);
            gl.DrawElements(PrimitiveType.Triangles, piece.IndexCount,
                DrawElementsType.UnsignedInt, (void*) 0);
        }
        gl.BindVertexArray(0);

        // Keep the turntable turning.
        if (IsSpinning)
            Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnDestroy(GL gl)
    {
        foreach (var piece in pieces)
        {
            gl.DeleteVertexArray(piece.VertexArray);
            gl.DeleteBuffer(piece.VertexBuffer);
            gl.DeleteBuffer(piece.IndexBuffer);
            if (piece.HasTexture)
                gl.DeleteTexture(piece.Texture);
        }
        pieces.Clear();
        if (program != 0)
            gl.DeleteProgram(program);
        program = 0;
    }

    unsafe Piece Upload(GL gl, GltfPrimitive primitive)
    {
        var piece = new Piece
        {
            IndexCount = (uint) primitive.Indices.Length,
            BaseColorFactor = primitive.BaseColorFactor,
            HasTexture = primitive.TextureRgba != null,
        };

        piece.VertexArray = gl.GenVertexArray();
        gl.BindVertexArray(piece.VertexArray);

        piece.VertexBuffer = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, piece.VertexBuffer);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, primitive.Vertices, BufferUsageARB.StaticDraw);

        // Interleaved: position (3), normal (3), texture coordinate (2).
        const uint stride = 8 * sizeof(float);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*) 0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*) (3 * sizeof(float)));
        gl.EnableVertexAttribArray(2);
        gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, (void*) (6 * sizeof(float)));

        piece.IndexBuffer = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, piece.IndexBuffer);
        gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, primitive.Indices, BufferUsageARB.StaticDraw);

        gl.BindVertexArray(0);

        if (primitive.TextureRgba is { } rgba)
        {
            piece.Texture = gl.GenTexture();
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, piece.Texture);
            gl.TexImage2D<byte>(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba,
                (uint) primitive.TextureWidth, (uint) primitive.TextureHeight, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, rgba);
            gl.GenerateMipmap(TextureTarget.Texture2D);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.LinearMipmapLinear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int) TextureWrapMode.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int) TextureWrapMode.Repeat);
            gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        return piece;
    }

    static uint BuildProgram(GL gl)
    {
        var vertex = Compile(gl, ShaderType.VertexShader, VertexShaderSource);
        var fragment = Compile(gl, ShaderType.FragmentShader, FragmentShaderSource);

        var handle = gl.CreateProgram();
        gl.AttachShader(handle, vertex);
        gl.AttachShader(handle, fragment);
        gl.LinkProgram(handle);
        gl.GetProgram(handle, ProgramPropertyARB.LinkStatus, out var linked);
        if (linked == 0)
            throw new InvalidOperationException($"Shader link failed: {gl.GetProgramInfoLog(handle)}");

        gl.DetachShader(handle, vertex);
        gl.DetachShader(handle, fragment);
        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);
        return handle;
    }

    static uint Compile(GL gl, ShaderType type, string source)
    {
        var shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compiled);
        if (compiled == 0)
            throw new InvalidOperationException($"{type} compile failed: {gl.GetShaderInfoLog(shader)}");
        return shader;
    }
}
