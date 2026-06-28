#nullable enable

extern alias __codebrix;

using System;
using CodeBrix.Platform.Extensions;

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator.XamlRedirection //Was previously: Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlMember : IEquatable<XamlMember>
	{
		private string? _name;
		private XamlType? _declaringType;
		private bool _isAttachable;

		private __codebrix::CodeBrix.Platform.Xaml.XamlMember? _codebrixMember;

		public static XamlMember? FromMember(__codebrix::CodeBrix.Platform.Xaml.XamlMember? member) => member != null ? new XamlMember(member) : null;

		public static XamlMember? WithDeclaringType(XamlMember member, XamlType declaringType)
		{
			var newMember = FromMember(member._codebrixMember);
			if (newMember != null)
			{
				newMember._declaringType = declaringType;
			}
			return newMember;
		}

		private XamlMember(__codebrix::CodeBrix.Platform.Xaml.XamlMember member) => this._codebrixMember = member;

		public XamlMember(string name, XamlType declaringType, bool isAttachable)
		{
			this._name = name;
			this._declaringType = declaringType;
			this._isAttachable = isAttachable;
		}

		public string Name
			=> !_name.IsNullOrEmpty() ? _name : _codebrixMember?.Name!;

		public XamlType DeclaringType
			=> _declaringType != null ? _declaringType : XamlType.FromType(_codebrixMember?.DeclaringType);

		public XamlType Type
			=> XamlType.FromType(_codebrixMember?.Type);

		public string? PreferredXamlNamespace
			=> _codebrixMember?.PreferredXamlNamespace;

		public bool IsAttachable
			=> _declaringType != null ? _isAttachable : _codebrixMember?.IsAttachable ?? false;

		public override string ToString() => _codebrixMember?.ToString() ?? "";

		public bool Equals(XamlMember? other)
			=> !_name.IsNullOrEmpty()
			? (
				other != null
				&& _name == other._name
				&& _declaringType == other._declaringType
				&& _isAttachable == other.IsAttachable
			)
			: _codebrixMember?.Equals(other?._codebrixMember) ?? false;

		public override bool Equals(object? other)
			=> other is XamlMember otherMember ? Equals(otherMember) : false;

		public override int GetHashCode()
			=> !_name.IsNullOrEmpty()
			? _name?.GetHashCode() ?? 0
			: _codebrixMember?.GetHashCode() ?? 0;
	}
}
