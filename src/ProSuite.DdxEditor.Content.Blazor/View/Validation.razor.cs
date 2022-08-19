using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Radzen;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class Validation
{
	private ElementReference _element;
	[CanBeNull] private string _errorMessage;

	[CanBeNull]
	[Parameter]
	public string ErrorMessage
	{
		get => _errorMessage;
		set
		{
			_errorMessage = value;

			ImagePath = string.IsNullOrEmpty(_errorMessage) ? null : @"images/Error.png";
		}
	}

	[Inject]
	public TooltipService TooltipService { get; set; }

	[Parameter]
	public RenderFragment ChildContent { get; set; }

	[Parameter]
	public string TooltipText { get; set; }

	[CanBeNull]
	public string ImagePath { get; set; }

	private void ShowTooltip(ElementReference elementReference, TooltipOptions options = null)
	{
		TooltipText = "Value not set";
		TooltipService.Open(elementReference, TooltipText, options);
	}

	private void OnMouseOver(ElementReference element)
	{
		if (! string.IsNullOrEmpty(ImagePath))
		{
			ShowTooltip(element, new TooltipOptions { Position = TooltipPosition.Bottom });
		}
	}
}
