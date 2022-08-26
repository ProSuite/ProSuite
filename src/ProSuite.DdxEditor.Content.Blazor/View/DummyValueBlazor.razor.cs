namespace ProSuite.DdxEditor.Content.Blazor.View
{
	public partial class DummyValueBlazor : TestParameterValueBlazorBase<string>
	{
		public string Value => GetValue();
	}
}
