namespace ProSuite.DdxEditor.Content.Blazor.View;

// todo daro rename DoubletTestParameterValueBlazor etc.
public partial class DoubleValueBlazor : TestParameterValueBlazorBase<double?>
{
	public double DoubleValue
	{
		get => GetValue() ?? -1;
		set => SetValue(value);
	}
}
