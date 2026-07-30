#nullable enable

using System.ComponentModel;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

//was previously: ICSharpCode.AvalonEdit/Utils/PropertyChangedWeakEventManager.cs in the AvalonEdit
//repo (MIT). Upstream attaches DeliverEvent directly to PropertyChanged; because
//PropertyChangedEventHandler is its own delegate type (not EventHandler), this port forwards
//through a private handler method instead. Delivery behavior is unchanged.

/// <summary>
/// Weak event manager for <see cref="INotifyPropertyChanged.PropertyChanged"/>.
/// </summary>
public sealed class PropertyChangedWeakEventManager : WeakEventManagerBase<PropertyChangedWeakEventManager, INotifyPropertyChanged>
{
	/// <inheritdoc/>
	protected override void StartListening(INotifyPropertyChanged source)
	{
		source.PropertyChanged += OnPropertyChanged;
	}

	/// <inheritdoc/>
	protected override void StopListening(INotifyPropertyChanged source)
	{
		source.PropertyChanged -= OnPropertyChanged;
	}

	void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		DeliverEvent(sender, e);
	}
}
