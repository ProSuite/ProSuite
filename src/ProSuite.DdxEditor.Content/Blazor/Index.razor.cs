using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.Blazor;

public partial class Index
{
	private QualityConditionRazor _view;

	[CanBeNull]
	[Parameter]
	public QualityConditionQualityConditionPresenterFactory Factory { get; set; }

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			Factory?.CreateObserver(_view);
		}
	}
}
