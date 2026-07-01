// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
// All Rights Reserved
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// https://github.com/AvaloniaUI/Avalonia/blob/5fa3ffaeab7e5cd2662ef02d03e34b9d4cb1a489/src/Avalonia.X11/XkbKeyTransform.cs

using System;
using System.Collections.Generic;
using Windows.System;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland //Was previously: Uno.WinUI.Runtime.Skia.X11
{
	public static class XkbKeyTransform
	{
		// TODO: uncomment and fix as needed
		private static readonly Dictionary<XkbKey, VirtualKey> s_keyFromX11Key = new()
		{
			{ XkbKey.Cancel, VirtualKey.Cancel },
			{ XkbKey.BackSpace, VirtualKey.Back },
			{ XkbKey.Tab, VirtualKey.Tab },
			{ XkbKey.ISO_Left_Tab, VirtualKey.Tab },
			// {XkbKey.Linefeed, VirtualKey.LineFeed},
			{ XkbKey.Clear, VirtualKey.Clear },
			{ XkbKey.Return, VirtualKey.Enter },
			{ XkbKey.KP_Enter, VirtualKey.Enter },
			{ XkbKey.Pause, VirtualKey.Pause },
			{ XkbKey.Caps_Lock, VirtualKey.CapitalLock },
			//{ XkbKey.?, VirtualKey.HangulMode }
			//{ XkbKey.?, VirtualKey.JunjaMode }
			//{ XkbKey.?, VirtualKey.FinalMode }
			//{ XkbKey.?, VirtualKey.KanjiMode }
			{ XkbKey.Escape, VirtualKey.Escape },
			//{ XkbKey.?, VirtualKey.ImeConvert }
			//{ XkbKey.?, VirtualKey.ImeNonConvert }
			//{ XkbKey.?, VirtualKey.ImeAccept }
			//{ XkbKey.?, VirtualKey.ImeModeChange }
			{ XkbKey.space, VirtualKey.Space },
			// {XkbKey.Prior, VirtualKey.Prior},
			// {XkbKey.KP_Prior, VirtualKey.Prior},
			{ XkbKey.Page_Down, VirtualKey.PageDown },
			{ XkbKey.KP_Page_Down, VirtualKey.PageDown },
			{ XkbKey.End, VirtualKey.End },
			{ XkbKey.KP_End, VirtualKey.End },
			{ XkbKey.Home, VirtualKey.Home },
			{ XkbKey.KP_Home, VirtualKey.Home },
			{ XkbKey.Left, VirtualKey.Left },
			{ XkbKey.KP_Left, VirtualKey.Left },
			{ XkbKey.Up, VirtualKey.Up },
			{ XkbKey.KP_Up, VirtualKey.Up },
			{ XkbKey.Right, VirtualKey.Right },
			{ XkbKey.KP_Right, VirtualKey.Right },
			{ XkbKey.Down, VirtualKey.Down },
			{ XkbKey.KP_Down, VirtualKey.Down },
			{ XkbKey.Select, VirtualKey.Select },
			{ XkbKey.Print, VirtualKey.Print },
			{ XkbKey.Execute, VirtualKey.Execute },
			//{ XkbKey.?, VirtualKey.Snapshot }
			{ XkbKey.Insert, VirtualKey.Insert },
			{ XkbKey.KP_Insert, VirtualKey.Insert },
			{ XkbKey.Delete, VirtualKey.Delete },
			{ XkbKey.KP_Delete, VirtualKey.Delete },
			{ XkbKey.Help, VirtualKey.Help },
			{ XkbKey.A, VirtualKey.A },
			{ XkbKey.B, VirtualKey.B },
			{ XkbKey.C, VirtualKey.C },
			{ XkbKey.D, VirtualKey.D },
			{ XkbKey.E, VirtualKey.E },
			{ XkbKey.F, VirtualKey.F },
			{ XkbKey.G, VirtualKey.G },
			{ XkbKey.H, VirtualKey.H },
			{ XkbKey.I, VirtualKey.I },
			{ XkbKey.J, VirtualKey.J },
			{ XkbKey.K, VirtualKey.K },
			{ XkbKey.L, VirtualKey.L },
			{ XkbKey.M, VirtualKey.M },
			{ XkbKey.N, VirtualKey.N },
			{ XkbKey.O, VirtualKey.O },
			{ XkbKey.P, VirtualKey.P },
			{ XkbKey.Q, VirtualKey.Q },
			{ XkbKey.R, VirtualKey.R },
			{ XkbKey.S, VirtualKey.S },
			{ XkbKey.T, VirtualKey.T },
			{ XkbKey.U, VirtualKey.U },
			{ XkbKey.V, VirtualKey.V },
			{ XkbKey.W, VirtualKey.W },
			{ XkbKey.X, VirtualKey.X },
			{ XkbKey.Y, VirtualKey.Y },
			{ XkbKey.Z, VirtualKey.Z },
			{ XkbKey.a, VirtualKey.A },
			{ XkbKey.b, VirtualKey.B },
			{ XkbKey.c, VirtualKey.C },
			{ XkbKey.d, VirtualKey.D },
			{ XkbKey.e, VirtualKey.E },
			{ XkbKey.f, VirtualKey.F },
			{ XkbKey.g, VirtualKey.G },
			{ XkbKey.h, VirtualKey.H },
			{ XkbKey.i, VirtualKey.I },
			{ XkbKey.j, VirtualKey.J },
			{ XkbKey.k, VirtualKey.K },
			{ XkbKey.l, VirtualKey.L },
			{ XkbKey.m, VirtualKey.M },
			{ XkbKey.n, VirtualKey.N },
			{ XkbKey.o, VirtualKey.O },
			{ XkbKey.p, VirtualKey.P },
			{ XkbKey.q, VirtualKey.Q },
			{ XkbKey.r, VirtualKey.R },
			{ XkbKey.s, VirtualKey.S },
			{ XkbKey.t, VirtualKey.T },
			{ XkbKey.u, VirtualKey.U },
			{ XkbKey.v, VirtualKey.V },
			{ XkbKey.w, VirtualKey.W },
			{ XkbKey.x, VirtualKey.X },
			{ XkbKey.y, VirtualKey.Y },
			{ XkbKey.z, VirtualKey.Z },
			{ XkbKey.Super_L, VirtualKey.LeftWindows },
			{ XkbKey.Super_R, VirtualKey.RightWindows },
			{ XkbKey.Menu, VirtualKey.Menu },
			//{ XkbKey.?, VirtualKey.Sleep }
			{ XkbKey.KP_0, VirtualKey.NumberPad0 },
			{ XkbKey.KP_1, VirtualKey.NumberPad1 },
			{ XkbKey.KP_2, VirtualKey.NumberPad2 },
			{ XkbKey.KP_3, VirtualKey.NumberPad3 },
			{ XkbKey.KP_4, VirtualKey.NumberPad4 },
			{ XkbKey.KP_5, VirtualKey.NumberPad5 },
			{ XkbKey.KP_6, VirtualKey.NumberPad6 },
			{ XkbKey.KP_7, VirtualKey.NumberPad7 },
			{ XkbKey.KP_8, VirtualKey.NumberPad8 },
			{ XkbKey.KP_9, VirtualKey.NumberPad9 },
			{ XkbKey.multiply, VirtualKey.Multiply },
			{ XkbKey.KP_Multiply, VirtualKey.Multiply },
			{ XkbKey.KP_Add, VirtualKey.Add },
			//{ XkbKey.?, VirtualKey.Separator }
			{ XkbKey.KP_Subtract, VirtualKey.Subtract },
			{ XkbKey.KP_Decimal, VirtualKey.Decimal },
			{ XkbKey.KP_Divide, VirtualKey.Divide },
			{ XkbKey.F1, VirtualKey.F1 },
			{ XkbKey.F2, VirtualKey.F2 },
			{ XkbKey.F3, VirtualKey.F3 },
			{ XkbKey.F4, VirtualKey.F4 },
			{ XkbKey.F5, VirtualKey.F5 },
			{ XkbKey.F6, VirtualKey.F6 },
			{ XkbKey.F7, VirtualKey.F7 },
			{ XkbKey.F8, VirtualKey.F8 },
			{ XkbKey.F9, VirtualKey.F9 },
			{ XkbKey.F10, VirtualKey.F10 },
			{ XkbKey.F11, VirtualKey.F11 },
			{ XkbKey.F12, VirtualKey.F12 },
			{ XkbKey.L3, VirtualKey.F13 },
			{ XkbKey.F14, VirtualKey.F14 },
			{ XkbKey.L5, VirtualKey.F15 },
			{ XkbKey.F16, VirtualKey.F16 },
			{ XkbKey.F17, VirtualKey.F17 },
			{ XkbKey.L8, VirtualKey.F18 },
			{ XkbKey.L9, VirtualKey.F19 },
			{ XkbKey.L10, VirtualKey.F20 },
			{ XkbKey.R1, VirtualKey.F21 },
			{ XkbKey.R2, VirtualKey.F22 },
			{ XkbKey.F23, VirtualKey.F23 },
			{ XkbKey.R4, VirtualKey.F24 },
			{ XkbKey.Num_Lock, VirtualKey.NumberKeyLock },
			{ XkbKey.Scroll_Lock, VirtualKey.Scroll },
			{ XkbKey.Shift_L, VirtualKey.LeftShift },
			{ XkbKey.Shift_R, VirtualKey.RightShift },
			{ XkbKey.Control_L, VirtualKey.LeftControl },
			{ XkbKey.Control_R, VirtualKey.RightControl },
			{ XkbKey.Alt_L, VirtualKey.LeftMenu },
			{ XkbKey.Alt_R, VirtualKey.RightMenu },
			//{ XkbKey.?, VirtualKey.BrowserBack }
			//{ XkbKey.?, VirtualKey.BrowserForward }
			//{ XkbKey.?, VirtualKey.BrowserRefresh }
			//{ XkbKey.?, VirtualKey.BrowserStop }
			//{ XkbKey.?, VirtualKey.BrowserSearch }
			//{ XkbKey.?, VirtualKey.BrowserFavorites }
			//{ XkbKey.?, VirtualKey.BrowserHome }
			//{ XkbKey.?, VirtualKey.VolumeMute }
			//{ XkbKey.?, VirtualKey.VolumeDown }
			//{ XkbKey.?, VirtualKey.VolumeUp }
			//{ XkbKey.?, VirtualKey.MediaNextTrack }
			//{ XkbKey.?, VirtualKey.MediaPreviousTrack }
			//{ XkbKey.?, VirtualKey.MediaStop }
			//{ XkbKey.?, VirtualKey.MediaPlayPause }
			//{ XkbKey.?, VirtualKey.LaunchMail }
			//{ XkbKey.?, VirtualKey.SelectMedia }
			//{ XkbKey.?, VirtualKey.LaunchApplication1 }
			//{ XkbKey.?, VirtualKey.LaunchApplication2 }
			// {XkbKey.minus, VirtualKey.OemMinus},
			// {XkbKey.underscore, VirtualKey.OemMinus},
			// {XkbKey.plus, VirtualKey.OemPlus},
			// {XkbKey.equal, VirtualKey.OemPlus},
			// {XkbKey.bracketleft, VirtualKey.OemOpenBrackets},
			// {XkbKey.braceleft, VirtualKey.OemOpenBrackets},
			// {XkbKey.bracketright, VirtualKey.OemCloseBrackets},
			// {XkbKey.braceright, VirtualKey.OemCloseBrackets},
			// {XkbKey.backslash, VirtualKey.OemPipe},
			// {XkbKey.bar, VirtualKey.OemPipe},
			// {XkbKey.semicolon, VirtualKey.OemSemicolon},
			// {XkbKey.colon, VirtualKey.OemSemicolon},
			// {XkbKey.apostrophe, VirtualKey.OemQuotes},
			// {XkbKey.quotedbl, VirtualKey.OemQuotes},
			// {XkbKey.comma, VirtualKey.OemComma},
			// {XkbKey.less, VirtualKey.OemComma},
			// {XkbKey.period, VirtualKey.OemPeriod},
			// {XkbKey.greater, VirtualKey.OemPeriod},
			// {XkbKey.slash, VirtualKey.Oem2},
			// {XkbKey.question, VirtualKey.Oem2},
			// {XkbKey.grave, VirtualKey.OemTilde},
			// {XkbKey.asciitilde, VirtualKey.OemTilde},
			{ XkbKey.XK_1, VirtualKey.Number1 },
			{ XkbKey.XK_2, VirtualKey.Number2 },
			{ XkbKey.XK_3, VirtualKey.Number3 },
			{ XkbKey.XK_4, VirtualKey.Number4 },
			{ XkbKey.XK_5, VirtualKey.Number5 },
			{ XkbKey.XK_6, VirtualKey.Number6 },
			{ XkbKey.XK_7, VirtualKey.Number7 },
			{ XkbKey.XK_8, VirtualKey.Number8 },
			{ XkbKey.XK_9, VirtualKey.Number9 },
			{ XkbKey.XK_0, VirtualKey.Number0 },
			//{ XkbKey.?, VirtualKey.AbntC1 }
			//{ XkbKey.?, VirtualKey.AbntC2 }
			//{ XkbKey.?, VirtualKey.Oem8 }
			//{ XkbKey.?, VirtualKey.Oem102 }
			//{ XkbKey.?, VirtualKey.ImeProcessed }
			//{ XkbKey.?, VirtualKey.System }
			//{ XkbKey.?, VirtualKey.OemAttn }
			//{ XkbKey.?, VirtualKey.OemFinish }
			//{ XkbKey.?, VirtualKey.DbeHiragana }
			//{ XkbKey.?, VirtualKey.OemAuto }
			//{ XkbKey.?, VirtualKey.DbeDbcsChar }
			//{ XkbKey.?, VirtualKey.OemBackTab }
			//{ XkbKey.?, VirtualKey.Attn }
			//{ XkbKey.?, VirtualKey.DbeEnterWordRegisterMode }
			//{ XkbKey.?, VirtualKey.DbeEnterImeConfigureMode }
			//{ XkbKey.?, VirtualKey.EraseEof }
			//{ XkbKey.?, VirtualKey.Play }
			//{ XkbKey.?, VirtualKey.Zoom }
			//{ XkbKey.?, VirtualKey.NoName }
			//{ XkbKey.?, VirtualKey.DbeEnterDialogConversionMode }
			//{ XkbKey.?, VirtualKey.OemClear }
			//{ XkbKey.?, VirtualKey.DeadCharProcessed }
		};

		public static VirtualKey VirtualKeyFromKeySym(IntPtr key)
			=> s_keyFromX11Key.TryGetValue((XkbKey)key, out var result) ? result : VirtualKey.None;
	}

}
