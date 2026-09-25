#nullable enable

using System.Diagnostics.CodeAnalysis;
using CodeBrix.Platform.UI.Contracts;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// The element side of the per-control handler seam (<see cref="IElementHandlerFactoryPlatform"/>,
	/// <see cref="IElementHandler"/>): the handler attached to this element while it is live, and the checks the
	/// Core hooks make before calling it.
	/// </summary>
	/// <remarks>
	/// Every hook in Core first reads <see cref="AreHandlersActive"/>, which stays <see langword="false"/> when the
	/// platform registers no handler service (the Skia heads): each hook is then one static field test that is false,
	/// and the element behaves exactly as it does without the seam.
	/// </remarks>
	public partial class UIElement
	{
		/// <summary>
		/// <see langword="true"/> when the platform registered an element handler factory or an overlay presenter.
		/// Resolved once, the first time it is read (see <see cref="RefreshElementHandlerServices"/>).
		/// </summary>
		internal static bool AreHandlersActive = PlatformServices.ResolveElementHandlerServices();

		private ElementHandlerAttachment? _handlerAttachment;

		/// <summary>
		/// Gets the platform handler attached to this element while it is in a live visual tree, or
		/// <see langword="null"/>.
		/// </summary>
		internal IElementHandler? Handler => _handlerAttachment?.Handler;

		/// <summary>
		/// Gets the capabilities of the attached handler, as last read by Core (<see cref="ElementHandlerCapabilities.None"/>
		/// when no handler is attached).
		/// </summary>
		internal ElementHandlerCapabilities HandlerCapabilities => _handlerAttachment?.Capabilities ?? ElementHandlerCapabilities.None;

		/// <summary>
		/// Resolves the element handler services again and updates <see cref="AreHandlersActive"/>. A platform
		/// bootstrap calls this right after it registered its <see cref="IElementHandlerFactoryPlatform"/> and/or
		/// <see cref="IOverlayPresenterPlatform"/> (the first read of <see cref="AreHandlersActive"/> may have
		/// happened before). Elements that are already live keep their current handler state.
		/// </summary>
		internal static void RefreshElementHandlerServices() => AreHandlersActive = PlatformServices.ResolveElementHandlerServices();

		/// <summary>
		/// Gets the attached handler when it has <paramref name="capability"/>.
		/// </summary>
		/// <param name="capability">The capability the caller needs.</param>
		/// <param name="handler">The handler, when the method returns <see langword="true"/>.</param>
		/// <returns><see langword="true"/> when a handler with <paramref name="capability"/> is attached.</returns>
		internal bool TryGetHandlerWith(ElementHandlerCapabilities capability, [NotNullWhen(true)] out IElementHandler? handler)
		{
			if (_handlerAttachment is { } attachment && (attachment.Capabilities & capability) != 0)
			{
				handler = attachment.Handler;
				return true;
			}

			handler = null;
			return false;
		}

		/// <summary>
		/// Gets a value indicating whether the attached handler has <paramref name="capability"/>.
		/// </summary>
		/// <param name="capability">The capability to test.</param>
		/// <returns><see langword="true"/> when a handler with <paramref name="capability"/> is attached.</returns>
		internal bool HasHandlerCapability(ElementHandlerCapabilities capability)
			=> _handlerAttachment is { } attachment && (attachment.Capabilities & capability) != 0;

		/// <summary>
		/// Called by the attached handler after its <see cref="IElementHandler.Capabilities"/> changed: Core reads
		/// them again, releases an applied template when the handler now owns the visuals (a template that is no
		/// longer suppressed is applied by the next measure), re-evaluates the ContentControl content bypass, and
		/// invalidates measure.
		/// </summary>
		internal void NotifyHandlerCapabilitiesChanged()
		{
			if (_handlerAttachment is not { } attachment)
			{
				return;
			}

			var previous = attachment.Capabilities;
			var current = attachment.Handler.Capabilities;
			if (previous == current)
			{
				return;
			}

			attachment.Capabilities = current;

			if ((current & ElementHandlerCapabilities.OwnsVisuals) != 0
				&& (previous & ElementHandlerCapabilities.OwnsVisuals) == 0
				&& this is Control control)
			{
				control.ReleaseTemplateForHandler();
			}

			OnHandlerCapabilitiesChanged(previous, current);

			if (((current ^ previous) & ElementHandlerCapabilities.OwnsVisuals) != 0)
			{
				UpdateHitTestForHandler();
			}

			InvalidateMeasure();
		}

		/// <summary>
		/// Re-evaluates this element's hit-test visibility after the OwnsVisuals state of its handler changed: an element
		/// whose handler owns the visuals is hit-testable (its template, and with it any background, is suppressed) and
		/// <c>HitTest</c> asks the handler (hook H8).
		/// </summary>
		private void UpdateHitTestForHandler()
		{
#if CODEBRIX_HAS_MANAGED_POINTERS
			UpdateHitTest();
#endif
		}

		/// <summary>
		/// Called after the attached handler's capabilities changed (not when the handler connects: a content
		/// control hosts its content when it loads).
		/// </summary>
		/// <param name="previous">The capabilities Core used until now.</param>
		/// <param name="current">The new capabilities.</param>
		internal virtual void OnHandlerCapabilitiesChanged(ElementHandlerCapabilities previous, ElementHandlerCapabilities current)
		{
		}

#if CODEBRIX_HAS_ENHANCED_LIFECYCLE
		/// <summary>
		/// Hook H1: creates and connects this element's handler. Called when the element enters a live visual tree
		/// (right after it became live, before its children enter), only when <see cref="AreHandlersActive"/>.
		/// </summary>
		private void ConnectElementHandler()
		{
			IElementHandler handler;
			ElementHandlerCapabilities capabilities;
			if (_handlerAttachment is { } retained)
			{
				if (!retained.IsDetached)
				{
					return;
				}

				// Hook H16: a handler retained across the last Leave (RetainedAcrossLeave) is connected again; the
				// factory is not asked. Its capabilities are read again, as at a first Connect.
				handler = retained.Handler;
				capabilities = handler.Capabilities;
				retained.IsDetached = false;
				retained.Capabilities = capabilities;
			}
			else
			{
				if (PlatformServices.ElementHandlerFactory?.CreateHandler(this) is not { } created)
				{
					return;
				}

				handler = created;
				capabilities = handler.Capabilities;
			}

			if ((capabilities & ElementHandlerCapabilities.OwnsVisuals) != 0 && this is Control control)
			{
				// A template can be applied before the element enters the tree (an explicit ApplyTemplate call).
				control.ReleaseTemplateForHandler();
			}

			_handlerAttachment ??= new ElementHandlerAttachment(handler, capabilities);
			handler.Connect(this);

			if ((capabilities & ElementHandlerCapabilities.OwnsVisuals) != 0)
			{
				UpdateHitTestForHandler();
			}
		}

		/// <summary>
		/// Hook H2: disconnects and forgets this element's handler. Called when the element leaves the live tree
		/// (after its children left), only when <see cref="AreHandlersActive"/>.
		/// </summary>
		private void DisconnectElementHandler()
		{
			if (_handlerAttachment is { IsDetached: false } attachment)
			{
				attachment.Handler.Disconnect();
				if ((attachment.Capabilities & ElementHandlerCapabilities.RetainedAcrossLeave) != 0)
				{
					// Hook H16: kept for the next Enter (a recycled row keeps its native view).
					attachment.IsDetached = true;
				}
				else
				{
					_handlerAttachment = null;
				}

				if ((attachment.Capabilities & ElementHandlerCapabilities.OwnsVisuals) != 0)
				{
					UpdateHitTestForHandler();
				}
			}
		}
#endif

		/// <summary>
		/// Forgets the handler this element kept after it left the live tree because the handler has
		/// <see cref="ElementHandlerCapabilities.RetainedAcrossLeave"/> (the platform destroyed its native view for
		/// good): the next Enter asks the factory for a new handler. The handler was already disconnected when the
		/// element left; it is not called again. Does nothing while the element is live or has no retained handler.
		/// </summary>
		/// <returns><see langword="true"/> when a retained handler was forgotten.</returns>
		internal bool ReleaseRetainedHandler()
		{
			if (_handlerAttachment is { IsDetached: true })
			{
				_handlerAttachment = null;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets a value indicating whether this element keeps a disconnected handler for its next Enter
		/// (<see cref="ElementHandlerCapabilities.RetainedAcrossLeave"/>).
		/// </summary>
		internal bool HasRetainedHandler => _handlerAttachment is { IsDetached: true };

		/// <summary>
		/// Hook H5 (reporting part): tells the attached handler, once per template, that Core did not materialize
		/// <paramref name="template"/> because the handler owns the visuals.
		/// </summary>
		/// <param name="template">The template that was not applied.</param>
		internal void ReportTemplateSuppressedToHandler(FrameworkTemplate? template)
		{
			if (_handlerAttachment is { } attachment
				&& template is not null
				&& !ReferenceEquals(attachment.ReportedTemplate, template))
			{
				attachment.ReportedTemplate = template;
				attachment.Handler.OnTemplateSuppressed(template);
			}
		}

		/// <summary>
		/// The handler attached to an element, with the state Core keeps for it.
		/// </summary>
		internal sealed class ElementHandlerAttachment
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="ElementHandlerAttachment"/> class.
			/// </summary>
			/// <param name="handler">The attached handler.</param>
			/// <param name="capabilities">Its capabilities, as read when it was attached.</param>
			internal ElementHandlerAttachment(IElementHandler handler, ElementHandlerCapabilities capabilities)
			{
				Handler = handler;
				Capabilities = capabilities;
			}

			/// <summary>Gets the attached handler.</summary>
			internal IElementHandler Handler { get; }

			/// <summary>Gets or sets the handler's capabilities, as last read by Core.</summary>
			internal ElementHandlerCapabilities Capabilities { get; set; }

			/// <summary>Gets or sets the last template reported through <see cref="IElementHandler.OnTemplateSuppressed"/>.</summary>
			internal FrameworkTemplate? ReportedTemplate { get; set; }

			/// <summary>
			/// Gets or sets a value indicating whether the handler was disconnected when the element left the live tree
			/// and is kept for the next Enter (<see cref="ElementHandlerCapabilities.RetainedAcrossLeave"/>).
			/// </summary>
			internal bool IsDetached { get; set; }
		}
	}
}
