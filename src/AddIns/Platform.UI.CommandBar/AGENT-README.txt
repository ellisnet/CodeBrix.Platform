================================================================================
AGENT-README: CodeBrix.Platform.CommandBar
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.CommandBar.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
A tool bar / command bar / button bar family of XAML controls for
CodeBrix.Platform applications, in the desktop tool bar tradition: a bar of
small icon buttons with tooltips, bound to view-model commands, with grouping,
separators, spacers, inline controls and overflow.

    ToolBarTray        several ToolBars in one row, wrapping to the next
    ToolBar            a row (or column) of items; items are any UIElement
    ToolBarGroup       a run of items with its own spacing and separators
    ToolBarSeparator   the divider between two runs
    ToolBarSpacer      fixed empty space, or all the space that is left
    ToolButton         icon and/or text, bound to a command
    ToolToggleButton   the same, with a checked state
    ToolDropDownButton the same, carrying a flyout

Bar-level presentation - icon size, whether items show icon, text or both,
where the text sits, whether tooltips are shown - comes from the four INHERITED
attached properties on ToolBarProperties, so a bar states them once and any
single item can override them.

Icons are ToolIconSource objects: SVG (themed, tintable, re-rasterised at the
display scale) and raster (PNG required; JPEG, BMP, GIF, WebP and ICO come free
through the platform's image decoder). Because ToolIconSource derives from the
framework's IconSource, the same icon also works anywhere the framework takes an
icon source. Target: .NET 10 or later; live on all six heads (Windows Win32-Skia,
Windows WPF-Skia, Linux X11, Linux Wayland, Linux FrameBuffer, macOS).

WHEN TO USE WHICH LAYER
-----------------------
This package is Layer 1: its own vocabulary, MVVM-first, desktop tool bar
semantics, meant for a tool bar written from scratch.

Layer 2 is WinUI CommandBar parity - CommandBar, AppBarButton,
AppBarToggleButton, AppBarSeparator, AppBarElementContainer and their relatives.
Those are FRAMEWORK types in Microsoft.UI.Xaml.Controls, not types this package
declares, and they work on the Skia heads: pasted WinUI CommandBar markup runs
unchanged, with no prefix edits, because the default XAML namespace of a page is
already Microsoft.UI.Xaml.Controls. Reach for them when you are porting existing
WinUI XAML; reach for the types here when you are writing a desktop tool bar of
your own.

The two layers do not overlap and do not compete. Nothing in this package
subclasses, replaces or styles a CommandBar, and a page may hold both - a WinUI
CommandBar across the top and a ToolBarTray of this package's bars below it. The
difference is the vocabulary and the semantics: a CommandBar is a WinUI app bar
(primary commands, a secondary-command overflow behind an ellipsis, a label
position for the whole bar, an open/closed state that overlays the page); a
ToolBar is a desktop tool bar (several bars in a tray, groups and separators,
inline controls, a chevron for what does not fit, per-button label modes, and
Qt's three drop-down popup modes).

The ICON story is shared, and it is the one thing that crosses between them.
ToolIconSource derives from the framework's IconSource, and SvgIcon / RasterIcon
are IconElements, so the same artwork works on a ToolButton and on an
AppBarButton - the element straight into the Icon, or the source through the
framework's IconSourceElement wrapper:

    <AppBarButton Label="Open">
        <AppBarButton.Icon>
            <cb:SvgIcon UriSource="ms-appx:///Icons/open.svg" Size="20" />
        </AppBarButton.Icon>
    </AppBarButton>

    <AppBarButton Label="Save">
        <AppBarButton.Icon>
            <IconSourceElement>
                <cb:SvgIconSource Source="ms-appx:///Icons/save.svg" Size="20" />
            </IconSourceElement>
        </AppBarButton.Icon>
    </AppBarButton>

Reach for SvgIconSource / RasterIconSource wherever an icon is wanted as a
VALUE - ToolButton.Icon, a XamlUICommand's IconSource, a shared resource - and
for the SvgIcon / RasterIcon ELEMENTS wherever the framework wants an element:
an AppBarButton's or MenuFlyoutItem's Icon, or a template. One thing does not
flow automatically: the core framework package NEVER depends on the SVG
renderer, so an SVG icon on a WinUI
AppBarButton works only in an application that references the SVG add-in
(CodeBrix.Platform.Svg.ApacheLicenseForever) or this package, which brings it.
Font, symbol, path and PNG icons on a WinUI AppBarButton need neither.

INSTALLATION
============
    dotnet add package CodeBrix.Platform.CommandBar.ApacheLicenseForever

Reference it from the project that carries your framework package references
(the application's .Core project in the standard layout); the XAML in the
shared .UI project then resolves the cb: namespace.

Dependencies (flow in automatically):
  CodeBrix.Platform.ApacheLicenseForever      the core framework
  CodeBrix.Platform.Svg.ApacheLicenseForever  the SVG renderer behind
                                              SvgImageSource; a HARD dependency,
                                              because SVG icons are not optional

License: Apache-2.0 (the SVG renderer it brings in is MIT).

KEY NAMESPACES / USINGS
=======================
    xmlns:cb="using:CodeBrix.Platform.UI.CommandBar"        (XAML)
    using CodeBrix.Platform.UI.CommandBar;                  (C#)

CORE API REFERENCE
==================

ToolBarProperties  (static class; four INHERITED attached properties)
---------------------------------------------------------------------
These four decide how every item in a bar presents itself. They are INHERITED,
so setting one on a tray, a bar, a group - or on the page - reaches every item
below it, and any single item can set its own and win.

    double        ToolBarProperties.IconSize       default 24
        The icon's edge length in LOGICAL pixels. Artwork is rasterised at this
        size multiplied by the display's rasterization scale, so it is
        pixel-exact rather than drawn once and stretched.
    LabelMode     ToolBarProperties.LabelMode      default IconOnly
        IconOnly | TextOnly | IconAndText. Switchable while the window is open:
        this is the "show button text" preference a desktop application offers.
        An icon-only button with no icon shows its text anyway, and a text-only
        button with no text shows its icon, so a mixed bar never contains blank
        squares.
    LabelPosition ToolBarProperties.LabelPosition  default Right
        Right (beside the icon) or Bottom (under it).
    bool          ToolBarProperties.ShowToolTips   default true
        Whether items show their composed tooltip. A single button overrides it
        with its own ShowToolTip (SINGULAR, nullable) property.

C#: ToolBarProperties.GetIconSize(element) / SetIconSize(element, 32), and the
same Get/Set pair for the other three. In XAML they are written attached:

    <cb:ToolBar cb:ToolBarProperties.IconSize="32"
                cb:ToolBarProperties.LabelMode="IconAndText">

ToolBar re-exposes all four as ordinary-looking properties (IconSize="32"),
but see COMMON PITFALLS: BINDING one of them must be written in the attached
form.

ToolBarTray  (partial class ToolBarTray : Panel)
-------------------------------------------------
Holds several ToolBars in one row and wraps to the next when the row runs out.

    Orientation Orientation      default Horizontal
        The axis the bars run along. A bar that did not state an orientation of
        its own is turned to match, and stays matched if the tray changes later.
    double      ToolBarSpacing   default 8
        The gap between two bars.

A tray gives each bar the width the bar asks for, which is why a filling
ToolBarSpacer inside a bar in a tray has nothing to fill - see PITFALLS.

ToolBar  (partial class ToolBar : ItemsControl)
------------------------------------------------
A row (or column) of items. Items are plain UI ELEMENTS - this package's
buttons, groups, separators and spacers, or any control at all (a ComboBox, a
TextBox, a TextBlock), hosted inline and centred across the bar. A non-element
item is wrapped in a ContentPresenter, so ItemTemplate and ItemTemplateSelector
work for data items.

    Orientation Orientation             default Horizontal
        Sets the matching orientation on every ToolBarGroup and
        ToolBarSeparator it hosts, so those never state it themselves.
    string      Title                   default ""
        The bar's name. It is the bar's accessibility name, the accessibility
        name of the overflow flyout, and the tail of each button's accessible
        name ("Save (Ctrl+S), Main"). It is NOT shown in the visible tooltip.
    double      ItemSpacing             default 4
        The gap between two items. Halved while IsCompact is true.
    bool        IsCompact               default false
        Density. Tightens the spacing and the bar's own padding.
    OverflowMode OverflowMode           default Chevron
        None    - items that do not fit are simply clipped.
        Wrap    - the bar's panel continues on a further line.
        Chevron - the trailing items that do not fit move, in order, into a
                  flyout behind a chevron button, and move back when the space
                  returns. They are the SAME element instances throughout, so
                  bindings, event handlers, toggle state and focus survive.
    bool        SeparatorBetweenGroups  default true
        Puts a separator between two adjacent ToolBarGroups. A separator you
        wrote yourself between them is respected, so this never doubles up.
    bool        HasOverflowItems        read-only
        True while the chevron is shown.
    IReadOnlyList<UIElement> OverflowItems   read-only
        The items currently behind the chevron, in order - the same instances
        the bar holds, not copies. Empty while everything fits.
    bool        ShowOverflow()
        Opens the overflow flyout. Answers false when there is nothing to show
        or the bar is not in a window yet.
    double IconSize / LabelMode LabelMode / LabelPosition LabelPosition /
    bool ShowToolTips
        These ARE the four ToolBarProperties attached properties, re-exposed -
        not copies. Setting one is what the items inherit; leaving one alone
        lets a value set on a tray or a page through untouched.
    const double DefaultItemSpacing = 4
    const string ItemsHostPartName = "PART_ItemsHost"
        The name of the items panel in the bar's template, for a re-template.

ToolBarGroup  (partial class ToolBarGroup : Panel)
---------------------------------------------------
A run of items that belong together, with its own tighter spacing. The bar
treats a group as ONE item, so a group overflows whole.

    Orientation Orientation   set by the bar
    double      Spacing       default 4

ToolBarSeparator  (partial class ToolBarSeparator : Control)
-------------------------------------------------------------
The divider between two runs; vertical in a horizontal bar and the other way
round.

    Orientation Orientation        set by the bar
    double      Thickness          default 1, in DEVICE pixels
    double      LogicalThickness   read-only: Thickness / the display scale
        The line is one device pixel at any scale, and its offset is snapped to
        the device grid, so it never becomes a blurry pixel-and-a-quarter.

ToolBarSpacer  (partial class ToolBarSpacer : FrameworkElement)
----------------------------------------------------------------
Empty space. Give it a Width (or Height in a vertical bar) for a fixed gap, or:

    bool Fill   default false
        Takes everything left over, which pushes what follows to the far end.
        Two filling spacers share what is left equally, which gives a
        left/centre/right bar.

ToolBarPanel  (partial class ToolBarPanel : Panel)
ToolBarOverflowButton  (partial class ToolBarOverflowButton : ButtonBase)
--------------------------------------------------------------------------
The bar's items panel and its chevron. Both are public because the bar's
template is public: an application that re-templates ToolBar has to be able to
name PART_ItemsHost. Neither is meant to be used directly otherwise.

ToolButton  (partial class ToolButton : ButtonBase)
----------------------------------------------------
    ToolIconSource Icon        default null
        The icon. When the button has none and its command is a XamlUICommand,
        the command's IconSource is shown instead - including the framework's
        own symbol, font and path icon sources.
    string      Text           default null
        The label. Used even when it is not drawn: an icon-only button puts it
        in the tooltip and reads it to a screen reader.
    string      Shortcut       default null
        Shortcut text for the tooltip ("Ctrl+S"). Set it only to OVERRIDE what
        is worked out for you from a keyboard accelerator registered on the
        button or carried by a bound XamlUICommand.
    bool?       ShowToolTip    default null
        Null means "whatever the bar says". False silences this button in a bar
        that shows tooltips; true brings it back in a bar that does not.
        Silencing the tooltip does not change what a screen reader reads.
    event TypedEventHandler<ToolButton, ClickWithModifiersEventArgs>
        ClickWithModifiers
        Raised right after the ordinary Click, for every route that clicks the
        button - pointer, keyboard and automation - carrying the modifier keys
        held AT THE CLICK (read then, not remembered from the last key event).
    string      ComposedToolTipText   read-only
        What the tooltip would say, whether or not tooltips are on - useful for
        a status line.
    string      AccessibleName        read-only
        What a screen reader announces, including the bar's Title.

    Template contract (read-only, set by the button, for a re-template):
        string ResolvedText, IconElement IconVisual, double EffectiveIconSize,
        Visibility IconVisibility, Visibility TextVisibility,
        Orientation LabelOrientation.

    Command / CommandParameter / IsEnabled follow the rules in COMMAND BINDING
    below.

ToolToggleButton  (partial class ToolToggleButton : ToolButton)
----------------------------------------------------------------
    bool IsChecked                          default false
    event TypedEventHandler<ToolToggleButton, object> IsCheckedChanged
Any click flips it. Bind it two-way to the view model; this XAML dialect has no
BindsTwoWayByDefault, so write Mode=TwoWay yourself.

ToolDropDownButton  (partial class ToolDropDownButton : ToolButton)
--------------------------------------------------------------------
    FlyoutBase  Flyout            default null
        Any flyout; a MenuFlyout is the usual choice. The SAME MenuFlyout
        instance may be shared with a menu bar or another button - it is a
        reference, not a copy.
    PopupMode   PopupMode         default MenuButton
        MenuButton - the main part runs Command, a separate arrow part opens
                     the flyout (a Save button with Save-as behind its arrow).
        Instant    - the whole button opens the flyout; there is no command.
        Delayed    - a press runs Command on release; a press HELD for
                     PressAndHoldDelay opens the flyout instead, and the
                     release that follows then does nothing.
    TimeSpan    PressAndHoldDelay default 600 ms
    Visibility  ArrowVisibility   read-only; collapsed in Delayed mode
    bool        IsFlyoutOpen      read-only
    void OpenFlyout() / void CloseFlyout()
    event TypedEventHandler<ToolDropDownButton, RoutedEventArgs> FlyoutClosed
        Raised after the flyout closed AND its items' command bindings were
        re-hooked, so the items are usable again by the time you see it.
A menu opens on the PRESS, as every desktop menu does; the release that follows
does nothing.

ICONS
-----
    abstract class ToolIconSource : IconSource
        The base of every icon this package understands. Because it derives
        from the framework's IconSource, one of these also works anywhere the
        framework takes an icon source.

    SvgIconSource : ToolIconSource
        Uri     Source      the artwork, and the LIGHT artwork when Dark is set
        Uri     Dark        optional alternate for the dark theme
        string  Markup      an SVG document written inline, instead of Source
        Brush   Tint        recolours the artwork; SolidColorBrush only
        IconTintMode TintMode   default CurrentColorOnly
        double  Size        overrides the inherited IconSize for this icon

    RasterIconSource : ToolIconSource
        Uri     Source / Uri Dark / Brush Tint / double Size
        PNG is the format to reach for. JPEG, BMP, GIF, WebP and ICO come free
        through the platform's image decoder - there is no format-specific code
        in this package.

    GlyphIconSource : ToolIconSource
        string Glyph, FontFamily FontFamily, double Size - a symbol-font glyph,
        for an application that already ships an icon font.

    SvgIcon : ImageIcon        and     RasterIcon : IconSourceElement
        The same two icons as ELEMENTS, for use where the framework takes an
        IconElement (a MenuFlyoutItem's Icon, an AppBarButton's Icon, or a
        template). Their artwork properties are called UriSource and
        DarkUriSource, because ImageIcon already owns the name Source; the
        rest (Markup, Tint, TintMode, Size) matches the sources. Both expose
        ResolvedUriSource (which artwork won, after the theme had its say),
        EffectiveIconSize and UpdateIcon().

    Markup extensions, for terse XAML:
        {cb:SvgIconSource Source=..., Dark=..., Markup=..., Tint=...,
                          TintMode=..., Size=...}
        {cb:RasterIconSource Source=..., Dark=..., Tint=..., Size=...}
        They are named after what they RETURN - a source - which is also what
        leaves <cb:SvgIcon /> and <cb:RasterIcon /> free to be the elements.
        A relative path is read as ms-appx:///; an absolute URI is taken as
        written. Use the full object syntax when you need to BIND a value.

    enum IconTintMode
        CurrentColorOnly     (default) recolours only artwork that asked for
                             currentColor. Artwork that states a colour keeps
                             it.
        ReplaceBlackAndWhite additionally replaces hard-coded black and white
                             fills and strokes. It does NOT reach a colour
                             written in an inline style= attribute (an inline
                             style outranks a stylesheet), nor an element that
                             states no colour at all and inherits the SVG
                             default of black.
        None                 no recolouring, even with a Tint set.

    static class IconResourceScheme
        const string Scheme = "cb-res"
        Uri  Create(assemblyName, resourceName)   ->  cb-res://MyLib/open.svg
        bool IsResourceUri(uri) / bool TryOpen(uri, out stream)
        void RegisterAssembly(assembly)
        Reads artwork straight out of a library's EMBEDDED RESOURCES, so an
        icon set can ship inside a class library instead of beside the
        application. A resource is found by its exact manifest name, or by an
        unambiguous suffix, so cb-res://MyLib/open.svg finds
        MyLib.Assets.Icons.open.svg. A missing assembly or resource is a
        missing icon, never an exception.

    static class IconRasterCache
        int Count / void Clear()
        One rasterisation per (artwork, theme, size, display scale, tint), held
        weakly and shared by every icon that wants the same thing.

    Scale variants: where open.scale-125.png sits beside open.png, the variant
    matching the display is used - the smallest one that is big enough, else
    the biggest there is. A file named without a qualifier IS the 100% artwork.

ToolTipComposer  (static class)
--------------------------------
The wording rules, exposed so an application can say the same thing elsewhere.

    string Compose(text, shortcutText, description)
        "Save (Ctrl+S)", with the description on a second line when it says
        something the label does not.
    string ComposeAccessibleName(text, shortcutText, description, barTitle)
        The same wording with the bar's title appended, on one line.
    string FormatShortcut(...)
        Formats a keyboard accelerator the way the framework's own menus do:
        Ctrl, Alt, Windows, Shift, then the key.

ClickWithModifiersEventArgs  (sealed class : EventArgs)
--------------------------------------------------------
    VirtualKeyModifiers Modifiers
    bool IsShiftPressed / IsControlPressed / IsAltPressed

AUTOMATION PEERS
----------------
ToolBarAutomationPeer (ToolBar control type, named from Title),
ToolBarTrayAutomationPeer and ToolBarGroupAutomationPeer (Group),
ToolBarSeparatorAutomationPeer (Separator),
ToolButtonAutomationPeer (Button, IInvokeProvider, composed name),
ToolToggleButtonAutomationPeer (IToggleProvider),
ToolDropDownButtonAutomationPeer (SplitButton, IExpandCollapseProvider; it
deliberately offers no Invoke pattern in Instant mode, because an Instant button
has no command to invoke).

COMMAND BINDING
---------------
  - Command may be any ICommand. IsEnabled follows CanExecute(CommandParameter)
    and is re-evaluated on CanExecuteChanged and when CommandParameter changes.
    An explicit IsEnabled="False" always wins.
  - A XamlUICommand or StandardUICommand additionally supplies, ONLY where the
    button did not state its own: Label -> Text, IconSource -> Icon,
    Description -> the tooltip's second line, KeyboardAccelerators -> the
    shortcut text AND accelerators registered on the button itself, so the
    shortcut works while the bar is in the tree, and AccessKey -> AccessKey.
    The button wins, then the command.
  - Checked state belongs to the VIEW: bind ToolToggleButton.IsChecked two-way
    to the view model. There is no "IsChecked from the command" magic.
  - Visibility is the ordinary property. A Collapsed item takes no space and is
    skipped by the overflow partition and by keyboard navigation.

KEYBOARD
--------
Tab moves into the bar and lands on the first item; the next Tab leaves the bar
(the bar is not itself a tab stop). Inside it: Left/Right walk along a
horizontal bar, Up/Down along a vertical one, Home and End go to the ends, and
there is no wrap-around. Enter and Space invoke the focused button. The
drop-down key - Down in a horizontal bar, Right in a vertical one - opens the
focused item's menu, whether that is a ToolDropDownButton's own Flyout or an
attached flyout on any other item. A group is walked through rather than being
a stop of its own; separators and spacers are not focusable; an item that has
moved into the overflow is off the bar's path, and the chevron is the last stop.
Access keys (Alt plus a letter) work on these controls as on any other, through
the framework's own access-key manager.


COMPLETE EXAMPLES
=================

1. A TRAY OF TWO BARS, THE SHAPE A DESKTOP APPLICATION USES
------------------------------------------------------------
    <Page xmlns:cb="using:CodeBrix.Platform.UI.CommandBar" ...>
      <cb:ToolBarTray cb:ToolBarProperties.IconSize="24"
                      cb:ToolBarProperties.LabelMode="IconOnly">

        <cb:ToolBar x:Name="MainBar" Title="Main" OverflowMode="Chevron">
          <cb:ToolBarGroup>
            <cb:ToolButton Command="{Binding NewCommand}" />
            <cb:ToolDropDownButton Command="{Binding OpenCommand}"
                                   PopupMode="MenuButton"
                                   Flyout="{StaticResource RecentFilesFlyout}" />
            <cb:ToolButton Command="{Binding SaveCommand}" />
          </cb:ToolBarGroup>

          <!-- Two adjacent groups get a separator between them automatically. -->
          <cb:ToolBarGroup>
            <cb:ToolDropDownButton Text="Engrave" PopupMode="MenuButton"
                                   Command="{Binding EngraveCommand}"
                                   Icon="{cb:SvgIconSource
                                          Source=Icons/engrave.svg,
                                          Dark=Icons/engrave.dark.svg}">
              <cb:ToolDropDownButton.Flyout>
                <MenuFlyout>
                  <MenuFlyoutItem Text="Preview"
                                  Command="{Binding EngraveModeCommand}"
                                  CommandParameter="Preview" />
                  <MenuFlyoutItem Text="Publish"
                                  Command="{Binding EngraveModeCommand}"
                                  CommandParameter="Publish" />
                </MenuFlyout>
              </cb:ToolDropDownButton.Flyout>
            </cb:ToolDropDownButton>
            <cb:ToolButton Text="Print" Shortcut="Ctrl+P"
                           Command="{Binding PrintCommand}"
                           Icon="{cb:SvgIconSource
                                  Source=Icons/print.svg,
                                  Dark=Icons/print.dark.svg}" />
          </cb:ToolBarGroup>

          <cb:ToolBarSeparator />

          <!-- Any control at all can be an item. -->
          <ComboBox ItemsSource="{Binding Scores}"
                    SelectedItem="{Binding SelectedScore, Mode=TwoWay}"
                    MinWidth="150" VerticalAlignment="Center" />

          <!-- Everything after a filling spacer is pushed to the far end. -->
          <cb:ToolBarSpacer Fill="True" />

          <cb:ToolToggleButton Text="Magnifier"
                               IsChecked="{Binding Magnifier, Mode=TwoWay}"
                               Icon="{cb:RasterIconSource
                                      Source=Icons/magnifier.png,
                                      Tint={ThemeResource TextFillColorPrimaryBrush}}" />
        </cb:ToolBar>

        <cb:ToolBar Title="Music">
          <ComboBox IsEditable="True" MinWidth="90" VerticalAlignment="Center"
                    ItemsSource="{Binding ZoomLevels}" />
          <cb:ToolBarSeparator />
          <cb:ToolButton Text="Previous page" Command="{Binding PreviousPageCommand}" />
          <TextBlock Text="{Binding PageLabel}" VerticalAlignment="Center" Margin="6,0" />
          <cb:ToolButton Text="Next page" Command="{Binding NextPageCommand}" />
        </cb:ToolBar>
      </cb:ToolBarTray>
    </Page>

2. BINDING ONE OF THE FOUR INHERITED PROPERTIES - USE THE ATTACHED FORM
------------------------------------------------------------------------
A "show button text" preference is the usual reason. Write the ATTACHED form,
never the bar's own property name, and read PITFALL 1 for why:

    <cb:ToolBar Title="Main"
        cb:ToolBarProperties.LabelMode="{Binding Verbose,
                                         Converter={StaticResource LabelModeConverter}}">

Setting it in code is the same thing:

    ToolBarProperties.SetLabelMode(MainBar, LabelMode.IconAndText);   // one bar
    ToolBarProperties.SetLabelMode(Tray, LabelMode.IconAndText);      // every bar

3. ONE COMMAND OBJECT DRIVING THE BUTTON, THE MENU ITEM AND THE SHORTCUT
--------------------------------------------------------------------------
    var open = new XamlUICommand
    {
        Label = "Open",
        Description = "Open a score from disk",
        IconSource = new SvgIconSource
        {
            Source = new Uri("ms-appx:///Icons/open.svg"),
            Dark = new Uri("ms-appx:///Icons/open.dark.svg"),
        },
    };
    open.KeyboardAccelerators.Add(new KeyboardAccelerator
    {
        Key = VirtualKey.O,
        Modifiers = VirtualKeyModifiers.Control,
    });
    open.ExecuteRequested += (_, _) => OpenScore();

A button bound to it states nothing at all:

    <cb:ToolButton Command="{Binding OpenCommand}" />

and shows the icon, reads "Open" when its bar is showing text, offers the
tooltip "Open (Ctrl+O)" with the description under it, and answers Ctrl+O while
the bar is in the tree.

4. ICONS
--------
    <!-- A light/dark pair. The dark artwork replaces the light one whenever the
         element's ActualTheme is dark, and swaps back live on a theme change. -->
    <cb:ToolButton Text="New"
                   Icon="{cb:SvgIconSource Source=Icons/new.svg,
                                           Dark=Icons/new.dark.svg}" />

    <!-- One file, any colour: artwork drawn in currentColor takes the tint. -->
    <cb:ToolButton Text="Next page"
                   Icon="{cb:SvgIconSource Source=Icons/chevron.svg,
                                           Tint={ThemeResource AccentFillColorDefaultBrush}}" />

    <!-- A PNG tinted through its ALPHA: the opaque pixels are painted with the
         tint, exactly as BitmapIcon.ShowAsMonochrome does. -->
    <cb:ToolButton Text="Magnifier"
                   Icon="{cb:RasterIconSource Source=Icons/magnifier.png,
                                              Tint=#FF3A6EA5}" />

    <!-- A JPEG, drawn as it was written. A JPEG has no alpha, so a tint would
         paint the whole rectangle - leave it unset. -->
    <cb:ToolButton Text="Score"
                   Icon="{cb:RasterIconSource Source=Icons/score.jpg}" />

    <!-- Artwork embedded in a class library rather than shipped beside the app. -->
    <cb:ToolButton Text="Open"
                   Icon="{cb:SvgIconSource Source=cb-res://MyCompany.Icons/open.svg}" />

    <!-- The same icons as ELEMENTS, where the framework wants an IconElement. -->
    <MenuFlyoutItem Text="Publish">
      <MenuFlyoutItem.Icon>
        <cb:SvgIcon UriSource="ms-appx:///Icons/publish.svg" Size="16" />
      </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>

    <!-- In code, when a value has to be computed or bound. -->
    button.Icon = new SvgIconSource
    {
        Markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'>"
               + "<circle cx='12' cy='12' r='9' fill='currentColor'/></svg>",
        Tint = new SolidColorBrush(Colors.SteelBlue),
    };

5. OVERFLOW
-----------
Nothing to write: OverflowMode is Chevron by default, so a bar that runs out of
room grows a chevron and puts the trailing items behind it, in order. To read or
drive it:

    if (MainBar.HasOverflowItems)
    {
        foreach (var item in MainBar.OverflowItems) { /* the same instances */ }
        MainBar.ShowOverflow();
    }

The bar copies its four presentation settings onto the overflow flyout's panel,
so a button behind the chevron looks like the ones still on the bar - see
PITFALL 5 for why that has to be done rather than inherited.

6. A MENU SHARED BY A TOOL BAR BUTTON AND SOMETHING ELSE
---------------------------------------------------------
    <Page.Resources>
      <MenuFlyout x:Key="RecentFilesFlyout">
        <MenuFlyoutItem Text="bach-invention.ly"
                        Command="{Binding OpenRecentCommand}"
                        CommandParameter="bach-invention.ly" />
      </MenuFlyout>
    </Page.Resources>

    <cb:ToolDropDownButton Text="Open" PopupMode="MenuButton"
                           Command="{Binding OpenCommand}"
                           Flyout="{StaticResource RecentFilesFlyout}" />

The flyout is a reference: the same instance can be the drop-down's menu and a
menu bar's sub-menu at once.

7. A TOOL BAR THAT SAYS NOTHING
--------------------------------
    <cb:ToolBar Title="View" cb:ToolBarProperties.ShowToolTips="False">
      <cb:ToolButton Text="Zoom in"  Command="{Binding ZoomInCommand}" />
      <cb:ToolButton Text="Zoom out" Command="{Binding ZoomOutCommand}" />
      <!-- ... except this one. -->
      <cb:ToolButton Text="Fit page" Command="{Binding FitCommand}" ShowToolTip="True" />
    </cb:ToolBar>

COMMON PITFALLS TO AVOID
========================

 1. DO NOT DECLARE A PROPERTY NAMED IconSize, LabelMode, LabelPosition OR
    ShowToolTips on a control you put in a bar, and BIND those four in the
    ATTACHED form.
    The framework propagates an inherited attached property to a child by
    looking for a property WITH THE SAME NAME on the child's type. A control
    that registers its own dependency property called "IconSize" therefore
    SILENTLY STOPS SEEING the bar's value - it reads the default instead, with
    no error anywhere. That is why ToolBar's own IconSize / LabelMode /
    LabelPosition / ShowToolTips ARE the attached properties rather than copies
    of them, and why the icon elements' size property is called Size.
    Consequence for your XAML: LabelMode="IconAndText" on a bar is fine, but
    BINDING it must be written cb:ToolBarProperties.LabelMode="{Binding ...}".

 2. ShowToolTips (plural, inherited) IS NOT ShowToolTip (singular, per button).
    The plural one is the bar-level setting every item inherits. The singular
    one is the nullable override on a single ToolButton. They are deliberately
    different names, because a per-button property named ShowToolTips would hit
    pitfall 1.

 3. AN ICON SOURCE CAN BE SHARED; AN ICON ELEMENT CANNOT.
    Give the same SvgIconSource to five buttons and each builds its own element
    from it, and all five follow a later change to the source. An SvgIcon or
    RasterIcon is an ELEMENT, and an element has one parent, so it belongs to
    one place in the tree.

 4. TINT IS A SolidColorBrush, AND ON A RASTER ICON IT PAINTS THE ALPHA.
    Only a SolidColorBrush tints; only its colour is used (make an icon
    translucent with the element's Opacity, not with the brush's alpha). On a
    RASTER icon the image's ALPHA CHANNEL becomes a mask that the tint paints -
    which is what makes a monochrome PNG follow a theme, and which turns an
    OPAQUE format such as JPEG into a filled rectangle. Leave Tint unset for
    artwork that carries its own colours.
    On an SVG, the default TintMode recolours only artwork that asked for
    currentColor; ReplaceBlackAndWhite goes further but cannot reach a colour
    written in an inline style= attribute, because an inline style outranks the
    stylesheet the tint is delivered as.

 5. A BUTTON BEHIND THE CHEVRON IS NOT IN THE BAR'S VISUAL TREE.
    The overflow flyout's panel is the flyout's content, not a child of the bar,
    so nothing about the bar reaches it by inheritance. The bar copies its four
    presentation settings across and re-hooks the command bindings of the
    buttons that move there, so you do not have to - but if you WALK UP from a
    button while it is in the overflow, you will not find the bar. Use
    ToolBar.OverflowItems to go the other way.

 6. WRITE Mode=TwoWay ON IsChecked YOURSELF.
    This XAML dialect has no BindsTwoWayByDefault, exactly as WinUI's own
    ToggleButton requires.

 7. A FILLING SPACER IN A BAR INSIDE A TRAY HAS NOTHING TO FILL.
    A tray gives each bar the width the bar asks for, so there is no space left
    over inside the bar. Put the bar somewhere that stretches - a Grid cell, a
    DockPanel - if you want Fill="True" to push something to the far end. This
    is the same rule a star-sized column follows in an auto-sized container.

 8. AN ICON SOURCE IS A VALUE; AN ICON ELEMENT IS A THING - AND THE NAME TELLS
    YOU WHICH ONE YOU JUST WROTE.
    SvgIconSource / RasterIconSource are what you hand to a property that wants
    an icon as a value - ToolButton.Icon, a XamlUICommand's IconSource, a
    shared ResourceDictionary entry - and to AppBarButton.Icon through the
    framework's IconSourceElement. SvgIcon / RasterIcon are IconElements: put
    one straight into an AppBarButton's or a MenuFlyoutItem's Icon, or use one
    in a template. The two carry the same properties, and a source builds the
    matching element when it is asked for one.
    THE ELEMENT FORM IS THE ELEMENT; THE MARKUP FORM IS THE SOURCE:

        <cb:SvgIcon UriSource="ms-appx:///Icons/open.svg" />  -> an SvgIcon
        Icon="{cb:SvgIconSource Source=Icons/open.svg}"       -> an SvgIconSource

    That is why the markup extensions are named after the SOURCES rather than
    after the elements. A prefixed XAML name is looked up with an "Extension"
    suffix FIRST, in either syntax, so an extension named after an element
    would answer for the element form too and hand an IconElement property a
    source - which does not fit, and fails where it is assigned.

 9. SET Size ON AN ICON ELEMENT, NOT Width AND Height.
    Size (or the inherited ToolBarProperties.IconSize) is what the icon is
    RASTERISED at. Setting Width and Height instead stretches a bitmap that was
    rendered for another size, which is exactly the blur this package exists to
    avoid.

10. ToolBar.ItemsPanel IS NOT USED.
    The bar owns its items panel, because overflow means moving the same
    elements between two panels and an items presenter's own child management
    would undo that. Re-template the bar (keeping a ToolBarPanel named
    PART_ItemsHost) to change how items are laid out.

11. A WINDOW MANAGER MAY EAT A MODIFIER BEFORE YOUR APPLICATION SEES IT.
    ClickWithModifiers reports what the platform says is held down at the click.
    Some desktops reserve a modifier for themselves - Cinnamon uses Alt as its
    window-drag modifier by default - and grab that key, so no application sees
    it. Shift and Control are the safe choices for a modifier-aware click.

12. AN ICON WITH NO ARTWORK IS AN EMPTY ICON, NOT AN EXCEPTION.
    A missing file, a missing embedded resource or an unknown assembly leaves
    the icon blank and the application running. Check ResolvedUriSource when an
    icon does not appear.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - It does NOT provide CommandBar, AppBarButton, AppBarToggleButton or
    AppBarSeparator. Those are framework types in Microsoft.UI.Xaml.Controls and
    WinUI CommandBar parity is core work that lands separately from this add-in.
    See WHEN TO USE WHICH LAYER above.
  - It does NOT make SVG icons work on a WinUI AppBarButton by itself: that
    works because the APPLICATION references the SVG add-in, which this package
    brings. The core framework package never depends on the SVG renderer.
  - It is NOT a menu bar. Use the framework's MenuBar for the application menu;
    a MenuFlyout can be shared between the two.
  - It is NOT a ribbon, and there is no tab/group ribbon model.
  - There is NO user customisation UI: no drag-to-reorder, no docking of bars to
    window edges, no "customise tool bar" dialog, and no persistence of any of
    that. Bars are declared in XAML (or built in code) and an application that
    wants them customisable stores its own state and rebuilds them.
  - There is NO per-item overflow priority. The overflow partition is strictly
    "the trailing items that do not fit", in order.
  - There are NO open/close animations on the overflow flyout.
  - It does NOT ship an icon set. The icon TYPES are here; the artwork is yours.
  - There is no named-icon-set resource dictionary sugar yet: build a
    ResourceDictionary of SvgIconSource values yourself if you want icons by
    name.
  - It does NOT rasterise SVG itself. Rendering goes through the platform's
    SvgImageSource, which the SVG add-in supplies; this package composes the
    stylesheet that carries a tint and asks for the right pixel size.

WORKING EXAMPLES ON GITHUB
==========================
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/CommandBarDemo
      The reference application for this package (all six heads).
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.CommandBar.Tests
      The test suite.
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.CommandBar
      The package source - fully XML-documented.

QUICK REFERENCE CARD
====================
namespace CodeBrix.Platform.UI.CommandBar
xmlns:cb="using:CodeBrix.Platform.UI.CommandBar"

static class ToolBarProperties       (inherited attached properties)
    double        IconSize       (24)        icon edge length, logical pixels
    LabelMode     LabelMode      (IconOnly)  IconOnly | TextOnly | IconAndText
    LabelPosition LabelPosition  (Right)     Right | Bottom
    bool          ShowToolTips   (true)      show the composed tooltip

enums: LabelMode, LabelPosition, OverflowMode (None | Wrap | Chevron),
       PopupMode (MenuButton | Instant | Delayed)

abstract class ToolIconSource : IconSource
    SvgIconSource      Source, Dark, Markup, Tint, TintMode, Size
    RasterIconSource   Source, Dark, Tint, Size
    GlyphIconSource    Glyph, FontFamily, Size
    SvgIcon/RasterIcon UriSource, DarkUriSource, (Markup,) Tint, (TintMode,)
                       Size, ResolvedUriSource, EffectiveIconSize, UpdateIcon()
    {cb:SvgIconSource ...}      {cb:RasterIconSource ...}   markup extensions
    IconTintMode       CurrentColorOnly | ReplaceBlackAndWhite | None
    IconResourceScheme cb-res://Assembly/resource.svg
    IconRasterCache    Count, Clear()

class ToolBarTray : Panel            Orientation, ToolBarSpacing
class ToolBar : ItemsControl         Orientation, Title, ItemSpacing, IsCompact,
                                     OverflowMode, SeparatorBetweenGroups,
                                     HasOverflowItems, OverflowItems,
                                     ShowOverflow(), and the four above
class ToolBarGroup : Panel           Orientation, Spacing
class ToolBarSeparator : Control     Orientation, Thickness, LogicalThickness
class ToolBarSpacer : FrameworkElement   Fill (or Width/Height)
class ToolButton : ButtonBase        Icon, Text, Shortcut, ShowToolTip,
                                     Command, CommandParameter,
                                     ClickWithModifiers, ComposedToolTipText,
                                     AccessibleName
class ToolToggleButton : ToolButton  IsChecked, IsCheckedChanged
class ToolDropDownButton : ToolButton  Flyout, PopupMode, PressAndHoldDelay,
                                     ArrowVisibility, IsFlyoutOpen,
                                     OpenFlyout(), CloseFlyout(), FlyoutClosed

static class ToolTipComposer         Compose, ComposeAccessibleName,
                                     FormatShortcut
