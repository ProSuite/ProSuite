using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Configuration;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class NumericUpDownElementExpression :
		ScreenElementExpression<NumericUpDownElementExpression>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NumericUpDownElementExpression"/> class.
		/// </summary>
		/// <param name="element">The element.</param>
		public NumericUpDownElementExpression([NotNull] NumericUpDownElement element)
			: base(element) { }

		protected override NumericUpDownElementExpression ThisExpression()
		{
			return this;
		}
	}
}
