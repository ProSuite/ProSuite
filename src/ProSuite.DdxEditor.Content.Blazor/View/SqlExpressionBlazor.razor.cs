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
		Value = string.Empty;
	}

	public void OnSqlExpressionBuilderClicked()
	{
		string resultExpression = QueryBuilderCallback();

		if (resultExpression != null)
		{
			Value = resultExpression;
		}
	}

	private void OnInput(ChangeEventArgs args)
	{
		Value = args.Value?.ToString();

		// Necessary to trigger the dirty flag to enable saving
		ExpressionChanged.InvokeAsync(Value);
	}
}
