namespace ProSuite.Commons.UI.ScreenBinding.Configuration
{
	public class BasicElementExpression : ScreenElementExpression<BasicElementExpression>
	{
		public BasicElementExpression(IScreenElement element) : base(element) { }

		protected override BasicElementExpression ThisExpression()
		{
			return this;
		}
	}
}
