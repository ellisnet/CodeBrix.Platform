using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using ResourceKey = Microsoft.UI.Xaml.SpecializedResourceDictionary.ResourceKey;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Element-level theming: an element's effective theme is its own <see cref="RequestedTheme"/>, else the nearest
	/// ancestor's, else the application's; theme resources and the default text foreground of the element and its
	/// subtree resolve for that theme, as in WinUI.
	/// </summary>
	public partial class FrameworkElement
	{
		private static readonly ResourceKey s_lightThemeKey = "Light";
		private static readonly ResourceKey s_darkThemeKey = "Dark";

		// Set the first time any element gets a RequestedTheme other than Default. Until then every theme lookup is the
		// application's and none of the element-theme work below runs.
		private static bool s_hasElementThemeOverrides;

		// Bumped on every RequestedTheme change and every parent change once element themes are in use; a cached
		// inherited theme is valid only for the generation it was computed in.
		private static int s_themeTreeGeneration = 1;

		// The element whose RequestedTheme change is being propagated to its subtree, if any.
		private static FrameworkElement s_themePropagationRoot;

		// The XamlRoot content whose RequestedTheme change is setting the application theme, if any.
		private static FrameworkElement s_rootThemeSyncSource;

		[ThreadStatic]
		private static Stack<FrameworkElement> s_themeWalk;

		// Mirror of RequestedTheme, kept by the property-changed callback so tree walks avoid a property read.
		private ElementTheme _requestedThemeOverride;

		private int _inheritedThemeGeneration;
		private ElementTheme _inheritedTheme;

		/// <summary>
		/// True once any element has had a RequestedTheme other than Default.
		/// </summary>
		internal static bool HasElementThemeOverrides => s_hasElementThemeOverrides;

		/// <summary>
		/// True when this element sets its own RequestedTheme (Light or Dark).
		/// </summary>
		internal bool HasRequestedThemeOverride => _requestedThemeOverride != ElementTheme.Default;

		/// <summary>
		/// Invalidates every cached inherited theme. Called when the tree changes shape.
		/// </summary>
		internal static void InvalidateElementThemes()
		{
			if (s_hasElementThemeOverrides)
			{
				unchecked
				{
					s_themeTreeGeneration++;
				}
			}
		}

		/// <summary>
		/// The theme this element's subtree uses because of RequestedTheme: its own, else the nearest ancestor's, else
		/// <see cref="ElementTheme.Default"/> (meaning the application's theme).
		/// </summary>
		internal ElementTheme GetEffectiveThemeOverride()
			=> _requestedThemeOverride != ElementTheme.Default ? _requestedThemeOverride : GetInheritedThemeOverride();

		/// <summary>
		/// The nearest RequestedTheme set on an ancestor (not on this element), or <see cref="ElementTheme.Default"/>.
		/// Cached per tree generation, so a steady-state read is a field comparison.
		/// </summary>
		internal ElementTheme GetInheritedThemeOverride()
		{
			if (!s_hasElementThemeOverrides)
			{
				return ElementTheme.Default;
			}

			var generation = s_themeTreeGeneration;
			if (_inheritedThemeGeneration == generation)
			{
				return _inheritedTheme;
			}

			// Walk up until an ancestor that has its own theme or a valid cache, then fill the caches on the way back.
			var walk = s_themeWalk ??= new Stack<FrameworkElement>();
			var baseCount = walk.Count;
			var result = ElementTheme.Default;
			var current = this;

			while (true)
			{
				walk.Push(current);

				var parent = GetThemeParent(current);
				if (parent is null)
				{
					break;
				}

				if (parent._requestedThemeOverride != ElementTheme.Default)
				{
					result = parent._requestedThemeOverride;
					break;
				}

				if (parent._inheritedThemeGeneration == generation)
				{
					result = parent._inheritedTheme;
					break;
				}

				current = parent;
			}

			while (walk.Count > baseCount)
			{
				var element = walk.Pop();
				element._inheritedTheme = result;
				element._inheritedThemeGeneration = generation;

				if (element._requestedThemeOverride != ElementTheme.Default)
				{
					// Below an element with its own theme, descendants inherit that theme.
					result = element._requestedThemeOverride;
				}
			}

			return _inheritedTheme;
		}

		/// <summary>
		/// The resource theme key ("Light", "Dark", or the application's) this element's theme resources resolve with.
		/// </summary>
		internal ResourceKey GetThemeKeyForResources() => GetEffectiveThemeOverride() switch
		{
			ElementTheme.Light => s_lightThemeKey,
			ElementTheme.Dark => s_darkThemeKey,
			_ => ResourceDictionary.GetApplicationTheme(),
		};

		/// <summary>
		/// Resource theme key for an explicit element theme; Default gives the application's theme key.
		/// </summary>
		internal static ResourceKey GetThemeKeyForResources(ElementTheme theme) => theme switch
		{
			ElementTheme.Light => s_lightThemeKey,
			ElementTheme.Dark => s_darkThemeKey,
			_ => ResourceDictionary.GetApplicationTheme(),
		};

		/// <summary>
		/// Opens a resource theme scope for <paramref name="owner"/>: theme resources resolved until the scope is
		/// disposed use the effective theme of the owner (or of <paramref name="resourceContextProvider"/>, or of the
		/// nearest element above a non-element owner). Does nothing while no element has a RequestedTheme, or when the
		/// owner has no element context (application and dictionary resources keep the surrounding theme).
		/// </summary>
		/// <param name="owner">The object whose resources are being resolved.</param>
		/// <param name="resourceContextProvider">The element that provides the resource context for a non-element owner.</param>
		/// <returns>A scope to dispose when the resolution is done.</returns>
		internal static ResourceDictionary.ThemeScope EnterThemeScope(DependencyObject owner, FrameworkElement resourceContextProvider = null)
		{
			if (!s_hasElementThemeOverrides)
			{
				return default;
			}

			var element = owner as FrameworkElement ?? resourceContextProvider ?? FindThemeContext(owner);
			if (element is null)
			{
				return default;
			}

			return ResourceDictionary.PushThemeScope(element.GetThemeKeyForResources());
		}

		/// <summary>
		/// The element whose theme applies to <paramref name="target"/>: the target itself when it is an element,
		/// else the nearest element among its parents (a Run's TextBlock, a brush's owner).
		/// </summary>
		internal static FrameworkElement FindThemeContext(DependencyObject target)
		{
			var current = target;
			for (var i = 0; current is not null && i < 64; i++)
			{
				if (current is FrameworkElement element)
				{
					return element;
				}

				current = current.GetParent() as DependencyObject;
			}

			return null;
		}

		private static FrameworkElement GetThemeParent(FrameworkElement element)
		{
			// Same chain as resource lookup (FrameworkElement.Parent, which honours a popup child's logical parent);
			// non-element links (for example a collection) are skipped to the next element.
			var current = element.Parent;
			for (var i = 0; current is not null && i < 64; i++)
			{
				if (current is FrameworkElement parent)
				{
					return parent;
				}

				current = current.GetParent() as DependencyObject;
			}

			return null;
		}

		private void OnRequestedThemeChanged(ElementTheme oldValue, ElementTheme newValue)
		{
			var inherited = GetInheritedThemeOverride();
			var appThemeBefore = Application.Current?.ActualElementTheme ?? ElementTheme.Light;
			var actualBefore = oldValue != ElementTheme.Default ? oldValue : (inherited != ElementTheme.Default ? inherited : appThemeBefore);

			_requestedThemeOverride = newValue;
			if (newValue != ElementTheme.Default && !s_hasElementThemeOverrides)
			{
				s_hasElementThemeOverrides = true;
			}

			InvalidateElementThemes();

			// The element the XamlRoot holds sets the application theme (which re-themes the whole tree).
			var previousSyncSource = s_rootThemeSyncSource;
			s_rootThemeSyncSource = this;
			try
			{
				SyncRootRequestedTheme();
			}
			finally
			{
				s_rootThemeSyncSource = previousSyncSource;
			}

			if (!s_hasElementThemeOverrides)
			{
				return;
			}

			((IDependencyObjectStoreProvider)this).Store.OnThemeBoundaryChanged(
				wasBoundary: oldValue != ElementTheme.Default,
				isBoundary: newValue != ElementTheme.Default);

			var appThemeAfter = Application.Current?.ActualElementTheme ?? ElementTheme.Light;
			var actualAfter = newValue != ElementTheme.Default ? newValue : (inherited != ElementTheme.Default ? inherited : appThemeAfter);

			if (actualBefore == actualAfter)
			{
				return;
			}

			if (appThemeBefore != appThemeAfter)
			{
				// The application theme changed and has already re-themed the tree; an element back on Default
				// was notified by that walk.
				if (newValue != ElementTheme.Default)
				{
					ActualThemeChanged?.Invoke(this, null);
				}

				return;
			}

			PropagateElementThemeChange();
			ActualThemeChanged?.Invoke(this, null);
		}

		/// <summary>
		/// Re-resolves theme resources of this element and its subtree after the element's effective theme changed,
		/// skipping descendants that set their own RequestedTheme (their theme did not change).
		/// </summary>
		private void PropagateElementThemeChange()
		{
			var previousRoot = s_themePropagationRoot;
			s_themePropagationRoot = this;
			try
			{
				Application.PropagateResourcesChanged(this, ResourceUpdateReason.ThemeResource, themeRoot: this);
			}
			finally
			{
				s_themePropagationRoot = previousRoot;
			}
		}

		/// <summary>
		/// Whether a theme-resource walk reaching this element should raise <see cref="ActualThemeChanged"/>: the
		/// element follows an inherited theme and that theme is the one that changed.
		/// </summary>
		private bool ShouldRaiseActualThemeChangedFromThemeWalk()
		{
			if (_requestedThemeOverride != ElementTheme.Default || ReferenceEquals(this, s_themePropagationRoot))
			{
				// Its own theme (or the changed element itself, which raises its own event).
				return false;
			}

			if (s_themePropagationRoot is not null)
			{
				// An element's theme walk stops at descendants with their own theme, so everything it reaches changed.
				return true;
			}

			// An application theme change reaches an element under a RequestedTheme override only when that override is
			// the XamlRoot content's own change which is setting the application theme.
			if (GetInheritedThemeOverride() == ElementTheme.Default)
			{
				return true;
			}

			return s_rootThemeSyncSource is not null && ReferenceEquals(FindThemeOverrideSource(), s_rootThemeSyncSource);
		}

		private FrameworkElement FindThemeOverrideSource()
		{
			var current = GetThemeParent(this);
			while (current is not null && current._requestedThemeOverride == ElementTheme.Default)
			{
				current = GetThemeParent(current);
			}

			return current;
		}
	}
}
