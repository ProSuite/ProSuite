using System;
using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public abstract class TestParameterValueBlazorBase<T> : ComponentBase, IDisposable
{
	[Parameter]
	public ViewModelBase ViewModel { get; set; }

	protected string ErrorMessage => ViewModel.ErrorMessage;

	public void Dispose()
	{
		ViewModel?.Dispose();
	}

	protected void SetValue(object value)
	{
		ViewModel.Value = value;
	}

	protected T GetValue()
	{
		return (T) ViewModel.Value;
	}
}
