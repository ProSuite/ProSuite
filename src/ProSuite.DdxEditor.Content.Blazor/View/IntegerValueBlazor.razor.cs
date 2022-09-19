namespace ProSuite.DdxEditor.Content.Blazor.View;

public partial class IntegerValueBlazor : TestParameterValueBlazorBase<int?>
{
	public int IntegerValue
	{
		get => GetValue() ?? -1;
		set => SetValue(value);
	}
}
