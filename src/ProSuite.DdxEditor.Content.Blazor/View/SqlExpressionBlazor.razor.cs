using System;
using Microsoft.AspNetCore.Components;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class SqlExpressionBlazor : ComponentBase
{
	public string SqlExpression { get; set; }

	[Parameter]
	public EventCallback OnTextChanged { get; set; }

	[Parameter]
	public Func<string> QueryBuilderCallback { get; set; }

	public void OnClickResetValue()
	{
		SqlExpression = string.Empty;
	}

	public void OnSqlExpressionBuilderClicked()
	{
		string resultExpression = QueryBuilderCallback();

		if (resultExpression != null)
		{
			SqlExpression = resultExpression;
		}
	}

	private void OnInput(ChangeEventArgs args)
	{
		SqlExpression = (args.Value?.ToString());
		OnTextChanged.InvokeAsync();
	}
}
