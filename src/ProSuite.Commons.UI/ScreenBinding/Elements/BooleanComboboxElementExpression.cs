using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Configuration;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class BooleanComboboxElementExpression :
		ScreenElementExpression<BooleanComboboxElementExpression>
	{
		public BooleanComboboxElementExpression([NotNull] IScreenElement element)
			: base(element) { }

		protected override BooleanComboboxElementExpression ThisExpression()
		{
			return this;
		}
	}
}
