using CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;
using Microsoft.UI.Xaml.Media;
using System;

namespace AdvancedTextEditDemo.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit.Sample/MyCompletionData.cs in the AvalonEdit repo (MIT).
//The Image type changed from System.Windows.Media.ImageSource to Microsoft.UI.Xaml.Media.ImageSource.

/// <summary>
/// Implements the editor's <see cref="ICompletionData"/> interface to provide the entries in
/// the completion drop-down.
/// </summary>
public class MyCompletionData : ICompletionData
{
    /// <summary>Creates completion data that inserts the given text.</summary>
    public MyCompletionData(string text)
    {
        Text = text;
    }

    /// <summary>Gets the image shown next to the entry; this sample shows none.</summary>
    public ImageSource Image => null;

    /// <summary>Gets the text used to filter the list and inserted on completion.</summary>
    public string Text { get; }

    /// <summary>
    /// Gets the displayed content. Use this property to show a rich UI element in the
    /// drop-down list instead of plain text.
    /// </summary>
    public object Content => Text;

    /// <summary>Gets the description shown next to the list for the selected entry.</summary>
    public object Description => "Description for " + Text;

    /// <summary>Gets the selection priority; this sample does not prioritize entries.</summary>
    public double Priority => 0;

    /// <summary>Performs the completion by replacing the completion segment with the text.</summary>
    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
