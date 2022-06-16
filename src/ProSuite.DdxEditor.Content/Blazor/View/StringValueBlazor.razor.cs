using System;
using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class StringValueBlazor : IDisposable
{
	public object Value
	{
		get => ViewModel.Value?.ToString();
		set => ViewModel.Value = value;
	}

	[Parameter]
	public ScalarTestParameterValueViewModel ViewModel { get; set; }

	public string StringValue
	{
		get => Value?.ToString();
		set => Value = value;
	}

	public void Dispose()
	{
		ViewModel?.Dispose();
	}
}
