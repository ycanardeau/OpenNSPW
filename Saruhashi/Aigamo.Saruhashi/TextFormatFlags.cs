// Code from: https://github.com/dotnet/winforms/blob/bd8c8a08e0d5f461d8a05ebee54f945cf9daf17e/src/System.Windows.Forms/src/System/Windows/Forms/TextFormatFlags.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Aigamo.Saruhashi;

/// <summary>
///  Specifies the display and layout information for text strings.
/// </summary>
/// <remarks>
///  This is a public enum wrapping the internal <see cref="User32.DT"/>.
/// </remarks>
[Flags]
public enum TextFormatFlags
{
	Bottom = 0x00000008,
	EndEllipsis = 0x00008000,
	ExpandTabs = 0x00000040,
	ExternalLeading = 0x00000200,
	Default = 0x00000000,
	HidePrefix = 0x10000000,
	HorizontalCenter = 0x00000001,
	Internal = 0x00001000,

	/// <remarks>
	///  This is the default.
	/// </remarks>
	Left = 0x00000000,
	ModifyString = 0x10000000,
	NoClipping = 0x00000100,
	NoPrefix = 0x00000800,
	NoFullWidthCharacterBreak = 0x00080000,
	PathEllipsis = 0x00004000,
	PrefixOnly = 0x00200000,
	Right = 0x00000002,
	RightToLeft = 0x00020000,
	SingleLine = 0x00000020,
	TextBoxControl = 0x00002000,

	/// <remarks>
	///  This is the default.
	/// </remarks>
	Top = 0x00000000,

	VerticalCenter = 0x00000004,
	WordBreak = 0x00000010,
	WordEllipsis = 0x00040000,

	/// <summary>
	///  The following flags are exclusive of TextRenderer (no Windows native flags)
	///  and apply to methods receiving a Graphics as the IDeviceContext object, and
	///  specify whether to reapply clipping and coordintate transformations to the hdc
	///  obtained from the Graphics object, which returns a clean hdc.
	/// </summary>
	PreserveGraphicsClipping = 0x01000000,
	PreserveGraphicsTranslateTransform = 0x02000000,

	/// <summary>
	///  Adds padding related to the drawing binding box, computed according to the font size.
	///  Match the System.Internal.GDI.TextPaddingOptions.
	/// </summary>
	/// <remarks>
	///  This is the default.
	/// </remarks>
	GlyphOverhangPadding = 0x00000000,
	NoPadding = 0x10000000,
	LeftAndRightPadding = 0x20000000
}
