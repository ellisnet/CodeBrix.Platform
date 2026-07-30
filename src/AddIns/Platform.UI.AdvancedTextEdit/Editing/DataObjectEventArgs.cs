#nullable enable

using System;

using Windows.ApplicationModel.DataTransfer;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: the WPF attached-event argument classes System.Windows.DataObjectCopyingEventArgs,
//DataObjectSettingDataEventArgs and DataObjectPastingEventArgs (PresentationCore), which this
//framework does not provide. Re-declared here with the members the editor uses: the WPF DataObject
//became Windows.ApplicationModel.DataTransfer.DataPackage (DataPackageView on the paste side), and
//the DataObjectEventArgs base collapsed into each class (CommandCancelled + CancelCommand()).
//The events are raised as plain .NET events on the TextArea (DataObjectCopying/
//DataObjectSettingData/DataObjectPasting) instead of WPF's routed attached events.

/// <summary>
/// Event data for the <see cref="TextArea.DataObjectCopying"/> event: raised when a copy, cut or
/// whole-line copy is about to place a data package on the clipboard. Handlers can inspect or
/// extend the data package, or cancel the command entirely.
/// </summary>
public sealed class DataObjectCopyingEventArgs : EventArgs
{
	/// <summary>
	/// Creates new event data.
	/// </summary>
	/// <param name="dataObject">The data package being assembled.</param>
	/// <param name="isDragDrop">Whether the copy originates from a drag'n'drop operation.</param>
	public DataObjectCopyingEventArgs(DataPackage dataObject, bool isDragDrop)
	{
		if (dataObject == null)
			throw new ArgumentNullException(nameof(dataObject));
		this.DataObject = dataObject;
		this.IsDragDrop = isDragDrop;
	}

	/// <summary>
	/// Gets the data package being assembled for the clipboard.
	/// </summary>
	public DataPackage DataObject { get; }

	/// <summary>
	/// Gets whether the copy originates from a drag'n'drop operation. Always false in this
	/// version (drag'n'drop is not supported).
	/// </summary>
	public bool IsDragDrop { get; }

	/// <summary>
	/// Gets whether the command has been cancelled by a handler (see <see cref="CancelCommand"/>).
	/// </summary>
	public bool CommandCancelled { get; private set; }

	/// <summary>
	/// Cancels the copy command: nothing is placed on the clipboard.
	/// </summary>
	public void CancelCommand()
	{
		CommandCancelled = true;
	}
}

/// <summary>
/// Event data for the <see cref="TextArea.DataObjectSettingData"/> event: raised once per data
/// format before the editor adds that format to a copy data package. Handlers can cancel to keep
/// the format out of the package.
/// </summary>
public sealed class DataObjectSettingDataEventArgs : EventArgs
{
	/// <summary>
	/// Creates new event data.
	/// </summary>
	/// <param name="dataObject">The data package being assembled.</param>
	/// <param name="format">The data format about to be added.</param>
	public DataObjectSettingDataEventArgs(DataPackage dataObject, string format)
	{
		if (dataObject == null)
			throw new ArgumentNullException(nameof(dataObject));
		if (format == null)
			throw new ArgumentNullException(nameof(format));
		this.DataObject = dataObject;
		this.Format = format;
	}

	/// <summary>
	/// Gets the data package being assembled.
	/// </summary>
	public DataPackage DataObject { get; }

	/// <summary>
	/// Gets the data format about to be added to the package.
	/// </summary>
	public string Format { get; }

	/// <summary>
	/// Gets whether the format has been vetoed by a handler (see <see cref="CancelCommand"/>).
	/// </summary>
	public bool CommandCancelled { get; private set; }

	/// <summary>
	/// Vetoes this data format: it is not added to the package. The copy itself continues.
	/// </summary>
	public void CancelCommand()
	{
		CommandCancelled = true;
	}
}

/// <summary>
/// Event data for the <see cref="TextArea.DataObjectPasting"/> event: raised when clipboard
/// content is about to be pasted. Handlers can redirect the format that will be applied or
/// cancel the paste entirely.
/// </summary>
public sealed class DataObjectPastingEventArgs : EventArgs
{
	/// <summary>
	/// Creates new event data.
	/// </summary>
	/// <param name="dataObject">A read-only view of the clipboard content.</param>
	/// <param name="isDragDrop">Whether the paste originates from a drag'n'drop operation.</param>
	/// <param name="formatToApply">The data format the editor intends to read.</param>
	public DataObjectPastingEventArgs(DataPackageView dataObject, bool isDragDrop, string formatToApply)
	{
		if (dataObject == null)
			throw new ArgumentNullException(nameof(dataObject));
		if (formatToApply == null)
			throw new ArgumentNullException(nameof(formatToApply));
		this.DataObject = dataObject;
		this.IsDragDrop = isDragDrop;
		this.FormatToApply = formatToApply;
	}

	/// <summary>
	/// Gets the read-only view of the clipboard content being pasted.
	/// </summary>
	public DataPackageView DataObject { get; }

	/// <summary>
	/// Gets whether the paste originates from a drag'n'drop operation. Always false in this
	/// version (drag'n'drop is not supported).
	/// </summary>
	public bool IsDragDrop { get; }

	/// <summary>
	/// Gets/Sets the data format the editor will read from the clipboard content. Handlers may
	/// change this to another format present in <see cref="DataObject"/>.
	/// </summary>
	public string FormatToApply { get; set; }

	/// <summary>
	/// Gets whether the paste has been cancelled by a handler (see <see cref="CancelCommand"/>).
	/// </summary>
	public bool CommandCancelled { get; private set; }

	/// <summary>
	/// Cancels the paste command: no text is inserted.
	/// </summary>
	public void CancelCommand()
	{
		CommandCancelled = true;
	}
}
