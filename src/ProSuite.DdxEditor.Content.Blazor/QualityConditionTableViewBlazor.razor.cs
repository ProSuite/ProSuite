using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DomainModel.AO.QA;
using Radzen;

namespace ProSuite.DdxEditor.Content.Blazor;

public partial class QualityConditionTableViewBlazor
{
	[NotNull]
	[Parameter]
	// ReSharper disable once NotNullMemberIsNotInitialized
	public IDataGridViewModel ViewModel { get; set; }

	private void OnFocusOut(FocusEventArgs args) { }

	#region layout

	private void OnRowRender(RowRenderEventArgs<ViewModelBase> args)
	{
		// expander or not?
		args.Expandable = args.Data is TestParameterValueCollectionViewModel;
	}

	private void OnCellRender(DataGridCellRenderEventArgs<ViewModelBase> args)
	{
		IDictionary<string, object> attributes = args.Attributes;

		if (args.Data is TestParameterValueCollectionViewModel vm &&
		    ! TestParameterTypeUtils.IsDatasetType(vm.Parameter.Type))
		{
			SetBackgroundColorGrey(args, attributes);
			return;
		}

		if (args.Data is ScalarTestParameterValueViewModel)
		{
			SetBackgroundColorGrey(args, attributes);
		}
	}

	private static void SetBackgroundColorGrey(
		[NotNull] DataGridCellRenderEventArgs<ViewModelBase> args,
		[NotNull] IDictionary<string, object> attributes)
	{
		if (args.Column.Property == "ModelName")
		{
			attributes.Add("colspan", 3);

			if (attributes.ContainsKey("style"))
			{
				attributes["style"] += "; background-color: #adadad";
			}
			else
			{
				attributes.Add("style", "background-color: #adadad");
			}
		}
	}

	#endregion
}
