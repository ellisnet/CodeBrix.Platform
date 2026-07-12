using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.UI.Converters;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Converters;

[TestClass]
public class Given_CollectionToVisibilityConverter
{
	private readonly CollectionToVisibilityConverter _converter = new();
	private readonly CollectionToVisibilityConverter _inverted = new() { Invert = true };

	[TestMethod]
	public void When_Collection_Has_Items() =>
		_converter.Convert(new List<int> { 1 }, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_Collection_Is_Empty() =>
		_converter.Convert(new List<int>(), typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_Null() =>
		_converter.Convert(null, typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_Array_Has_Items() =>
		_converter.Convert(new[] { "a" }, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_Plain_Enumerable_Has_Items() =>
		_converter.Convert(Enumerable.Range(1, 3).Where(n => n > 2), typeof(Visibility), null, null)
			.Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_Plain_Enumerable_Is_Empty() =>
		_converter.Convert(Enumerable.Empty<int>().Where(n => n > 2), typeof(Visibility), null, null)
			.Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_Plain_Enumerable_Probe_Disposes_Enumerator()
	{
		//Arrange
		var enumerable = new DisposalTrackingEnumerable();

		//Act
		_ = _converter.Convert(enumerable, typeof(Visibility), null, null);

		//Assert
		Assert.IsTrue(enumerable.EnumeratorDisposed);
	}

	[TestMethod]
	public void When_NonEnumerable_Counts_As_Content() =>
		_converter.Convert(42, typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_Empty_Inverted() =>
		_inverted.Convert(Array.Empty<int>(), typeof(Visibility), null, null).Should().Be(Visibility.Visible);

	[TestMethod]
	public void When_Has_Items_Inverted() =>
		_inverted.Convert(new[] { 1 }, typeof(Visibility), null, null).Should().Be(Visibility.Collapsed);

	[TestMethod]
	public void When_ConvertBack_Throws() =>
		Assert.ThrowsExactly<NotSupportedException>(() =>
			_converter.ConvertBack(Visibility.Visible, typeof(IEnumerable), null, null));

	private sealed class DisposalTrackingEnumerable : IEnumerable<int>
	{
		public bool EnumeratorDisposed { get; private set; }

		public IEnumerator<int> GetEnumerator() => new TrackingEnumerator(this);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private sealed class TrackingEnumerator(DisposalTrackingEnumerable owner) : IEnumerator<int>
		{
			public int Current => 1;
			object IEnumerator.Current => Current;
			public bool MoveNext() => true;
			public void Reset() { }
			public void Dispose() => owner.EnumeratorDisposed = true;
		}
	}
}
