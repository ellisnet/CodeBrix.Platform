#nullable enable

using System;
using System.Globalization;
using Windows.Foundation.Metadata;

namespace CodeBrix.Platform.UI.FlexPanel;

//was previously: the FlexBasis struct from src/Core/src/Layouts/FlexEnums.cs plus the string
//grammar of FlexBasisTypeConverter from src/Core/src/Converters/FlexEnumsConverters.cs in
//dotnet/maui. The MAUI TypeConverter is replaced by [CreateFromString], which both the XAML
//source generator and the runtime XAML reader honor, so "Auto", "150" and "30%" all work as
//attribute text for the FlexPanel.Basis attached property.

/// <summary>
/// The initial main-axis size of a child in a <see cref="FlexPanel"/>, set with the
/// <see cref="FlexPanel.BasisProperty"/> attached property. A basis is either
/// <see cref="Auto"/> (the child's measured size is used), an absolute length in
/// device-independent pixels, or a fraction of the panel's main-axis size.
/// </summary>
/// <remarks>
/// In XAML, write <c>FlexPanel.Basis="Auto"</c>, an absolute length such as
/// <c>FlexPanel.Basis="150"</c>, or a percentage such as <c>FlexPanel.Basis="30%"</c>.
/// </remarks>
[CreateFromString(MethodName = "CodeBrix.Platform.UI.FlexPanel.FlexBasis.CreateFromString")]
public struct FlexBasis : IEquatable<FlexBasis>
{
	private readonly bool _isLength;
	private readonly bool _isRelative;

	/// <summary>
	/// The basis length: device-independent pixels when <see cref="IsRelative"/> is false, or a
	/// fraction in [0, 1] of the panel's main-axis size when <see cref="IsRelative"/> is true.
	/// Zero when <see cref="IsAuto"/>.
	/// </summary>
	public float Length { get; }

	/// <summary>
	/// Whether the child's measured main-axis size is used instead of an explicit basis.
	/// </summary>
	public bool IsAuto => !_isLength && !_isRelative;

	/// <summary>
	/// Whether <see cref="Length"/> is a fraction of the panel's main-axis size rather than an
	/// absolute length.
	/// </summary>
	public bool IsRelative => _isRelative;

	/// <summary>
	/// The default basis: the child's measured main-axis size is used.
	/// </summary>
	public static readonly FlexBasis Auto;

	/// <summary>
	/// Initializes a basis with the given length.
	/// </summary>
	/// <param name="length">The basis length. Must not be negative; when
	/// <paramref name="isRelative"/> is true it must be in [0, 1].</param>
	/// <param name="isRelative">True to interpret <paramref name="length"/> as a fraction of the
	/// panel's main-axis size; false for device-independent pixels.</param>
	public FlexBasis(float length, bool isRelative = false)
	{
		if (length < 0)
			throw new ArgumentException("should be a positive value", nameof(length));
		if (isRelative && length > 1)
			throw new ArgumentException("relative length should be in [0, 1]", nameof(length));
		_isLength = !isRelative;
		_isRelative = isRelative;
		Length = length;
	}

	/// <summary>
	/// Converts an absolute device-independent-pixel length to a <see cref="FlexBasis"/>.
	/// </summary>
	/// <param name="length">The absolute basis length.</param>
	public static implicit operator FlexBasis(float length)
		=> new FlexBasis(length);

	/// <summary>
	/// Parses XAML attribute text: <c>"Auto"</c> (case-insensitive), an invariant-culture number
	/// (absolute device-independent pixels), or a number followed by <c>%</c> (percentage of the
	/// panel's main-axis size).
	/// </summary>
	/// <param name="value">The attribute text to parse.</param>
	/// <returns>The parsed basis.</returns>
	public static FlexBasis CreateFromString(string value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		var trimmed = value.Trim();

		if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
			return Auto;

		if (trimmed.EndsWith('%')
			&& float.TryParse(trimmed.AsSpan(0, trimmed.Length - 1), NumberStyles.Number, CultureInfo.InvariantCulture, out var percent))
		{
			return new FlexBasis(percent / 100f, isRelative: true);
		}

		if (float.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var length))
			return new FlexBasis(length);

		throw new FormatException($"Cannot convert \"{value}\" into a {nameof(FlexBasis)}. Use \"Auto\", a number, or a percentage such as \"30%\".");
	}

	/// <summary>
	/// Whether this basis equals <paramref name="other"/> in kind and length.
	/// </summary>
	/// <param name="other">The basis to compare with.</param>
	/// <returns>True when both are the same kind (auto, absolute, or relative) and length.</returns>
	public bool Equals(FlexBasis other)
		=> _isLength == other._isLength && _isRelative == other._isRelative && Length == other.Length;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is FlexBasis other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => _isRelative.GetHashCode() ^ Length.GetHashCode();

	/// <summary>
	/// Equality operator; see <see cref="Equals(FlexBasis)"/>.
	/// </summary>
	public static bool operator ==(FlexBasis left, FlexBasis right) => left.Equals(right);

	/// <summary>
	/// Inequality operator; see <see cref="Equals(FlexBasis)"/>.
	/// </summary>
	public static bool operator !=(FlexBasis left, FlexBasis right) => !(left == right);

	/// <inheritdoc />
	public override string ToString()
		// The percentage is rounded so fractions that are not exactly representable as float
		// (0.3f * 100 is 30.000002) round-trip to the text they were parsed from.
		=> IsAuto ? "Auto"
			: IsRelative ? MathF.Round(Length * 100f, 4).ToString(CultureInfo.InvariantCulture) + "%"
			: Length.ToString(CultureInfo.InvariantCulture);
}
