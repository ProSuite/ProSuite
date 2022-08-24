using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class TestParameterValueCollectionBlazor : IDisposable
{
	private TestParameterValueCollectionViewModel _viewModel;

	public object Value => ViewModel.DisplayName;

	[Parameter]
	public TestParameterValueCollectionViewModel ViewModel
	{
		get => _viewModel;
		set
		{
			_viewModel = value;
			_viewModel.DisplayNameChanged += OnDisplayNameChanged;
		}
	}

	public void Dispose()
	{
		_viewModel.DisplayNameChanged -= OnDisplayNameChanged;
	}

	private void OnDisplayNameChanged(object sender, PropertyChangedEventArgs e)
	{
		StateHasChanged();
	}
}
