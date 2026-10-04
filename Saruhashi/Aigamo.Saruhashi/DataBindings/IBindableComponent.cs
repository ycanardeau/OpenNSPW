// Code from: https://github.com/dotnet/winforms/blob/b666dc7a94d8ac87a7d300cfb4fa86332fb79bae/src/System.Windows.Forms/src/System/Windows/Forms/IBindableComponent.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel;

namespace Aigamo.Saruhashi;

public interface IBindableComponent : IComponent
{
	ControlBindingsCollection DataBindings { get; }
	BindingContext BindingContext { get; set; }
}
