using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class DatasetTestParameterValueBlazor
{
	public object Value
	{
		get => ViewModel.Value;
		set => ViewModel.Value = value;
	}

	[Parameter]
	public DatasetTestParameterValueViewModel ViewModel { get; set; }

	private void OnClick()
	{
		ViewModel.FindDatasetClicked();
	}
}
