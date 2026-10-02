using System;
using System.Linq;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.AdvancedTextEdit;

public partial class AdvancedTextEdit
{
	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new AdvancedTextEditAutomationPeer(this);
}

/// <summary>Exposes the editor as an editable text control through standard UI automation.
/// Value replacement is undoable and respects whole-document and section read-only rules.</summary>
public sealed class AdvancedTextEditAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
	/// <summary>Creates a peer for an editor.</summary>
	public AdvancedTextEditAutomationPeer(AdvancedTextEdit owner) : base(owner) { }

	private AdvancedTextEdit Editor => (AdvancedTextEdit)Owner;

	/// <inheritdoc />
	public string Value => Editor.Text;

	/// <summary>True when replacing the whole value would modify a protected section.</summary>
	public bool IsReadOnly
	{
		get
		{
			if (Editor.IsReadOnly || Editor.Document is null) return true;
			var provider = Editor.TextArea.ReadOnlySectionProvider;
			var length = Editor.Document.TextLength;
			return !provider.CanInsert(0) || (length > 0 &&
				provider.GetDeletableSegments(new SimpleSegment(0, length)).Sum(segment => segment.Length) != length);
		}
	}

	/// <inheritdoc />
	public void SetValue(string value)
	{
		ArgumentNullException.ThrowIfNull(value);
		if (!IsEnabled()) throw new ElementNotEnabledException();
		if (IsReadOnly) throw new InvalidOperationException("The editor's value is read-only.");
		Editor.Document.Replace(0, Editor.Document.TextLength, value);
		Editor.Select(value.Length, 0);
		Editor.TextArea.Caret.BringCaretToView();
	}

	/// <inheritdoc />
	protected override string GetClassNameCore() => nameof(AdvancedTextEdit);

	/// <inheritdoc />
	protected override void SetFocusCore() => Editor.Focus(FocusState.Programmatic);

	/// <inheritdoc />
	protected override bool HasKeyboardFocusCore() => Editor.TextArea.IsKeyboardFocused;

	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

	/// <inheritdoc />
	protected override object GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.Value ? this : base.GetPatternCore(patternInterface);
}
