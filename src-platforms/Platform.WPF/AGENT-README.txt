================================================================================
AGENT-README: CodeBrix.Platform.WPF
A Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Platform.WPF is the WPF member of the CodeBrix.Platform "native"
package families (siblings: CodeBrix.Platform.WinUI and
CodeBrix.Platform.Mobile). It ships the platform-agnostic CodeBrix "Simple"
MVVM toolkit -- SimpleViewModel, SimpleCommand, SimpleDialog, SimpleMessaging,
SimpleServiceResolver, SimpleEnum and SimpleOsInfo -- compiled for WPF.

NOT UNO-BASED
-------------
These packages are separate from the Uno-derived CodeBrix.Platform packages and
share no code with them. The only shared source is the linked
CodeBrix.Platform.Simple/*.cs files (namespace CodeBrix.Platform.Simple), which
live at src-platforms/Platform.Simple in the repo and are compiled into each
family via file links under a Simple/ folder in the Core project.

COMPILATION CONSTANT
--------------------
WPF uses the SimpleViewModel "#else" branch (System.Windows). It therefore
defines NONE of the platform constants -- WIN_UI, MAUI and HAS_CODEBRIX are all
left undefined, which selects the WPF code path. UseWPF + the -windows target
framework supply System.Windows. The selection applies in Debug AND Release.

PACKAGES
--------
CodeBrix.Platform.WPF.ApacheLicenseForever   (Core) -- MVVM toolkit for WPF.

INSTALL
-------
dotnet add package CodeBrix.Platform.WPF.ApacheLicenseForever
Namespace:          CodeBrix.Platform.Simple
Target framework:   net10.0-windows (UseWPF)

STATUS
------
First iteration -- the Core package only. This is a stub AGENT-README, to be
expanded as the family grows.
================================================================================
