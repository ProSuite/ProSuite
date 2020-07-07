using ProSuite.Commons.UI.ScreenBinding.Configuration;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class CheckboxElementExpression :
		ScreenElementExpression<CheckboxElementExpression>
	{
		public CheckboxElementExpression(IScreenElement element) : base(element) { }

		protected override CheckboxElementExpression ThisExpression()
		{
			return this;
		}
	}
}
