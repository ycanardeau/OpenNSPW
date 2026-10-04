// Code from: https://github.com/dotnet/winforms/blob/b666dc7a94d8ac87a7d300cfb4fa86332fb79bae/src/System.Windows.Forms/src/System/Windows/Forms/ConvertEventArgs.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable


namespace Aigamo.Saruhashi;

public class ConvertEventArgs : EventArgs
{
	public ConvertEventArgs(object value, Type desiredType)
	{
		Value = value;
		DesiredType = desiredType;
	}

	public object Value { get; set; }

	public Type DesiredType { get; }
}
