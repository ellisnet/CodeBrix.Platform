================================================================================
AGENT-README: CodeBrix.Platform.WinUI
A Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Platform.WinUI is the WinUI (Windows App SDK) member of the
CodeBrix.Platform "native" package families (siblings: CodeBrix.Platform.WPF and
CodeBrix.Platform.Mobile). It ships the platform-agnostic CodeBrix "Simple"
MVVM toolkit -- SimpleViewModel, SimpleCommand, SimpleDialog, SimpleMessaging,
SimpleServiceResolver, SimpleEnum and SimpleOsInfo -- compiled for WinUI.

NOT UNO-BASED
-------------
These packages are separate from the Uno-derived CodeBrix.Platform packages and
share no code with them. The only shared source is the linked
CodeBrix.Platform.Simple/*.cs files (namespace CodeBrix.Platform.Simple), which
live at src-platforms/Platform.Simple in the repo and are compiled into each
family via file links under a Simple/ folder in the Core project.

COMPILATION CONSTANT
--------------------
WinUI defines the WIN_UI constant (Microsoft.UI.Xaml / Microsoft.UI.Dispatching
code path in the shared SimpleViewModel source). HAS_CODEBRIX (the Uno path) is
intentionally NOT defined. The constant is set for both Debug AND Release.

PACKAGES
--------
CodeBrix.Platform.WinUI.ApacheLicenseForever   (Core) -- MVVM toolkit for WinUI.

INSTALL
-------
dotnet add package CodeBrix.Platform.WinUI.ApacheLicenseForever
Namespace:          CodeBrix.Platform.Simple
Target framework:   net10.0-windows10.0.19041.0 (Windows App SDK)
Dependency:         Microsoft.WindowsAppSDK

STATUS
------
First iteration -- the Core package only. This is a stub AGENT-README, to be
expanded as the family grows.
================================================================================
