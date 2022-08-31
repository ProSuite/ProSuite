using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class TestParameterValueCollectionBlazor
{
	[Parameter]
	public TestParameterValueCollectionViewModel ViewModel { get; set; }
}
