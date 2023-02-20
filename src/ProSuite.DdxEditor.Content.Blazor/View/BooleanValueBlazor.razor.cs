using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class BooleanValueBlazor
{
	[Parameter]
	public ScalarTestParameterValueViewModel ViewModel { get; set; }

	[CanBeNull]
	public object Value
	{
		get => ViewModel.Value;
		set => ViewModel.Value = value;
	}

	public bool BoolValue
	{
		get => Value != null && (bool) Value;
		set => Value = value;
	}
}
