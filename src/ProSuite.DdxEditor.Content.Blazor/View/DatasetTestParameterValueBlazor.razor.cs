using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class DatasetTestParameterValueBlazor
{
	[Parameter]
	public DatasetTestParameterValueViewModel ViewModel { get; set; }

	public object Value => ViewModel.DisplayValue;

	private void OnClick()
	{
		ViewModel.FindDatasetClicked();
	}
}
