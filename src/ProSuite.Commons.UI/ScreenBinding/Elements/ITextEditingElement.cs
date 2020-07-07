namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public interface ITextEditingElement : IScreenElement
	{
		CoerceFunction<string> Coercion { get; set; }

		FormatValue Format { get; set; }
	}
}
