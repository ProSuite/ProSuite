using System;
using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class DatasetTestParameterValueBlazor : IDisposable
{
	// todo daro use TestParameterValueBlazorBase
	[Parameter]
	public DatasetTestParameterValueViewModel ViewModel { get; set; }

	public string ErrorMessage => ViewModel.ErrorMessage;

	public object Value => ViewModel.DisplayValue;

	public void Dispose()
	{
		ViewModel?.Dispose();
	}

	private void OnClick()
	{
		ViewModel.FindDatasetClicked();
	}

	private void OnLinkClicked()
	{
		ViewModel.GoTo();
	}
}
