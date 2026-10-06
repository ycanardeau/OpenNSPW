// Code from: https://github.com/dotnet/winforms/blob/b666dc7a94d8ac87a7d300cfb4fa86332fb79bae/src/System.Windows.Forms/src/System/Windows/Forms/BindingCompleteEventArgs.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel;

namespace Aigamo.Saruhashi;

/// <summary>
///  Provides information about a Binding Completed event.
/// </summary>
/// <remarks>
///  Constructor for BindingCompleteEventArgs.
/// </remarks>
public class BindingCompleteEventArgs(
	Binding binding,
	BindingCompleteState state,
	BindingCompleteContext context,
	string errorText,
	Exception exception,
	bool cancel
	) : CancelEventArgs(cancel)
{

	/// <summary>
	///  Constructor for BindingCompleteEventArgs.
	/// </summary>
	public BindingCompleteEventArgs(
		Binding binding,
		BindingCompleteState state,
		BindingCompleteContext context,
		string errorText,
		Exception exception
	)
		: this(binding, state, context, errorText, exception, true) { }

	/// <summary>
	///  Constructor for BindingCompleteEventArgs.
	/// </summary>
	public BindingCompleteEventArgs(
		Binding binding,
		BindingCompleteState state,
		BindingCompleteContext context,
		string errorText
	)
		: this(binding, state, context, errorText, null, true) { }

	/// <summary>
	///  Constructor for BindingCompleteEventArgs.
	/// </summary>
	public BindingCompleteEventArgs(
		Binding binding,
		BindingCompleteState state,
		BindingCompleteContext context
	)
		: this(binding, state, context, string.Empty, null, false) { }

	public Binding Binding { get; } = binding;

	public BindingCompleteState BindingCompleteState { get; } = state;

	public BindingCompleteContext BindingCompleteContext { get; } = context;

	public string ErrorText { get; } = errorText ?? string.Empty;

	public Exception Exception { get; } = exception;
}
