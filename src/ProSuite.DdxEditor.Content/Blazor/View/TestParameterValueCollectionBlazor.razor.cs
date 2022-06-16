using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class TestParameterValueCollectionBlazor
{
	public object Value => ViewModel.DisplayName;

	[Parameter]
	public TestParameterValueCollectionViewModel ViewModel { get; set; }
}
