using System;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class StringValueBlazor : IDisposable
{
	[Parameter]
	public ViewModelBase ViewModel { get; set; }

	[CanBeNull]
	public object Value
	{
		get => ViewModel.Value?.ToString();
		set => ViewModel.Value = value;
	}

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
