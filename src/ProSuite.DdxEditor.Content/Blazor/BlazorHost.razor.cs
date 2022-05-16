using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.Blazor;

public partial class BlazorHost
{
	private QualityConditionRazor _view;

	[CanBeNull]
	[Parameter]
	public QualityConditionPresenterFactory Factory { get; set; }

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			Factory?.CreateObserver(_view);
		}
	}
}
