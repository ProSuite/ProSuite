using System;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class BooleanValueBlazor : IDisposable
{
	[Parameter]
	public ScalarTestParameterValueViewModel ViewModel { get; set; }

	[CanBeNull]
	public object Value
	{
		get => ViewModel.Value;
		set => ViewModel.Value = value;
	}

	public bool BoolValue
	{
		get => Value != null && (bool) Value;
		set => Value = value;
	}

	public void Dispose()
	{
		ViewModel?.Dispose();
	}
}
