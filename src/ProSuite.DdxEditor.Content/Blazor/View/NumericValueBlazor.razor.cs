using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class NumericValueBlazor
{
	public string Value
	{
		get => ViewModel.Value.ToString();
		set => ViewModel.Value = value;
	}

	[Parameter]
	public ScalarTestParameterValueViewModel ViewModel { get; set; }
}
