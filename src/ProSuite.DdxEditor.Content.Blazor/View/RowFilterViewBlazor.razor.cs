using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class RowFilterViewBlazor
{
	[Parameter]
	public DatasetTestParameterValueViewModel ViewModel { get; set; }

	public object Value
	{
		get => ViewModel.RowFilterExpression;
		set => ViewModel.RowFilterExpression = (string) value;
	}

	private void OnClick()
	{
		ViewModel.FindRowFilterClicked(ViewModel.DatasetSource);
	}

	private void OnClickRemove()
	{
		ViewModel.RowFilterConfigurations.Clear();
		ViewModel.RowFilterExpression = null;
	}
}
