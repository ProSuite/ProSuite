using System;
using Microsoft.AspNetCore.Components;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class SqlExpressionBlazor : ComponentBase
{
	[Parameter]
	public string Value { get; set; }

	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	[Parameter]
	public EventCallback<string> ExpressionChanged { get; set; }

	[Parameter]
	public Func<string> QueryBuilderCallback { get; set; }

	public void OnClickResetValue()
	{
		SetValue(string.Empty);
	}

	public void OnSqlExpressionBuilderClicked()
	{
		string resultExpression = QueryBuilderCallback();

		if (resultExpression != null)
		{
			SetValue(resultExpression);
		}
	}

	private void OnInput(ChangeEventArgs args)
	{
		SetValue(args.Value?.ToString());
	}

	private void SetValue(string newValue)
	{
		Value = newValue;

		// Necessary to trigger the dirty flag to enable saving
		ExpressionChanged.InvokeAsync(Value);
	}
}
