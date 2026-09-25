#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fence for WPE1-1 item C0e: one date picked in a DatePicker's flyout raises DateChanged once (it was raised twice:
/// once by the Date the pick sets, once more by the flyout's DatePicked handler). The pick comes through a native
/// picker flyout, the way the Android head supplies one (<see cref="ISkiaNativeDatePickerProviderExtension"/>).
/// </summary>
public class DatePickerDateChangedTests
{
	private static readonly DateTimeOffset PickedDate = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

	[Fact]
	public void When_A_Date_Is_Picked_In_A_Templated_DatePicker_Then_DateChanged_Is_Raised_Once()
	{
		//Arrange
		var picker = NativePicker(withTemplate: true);
		var raised = new List<DatePickerValueChangedEventArgs>();
		picker.DateChanged += (_, e) => raised.Add(e);
		var oldDate = picker.Date;

		//Act
		FlyoutOf(picker).Pick(PickedDate, oldDate);

		//Assert
		raised.Should().HaveCount(1);
		raised[0].NewDate.Should().Be(PickedDate);
		picker.Date.Should().Be(PickedDate);
		picker.SelectedDate.Should().Be(PickedDate);
	}

	[Fact]
	public void When_A_Date_Is_Picked_In_A_DatePicker_Whose_Template_Is_Not_Applied_Then_DateChanged_Is_Still_Raised_Once()
	{
		//Arrange
		var picker = NativePicker(withTemplate: false);
		var raised = new List<DatePickerValueChangedEventArgs>();
		picker.DateChanged += (_, e) => raised.Add(e);
		var oldDate = picker.Date;

		//Act
		FlyoutOf(picker).Pick(PickedDate, oldDate);

		//Assert
		raised.Should().HaveCount(1);
		raised[0].NewDate.Should().Be(PickedDate);
		raised[0].OldDate.Should().Be(oldDate);
	}

	[Fact]
	public void When_The_Same_Date_Is_Picked_Again_Then_DateChanged_Is_Not_Raised()
	{
		//Arrange
		var picker = NativePicker(withTemplate: true);
		FlyoutOf(picker).Pick(PickedDate, picker.Date);
		var raised = 0;
		picker.DateChanged += (_, _) => raised++;

		//Act
		FlyoutOf(picker).Pick(PickedDate, PickedDate);

		//Assert
		raised.Should().Be(0);
	}

	private static DatePicker NativePicker(bool withTemplate)
	{
		NativeDatePickerProvider.EnsureRegistered();
		var picker = new DatePicker { UseNativeStyle = true };
		if (withTemplate)
		{
			picker.Template = new ControlTemplate(() => new Grid());
			var host = new Grid { Name = "host" };
			host.Enter(new EnterParams(isLive: true), 0);
			host.Children.Add(picker);
			picker.ApplyTemplate();
		}

		return picker;
	}

	/// <summary>The flyout the picker created (it creates it lazily, the first time it shows a flyout).</summary>
	private static FakeNativeDatePickerFlyout FlyoutOf(DatePicker picker)
		=> (FakeNativeDatePickerFlyout)typeof(DatePicker)
			.GetProperty("_flyout", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(picker)!;

	/// <summary>The stand-in for a platform's native date picker: every DatePicker with UseNativeStyle gets one.</summary>
	private sealed class NativeDatePickerProvider : ISkiaNativeDatePickerProviderExtension
	{
		private static bool _registered;

		internal static void EnsureRegistered()
		{
			if (!_registered)
			{
				ApiExtensibility.Register(typeof(ISkiaNativeDatePickerProviderExtension), _ => new NativeDatePickerProvider());
				_registered = true;
			}
		}

		public DatePickerFlyout CreateNativeDatePickerFlyout() => new FakeNativeDatePickerFlyout();
	}

	/// <summary>A native picker flyout that reports a pick the way a native dialog's result does.</summary>
	private sealed class FakeNativeDatePickerFlyout : DatePickerFlyout
	{
		internal void Pick(DateTimeOffset newDate, DateTimeOffset oldDate)
		{
			Date = newDate;
			_datePicked?.Invoke(this, new DatePickedEventArgs(newDate, oldDate));
		}
	}
}
