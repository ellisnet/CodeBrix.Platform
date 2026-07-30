#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

//was previously: ICSharpCode.AvalonEdit/Utils/RopeTextReader.cs in the AvalonEdit repo (MIT).
//Transliterated unchanged; Debug.Assert calls encode the leaf-node invariants (currentNode is
//always a leaf with contents between calls) where the upstream code relied on them implicitly.

/// <summary>
/// TextReader implementation that reads text from a rope.
/// </summary>
public sealed class RopeTextReader : TextReader
{
	Stack<RopeNode<char>> stack = new Stack<RopeNode<char>>();
	RopeNode<char>? currentNode;
	int indexInsideNode;

	/// <summary>
	/// Creates a new RopeTextReader.
	/// Internally, this method creates a Clone of the rope; so the text reader will always read through the old
	/// version of the rope if it is modified. <seealso cref="Rope{T}.Clone()"/>
	/// </summary>
	public RopeTextReader(Rope<char> rope)
	{
		if (rope == null)
			throw new ArgumentNullException(nameof(rope));

		// We force the user to iterate through a clone of the rope to keep the API contract of RopeTextReader simple
		// (what happens when a rope is modified while iterating through it?)
		rope.root.Publish();

		// special case for the empty rope:
		// leave currentNode initialized to null (RopeTextReader doesn't support empty nodes)
		if (rope.Length != 0)
		{
			currentNode = rope.root;
			GoToLeftMostLeaf();
		}
	}

	void GoToLeftMostLeaf()
	{
		Debug.Assert(currentNode != null);
		while (currentNode.contents == null)
		{
			if (currentNode.height == 0)
			{
				// this is a function node - move to its contained rope
				currentNode = currentNode.GetContentNode();
				continue;
			}
			Debug.Assert(currentNode.left != null && currentNode.right != null);
			stack.Push(currentNode.right);
			currentNode = currentNode.left;
		}
		Debug.Assert(currentNode.height == 0);
	}

	/// <inheritdoc/>
	public override int Peek()
	{
		if (currentNode == null)
			return -1;
		Debug.Assert(currentNode.contents != null); // currentNode is always a leaf node
		return currentNode.contents[indexInsideNode];
	}

	/// <inheritdoc/>
	public override int Read()
	{
		if (currentNode == null)
			return -1;
		Debug.Assert(currentNode.contents != null); // currentNode is always a leaf node
		char result = currentNode.contents[indexInsideNode++];
		if (indexInsideNode >= currentNode.length)
			GoToNextNode();
		return result;
	}

	void GoToNextNode()
	{
		if (stack.Count == 0)
		{
			currentNode = null;
		}
		else
		{
			indexInsideNode = 0;
			currentNode = stack.Pop();
			GoToLeftMostLeaf();
		}
	}

	/// <inheritdoc/>
	public override int Read(char[] buffer, int index, int count)
	{
		if (currentNode == null)
			return 0;
		Debug.Assert(currentNode.contents != null); // currentNode is always a leaf node
		int amountInCurrentNode = currentNode.length - indexInsideNode;
		if (count < amountInCurrentNode)
		{
			Array.Copy(currentNode.contents, indexInsideNode, buffer, index, count);
			indexInsideNode += count;
			return count;
		}
		else
		{
			// read to end of current node
			Array.Copy(currentNode.contents, indexInsideNode, buffer, index, amountInCurrentNode);
			GoToNextNode();
			return amountInCurrentNode;
		}
	}
}
