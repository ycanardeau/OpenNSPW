// Code from: https://github.com/dotnet/winforms/blob/b666dc7a94d8ac87a7d300cfb4fa86332fb79bae/src/System.Windows.Forms/src/SRCategoryAttribute.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel;

namespace Aigamo.Saruhashi;

[AttributeUsage(AttributeTargets.All)]
internal sealed class SRCategoryAttribute : CategoryAttribute
{
	public SRCategoryAttribute(string category) : base(category)
	{
	}

	protected override string GetLocalizedString(string value)
	{
		return SR.GetResourceString(value);
	}
}
