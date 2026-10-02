using System;
using CodeBrix.Platform.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer;

namespace CodeBrix.Platform.PlayTest.Hosting;

// A clipboard belonging only to this application session. Tests never read or overwrite
// the developer's desktop clipboard. The actual application's Clipboard API uses this store.
internal sealed class VirtualClipboard : IClipboardExtension
{
    private DataPackageView _content = new DataPackage().GetView();
    public event EventHandler<object>? ContentChanged;
    public void StartContentChanged() { }
    public void StopContentChanged() { }
    public void Flush() { }
    public DataPackageView GetContent() => _content;
    public void Clear() => SetContent(new DataPackage());
    public void SetContent(DataPackage content)
    {
        _content = content.GetView();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }
}
