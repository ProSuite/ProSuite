using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Configuration;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class TextEditingElementExpression :
		ScreenElementExpression<TextEditingElementExpression>
	{
		private const string PRECISION_FORMAT_PATTERN = "{{0:F{0}}}";
		private readonly ITextEditingElement _element;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextEditingElementExpression"/> class.
		/// </summary>
		/// <param name="element">The element.</param>
		public TextEditingElementExpression([NotNull] ITextEditingElement element)
			: base(element)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			_element = element;
		}

		protected override TextEditingElementExpression ThisExpression()
		{
			return this;
		}

		public TextEditingElementExpression ThePrecisionIs(int numDigitsAfterDecimal)
		{
			_element.Format = o => string.Format(
				string.Format(PRECISION_FORMAT_PATTERN,
				              numDigitsAfterDecimal), o);

			return this;
		}
	}
}
