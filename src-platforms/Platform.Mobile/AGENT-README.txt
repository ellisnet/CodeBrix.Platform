================================================================================
AGENT-README: CodeBrix.Platform.Mobile
A Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Platform.Mobile is the .NET MAUI member of the CodeBrix.Platform
"native" package families (siblings: CodeBrix.Platform.WinUI and
CodeBrix.Platform.WPF). It ships the platform-agnostic CodeBrix "Simple"
MVVM toolkit -- SimpleViewModel, SimpleCommand, SimpleDialog, SimpleMessaging,
SimpleServiceResolver, SimpleEnum and SimpleOsInfo -- compiled for .NET MAUI.

NOT UNO-BASED
-------------
These packages are separate from the Uno-derived CodeBrix.Platform packages and
share no code with them. The only shared source is the linked
CodeBrix.Platform.Simple/*.cs files (namespace CodeBrix.Platform.Simple), which
live at src-platforms/Platform.Simple in the repo and are compiled into each
family via file links under a Simple/ folder in the Core project.

COMPILATION CONSTANT
--------------------
Mobile defines the MAUI constant (Microsoft.Maui.* code path in the shared
SimpleViewModel source). HAS_CODEBRIX (the Uno path) is intentionally NOT
defined. The constant is set for both Debug AND Release.

PACKAGES
--------
CodeBrix.Platform.Mobile.ApacheLicenseForever   (Core) -- MVVM toolkit for MAUI.

INSTALL
-------
dotnet add package CodeBrix.Platform.Mobile.ApacheLicenseForever
Namespace:          CodeBrix.Platform.Simple
Target frameworks:  net10.0-android; net10.0-ios; net10.0-maccatalyst;
                    net10.0-windows10.0.19041.0 (Windows host)

STATUS
------
First iteration -- the Core package only. This is a stub AGENT-README, to be
expanded as the family grows.
================================================================================
