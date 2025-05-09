using Microsoft.AspNetCore.Components;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class StringValueBlazor : TestParameterValueBlazorBase<string>
{
	public string StringValue
	{
		get => GetValue();
		set => SetValue(value);
	}

	private bool ShowExpressionBuilderButton =>
		ViewModel is ScalarTestParameterValueViewModel scalarViewModel &&
		scalarViewModel.ShowSqlExpressionBuilderButton;

	private void OnInput(ChangeEventArgs args)
	{
		SetValue(args.Value);
	}

	private void OnExpressionBuilderClicked()
	{
		string newValue =
			((ScalarTestParameterValueViewModel) ViewModel).ShowSqlExpressionBuilder();

		if (newValue != null)
		{
			SetValue(newValue);
		}
	}
}
