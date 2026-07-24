# `ApplicationView`

The `ApplicationView` class is needed to retrieve the "visible bounds" of the screen — the region not occupied by system chrome. This concept has been mostly dropped in WinAppSDK (at least for now), so calling `ApplicationView.GetForCurrentView` throws an exception. To keep this functionality internally we use the `ApplicationView.GetForWindowId` method, which returns a Window-specific instance.

For easier access to the `VisibleBounds` and `TrueVisibleBounds` within our codebase, you can utilize the `XamlRoot.VisualTree.VisibleBounds` and `XamlRoot.VisualTree.TrueVisibleBounds` properties. This also works in windowless scenarios like XAML islands, where it just returns the plain island bounds.

# Window initialization

When a `Window` is constructed before the application is initialized (e.g. before `Application.Current` is set), we do not construct its content yet; that initialization is delayed and run when the application is initialized (by calling the `Window.Initialize` method). If the window is created later in the lifetime of the application, we initialize it immediately.
