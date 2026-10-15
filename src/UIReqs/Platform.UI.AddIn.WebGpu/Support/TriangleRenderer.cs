using System;
using System.Text;
using CodeBrix.Platform.UI.WebGpu;
using CodeBrix.WebGpu.Native;
using SkiaSharp;
using Wgpu = CodeBrix.WebGpu.Native.WebGpu;

namespace CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs.Support;

/// <summary>
/// The scenarios' renderer: a real WebGPU pipeline that draws one opaque green triangle over the view's clear colour,
/// and - when asked - an overlay that paints a white square into the view's top-left corner with SkiaSharp. Green and
/// white are colours no clear colour of these scenarios uses, so "the region shows green" can only mean the triangle
/// reached the screen, and "the region shows white" only the overlay.
/// </summary>
public sealed unsafe class TriangleRenderer : IWebGpuRenderer
{
	/// <summary>The side of the overlay's square, in effective pixels.</summary>
	public const float OverlaySquare = 60f;

	private const string Wgsl =
		"@vertex fn vs(@builtin(vertex_index) index: u32) -> @builtin(position) vec4<f32> {\n"
		+ "    var corners = array<vec2<f32>, 3>(vec2<f32>(-0.6, -0.6), vec2<f32>(0.6, -0.6), vec2<f32>(0.0, 0.7));\n"
		+ "    return vec4<f32>(corners[index], 0.0, 1.0);\n"
		+ "}\n"
		+ "@fragment fn fs() -> @location(0) vec4<f32> { return vec4<f32>(0.0, 1.0, 0.0, 1.0); }\n";

	private readonly bool _drawOverlay;
	private ShaderModule _module;
	private RenderPipeline _pipeline;

	/// <summary>Creates the renderer.</summary>
	/// <param name="drawOverlay">True to paint the white overlay square too.</param>
	public TriangleRenderer(bool drawOverlay)
	{
		_drawOverlay = drawOverlay;
	}

	/// <inheritdoc/>
	public void OnDeviceReady(IWebGpuDeviceSource device)
	{
		ArgumentNullException.ThrowIfNull(device);
		var code = Encoding.UTF8.GetBytes(Wgsl);
		fixed (byte* text = code)
		{
			var wgsl = new ShaderSourceWGSL { Code = WgpuString.FromPinned(text, code.Length) };
			wgsl.Chain.SType = ShaderSourceWGSL.StructType;
			var descriptor = new ShaderModuleDescriptor { NextInChain = &wgsl.Chain };
			_module = Wgpu.DeviceCreateShaderModule(device.Device, &descriptor);
		}

		fixed (byte* vs = "vs"u8)
		fixed (byte* fs = "fs"u8)
		{
			var target = new ColorTargetState { Format = TextureFormat.BGRA8Unorm, WriteMask = ColorWriteMask.All };
			var fragment = new FragmentState
			{
				Module = _module,
				EntryPoint = WgpuString.FromPinned(fs, 2),
				TargetCount = 1,
				Targets = &target,
			};
			var descriptor = new RenderPipelineDescriptor
			{
				Vertex = new VertexState { Module = _module, EntryPoint = WgpuString.FromPinned(vs, 2) },
				Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, FrontFace = FrontFace.CCW, CullMode = CullMode.None },
				Multisample = new MultisampleState { Count = 1, Mask = 0xFFFFFFFF },
				Fragment = &fragment,
			};
			_pipeline = Wgpu.DeviceCreateRenderPipeline(device.Device, &descriptor);
		}
	}

	/// <inheritdoc/>
	public void OnRender(WebGpuFrame frame)
	{
		ArgumentNullException.ThrowIfNull(frame);
		if (_pipeline.IsNull)
		{
			return;
		}

		var attachment = new RenderPassColorAttachment
		{
			View = frame.TextureView,
			DepthSlice = Constants.DepthSliceUndefined,
			LoadOp = LoadOp.Load,
			StoreOp = StoreOp.Store,
		};
		var descriptor = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &attachment };
		var pass = Wgpu.CommandEncoderBeginRenderPass(frame.Encoder, &descriptor);
		Wgpu.RenderPassEncoderSetPipeline(pass, _pipeline);
		Wgpu.RenderPassEncoderDraw(pass, 3, 1, 0, 0);
		Wgpu.RenderPassEncoderEnd(pass);
		Wgpu.RenderPassEncoderRelease(pass);
	}

	/// <inheritdoc/>
	public void OnOverlay(SKCanvas canvas, SKSize size)
	{
		ArgumentNullException.ThrowIfNull(canvas);
		if (!_drawOverlay)
		{
			return;
		}

		using var paint = new SKPaint { Color = SKColors.White };
		canvas.DrawRect(new SKRect(0, 0, OverlaySquare, OverlaySquare), paint);
	}

	/// <inheritdoc/>
	public void OnDeviceLost(string reason)
	{
		if (!_pipeline.IsNull)
		{
			Wgpu.RenderPipelineRelease(_pipeline);
			_pipeline = RenderPipeline.Null;
		}

		if (!_module.IsNull)
		{
			Wgpu.ShaderModuleRelease(_module);
			_module = ShaderModule.Null;
		}
	}
}
