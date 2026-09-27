using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml
{
	// The access-key members of UIElement. They used to be hand edits inside the generated UIElement file
	// (Generated/3.0.0.0/Microsoft.UI.Xaml/UIElement.cs); they live here so that the API sync generator sees them
	// as implemented ("Skipping already declared ...") and a regeneration cannot revert them.
	public partial class UIElement
	{
		/// <summary>
		/// Gets or sets the access key (mnemonic) for this element.
		/// </summary>
		public string AccessKey
		{
			get => (string)this.GetValue(AccessKeyProperty);
			set => this.SetValue(AccessKeyProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="AccessKey"/> dependency property.
		/// </summary>
		/// <remarks>
		/// A change registers the element with (or removes it from) <see cref="AccessKeyManager"/>.
		/// </remarks>
		public static DependencyProperty AccessKeyProperty { get; } =
			DependencyProperty.Register(
				nameof(AccessKey), typeof(string),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					default(string),
					FrameworkPropertyMetadataOptions.Default,
					(s, e) => AccessKeyManager.OnElementAccessKeyChanged(s as UIElement, e.NewValue as string)));

		/// <summary>
		/// Gets or sets a value that specifies whether the access key display is dismissed when an access key is
		/// invoked. The default is <see langword="true"/>.
		/// </summary>
		public bool ExitDisplayModeOnAccessKeyInvoked
		{
			get => (bool)this.GetValue(ExitDisplayModeOnAccessKeyInvokedProperty);
			set => this.SetValue(ExitDisplayModeOnAccessKeyInvokedProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="ExitDisplayModeOnAccessKeyInvoked"/> dependency property (default
		/// <see langword="true"/>, as in WinUI).
		/// </summary>
		public static DependencyProperty ExitDisplayModeOnAccessKeyInvokedProperty { get; } =
			DependencyProperty.Register(
				nameof(ExitDisplayModeOnAccessKeyInvoked), typeof(bool),
				typeof(UIElement),
				new FrameworkPropertyMetadata(true));

		/// <summary>
		/// Gets or sets a source element that provides the access key scope for this element, even if it is not in
		/// the visual tree of the source element.
		/// </summary>
		public DependencyObject AccessKeyScopeOwner
		{
			get => (DependencyObject)this.GetValue(AccessKeyScopeOwnerProperty);
			set => this.SetValue(AccessKeyScopeOwnerProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="AccessKeyScopeOwner"/> dependency property.
		/// </summary>
		public static DependencyProperty AccessKeyScopeOwnerProperty { get; } =
			DependencyProperty.Register(
				nameof(AccessKeyScopeOwner), typeof(DependencyObject),
				typeof(UIElement),
				new FrameworkPropertyMetadata(default(DependencyObject)));

		/// <summary>
		/// Gets or sets a value that indicates whether this element defines its own access key scope.
		/// </summary>
		public bool IsAccessKeyScope
		{
			get => (bool)this.GetValue(IsAccessKeyScopeProperty);
			set => this.SetValue(IsAccessKeyScopeProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="IsAccessKeyScope"/> dependency property.
		/// </summary>
		public static DependencyProperty IsAccessKeyScopeProperty { get; } =
			DependencyProperty.Register(
				nameof(IsAccessKeyScope), typeof(bool),
				typeof(UIElement),
				new FrameworkPropertyMetadata(default(bool)));
	}
}
