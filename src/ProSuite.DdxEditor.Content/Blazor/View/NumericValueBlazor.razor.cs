using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class NumericValueBlazor
{
	public object Value
	{
		get => ViewModel.Value;
		set => ViewModel.Value = value;
	}

	[Parameter]
	public ScalarTestParameterValueViewModel ViewModel { get; set; }
}
