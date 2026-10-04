// Code from: https://github.com/dotnet/winforms/blob/100762af36a2d0caf6adc1e46f6d6b9ca1e6a8c0/src/System.Windows.Forms/src/System/Windows/Forms/VisualStyles/CheckBoxState.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Aigamo.Saruhashi;

public enum CheckBoxState
{
	UncheckedNormal = 1,
	UncheckedHot = 2,
	UncheckedPressed = 3,
	UncheckedDisabled = 4,
	CheckedNormal = 5,
	CheckedHot = 6,
	CheckedPressed = 7,
	CheckedDisabled = 8,
	MixedNormal = 9,
	MixedHot = 10,
	MixedPressed = 11,
	MixedDisabled = 12
}
