using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SharpGLTF.Schema2;
using SkiaSharp;

namespace EmulateFrameBufferDemo.Rendering;

/// <summary>
/// One drawable piece of a loaded glTF model: an interleaved vertex buffer
/// (position, normal, texture coordinate), an index buffer, and the decoded
/// base-colour texture, all in the plain arrays OpenGL wants.
/// </summary>
public sealed class GltfPrimitive
{
    /// <summary>Interleaved vertices: 3 floats position, 3 normal, 2 texture coordinate.</summary>
    public required float[] Vertices { get; init; }

    /// <summary>Triangle indices into <see cref="Vertices"/>.</summary>
    public required uint[] Indices { get; init; }

    /// <summary>The base-colour texture as tightly packed RGBA bytes, or null when untextured.</summary>
    public byte[]? TextureRgba { get; init; }

    /// <summary>The texture's width in pixels; 0 when untextured.</summary>
    public int TextureWidth { get; init; }

    /// <summary>The texture's height in pixels; 0 when untextured.</summary>
    public int TextureHeight { get; init; }

    /// <summary>The material's base-colour factor, applied when there is no texture.</summary>
    public Vector4 BaseColorFactor { get; init; } = Vector4.One;
}

/// <summary>
/// A glTF file loaded into GPU-ready form, normalized so that whatever the
/// model's own units are it arrives centred on the origin and about one unit
/// across — which lets the viewer frame any model without knowing its scale.
/// </summary>
public sealed class GltfModel
{
    GltfModel(IReadOnlyList<GltfPrimitive> primitives)
    {
        Primitives = primitives;
    }

    /// <summary>The model's drawable pieces.</summary>
    public IReadOnlyList<GltfPrimitive> Primitives { get; }

    /// <summary>
    /// Loads the glTF at the given path, flattening its node hierarchy into
    /// world-space primitives and normalizing the result to a unit cube
    /// centred on the origin.
    /// </summary>
    public static GltfModel Load(string filePath)
    {
        var model = ModelRoot.Load(filePath);
        var scene = model.DefaultScene ?? model.LogicalScenes.FirstOrDefault()
            ?? throw new InvalidDataException($"'{filePath}' contains no scene");

        // Collect every primitive in world space, tracking the overall bounds
        // so the model can be recentred and rescaled afterwards.
        var collected = new List<(List<float> Vertices, List<uint> Indices, Material? Material)>();
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var node in scene.VisualChildren.SelectMany(Flatten))
        {
            if (node.Mesh == null)
                continue;
            var transform = node.WorldMatrix;
            var normalTransform = NormalMatrix(transform);

            foreach (var primitive in node.Mesh.Primitives)
            {
                var positions = primitive.GetVertexAccessor("POSITION")?.AsVector3Array();
                if (positions == null)
                    continue;
                var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var uvs = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

                var vertices = new List<float>(positions.Count * 8);
                for (var i = 0; i < positions.Count; i++)
                {
                    var position = Vector3.Transform(positions[i], transform);
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);

                    var normal = normals != null
                        ? Vector3.Normalize(Vector3.TransformNormal(normals[i], normalTransform))
                        : Vector3.UnitY;
                    var uv = uvs != null ? uvs[i] : Vector2.Zero;

                    vertices.Add(position.X); vertices.Add(position.Y); vertices.Add(position.Z);
                    vertices.Add(normal.X); vertices.Add(normal.Y); vertices.Add(normal.Z);
                    vertices.Add(uv.X); vertices.Add(uv.Y);
                }

                var indices = primitive.GetIndices();
                collected.Add((vertices, indices.Select(index => (uint) index).ToList(), primitive.Material));
            }
        }

        if (collected.Count == 0)
            throw new InvalidDataException($"'{filePath}' contains no drawable primitives");

        // Normalize: centre on the origin, scale the longest side to 1.
        var centre = (min + max) * 0.5f;
        var extent = max - min;
        var longest = MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z));
        var scale = longest > 0 ? 1f / longest : 1f;

        var primitives = new List<GltfPrimitive>(collected.Count);
        foreach (var (vertices, indices, material) in collected)
        {
            for (var i = 0; i < vertices.Count; i += 8)
            {
                vertices[i] = (vertices[i] - centre.X) * scale;
                vertices[i + 1] = (vertices[i + 1] - centre.Y) * scale;
                vertices[i + 2] = (vertices[i + 2] - centre.Z) * scale;
            }

            var (rgba, width, height) = DecodeBaseColor(material);
            primitives.Add(new GltfPrimitive
            {
                Vertices = vertices.ToArray(),
                Indices = indices.ToArray(),
                TextureRgba = rgba,
                TextureWidth = width,
                TextureHeight = height,
                BaseColorFactor = BaseColorFactorOf(material),
            });
        }

        return new GltfModel(primitives);
    }

    static IEnumerable<Node> Flatten(Node node)
    {
        yield return node;
        foreach (var child in node.VisualChildren.SelectMany(Flatten))
            yield return child;
    }

    // The inverse-transpose, so non-uniform scaling does not skew normals.
    static Matrix4x4 NormalMatrix(Matrix4x4 transform) =>
        Matrix4x4.Invert(transform, out var inverted)
            ? Matrix4x4.Transpose(inverted)
            : transform;

    static Vector4 BaseColorFactorOf(Material? material)
    {
        var parameter = material?.FindChannel("BaseColor")?.Parameters
            .FirstOrDefault(p => p.Name == "RGBA");
        return parameter?.Value is Vector4 value ? value : Vector4.One;
    }

    // glTF images are JPEG or PNG; Skia decodes both, and premultiplication is
    // irrelevant here because the model's base colour is opaque.
    static (byte[]? Rgba, int Width, int Height) DecodeBaseColor(Material? material)
    {
        var image = material?.FindChannel("BaseColor")?.Texture?.PrimaryImage?.Content;
        if (image is not { } content || content.Content.IsEmpty)
            return (null, 0, 0);

        using var data = SKData.CreateCopy(content.Content.ToArray());
        using var bitmap = SKBitmap.Decode(data);
        if (bitmap == null)
            return (null, 0, 0);

        var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var converted = new SKBitmap(info);
        if (!bitmap.CopyTo(converted, SKColorType.Rgba8888))
            return (null, 0, 0);

        return (converted.Bytes, converted.Width, converted.Height);
    }
}
