using Microsoft.AspNetCore.Components;

namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class StringValueBlazor : TestParameterValueBlazorBase<string>
{
	public string StringValue
	{
		get => GetValue();
		set => SetValue(value);
	}

	private void OnInput(ChangeEventArgs args)
	{
		SetValue(args.Value);
	}
}
