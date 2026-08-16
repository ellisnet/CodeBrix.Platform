// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

#nullable enable

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using CodeBrix.Platform.ApplicationModel.DataTransfer;
using CodeBrix.Platform.Foundation.Logging;

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// The opt-in clipboard both framebuffer heads register when the host builder's
/// <see cref="FramebufferHostBuilder.EnableSimpleTextClipboard"/> was called: a
/// Last-In-Only-Out, TEXT-ONLY clipboard that exists in the application process
/// alone. Nothing reaches a system clipboard and nothing crosses applications —
/// copy and paste are operations within the application. Copying content
/// without a text representation is refused with an error log, and the
/// previous clipboard text is KEPT — the way real clipboards keep their
/// content when a copy fails.
/// </summary>
internal sealed class SimpleTextClipboardExtension : IClipboardExtension
{
	public static SimpleTextClipboardExtension Instance { get; } = new SimpleTextClipboardExtension();

	private string? _text;

	private SimpleTextClipboardExtension()
	{
	}

	public event EventHandler<object>? ContentChanged;

	public void StartContentChanged()
	{
	}

	public void StopContentChanged()
	{
	}

	public void Clear()
	{
		_text = null;
		ContentChanged?.Invoke(this, EventArgs.Empty);
	}

	public void Flush()
	{
		// Nothing outlives the process, so there is nowhere to flush to.
	}

	public DataPackageView GetContent()
	{
		// Always a real (possibly empty) view: an enabled clipboard holding
		// nothing behaves like an empty clipboard, never like a missing one.
		var package = new DataPackage();
		if (_text is { } text)
		{
			package.SetText(text);
		}
		return package.GetView();
	}

	public void SetContent(DataPackage content)
	{
		var view = content?.GetView();
		if (view == null || !view.Contains(StandardDataFormats.Text))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error(
					"The simple text clipboard supports text only; the copied content was not text and was ignored.");
			}
			return;
		}
		_ = StoreTextAsync(view);
	}

	private async Task StoreTextAsync(DataPackageView view)
	{
		try
		{
			// A package carrying text among other formats stores just the
			// text: this is a text clipboard, and the text is the part our
			// controls put there.
			_text = await view.GetTextAsync();
			ContentChanged?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("The simple text clipboard could not read the copied text: " + e.Message);
			}
		}
	}
}
