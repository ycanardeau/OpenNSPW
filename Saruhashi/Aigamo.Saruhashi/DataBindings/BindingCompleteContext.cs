// Code from: https://github.com/dotnet/winforms/blob/100762af36a2d0caf6adc1e46f6d6b9ca1e6a8c0/src/System.Windows.Forms/src/System/Windows/Forms/BindingCompleteContext.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Aigamo.Saruhashi;

/// <summary>
///  Indicates the direction of a binding operation.
/// </summary>
public enum BindingCompleteContext
{
    /// <summary>
    ///  Control value is being updated from data source value.
    /// </summary>
    ControlUpdate = 0,

    /// <summary>
    ///  Data source value is being updated from control value.
    /// </summary>
    DataSourceUpdate = 1,
}
