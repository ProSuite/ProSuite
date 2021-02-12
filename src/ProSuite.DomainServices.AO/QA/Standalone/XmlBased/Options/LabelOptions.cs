using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class LabelOptions
	{
		public LabelOptions(bool visible,
		                    [CanBeNull] string expression = null,
		                    bool isExpressionSimple = true,
		                    double minimumScale = 0)
		{
			Visible = visible;
			MinimumScale = minimumScale;
			Expression = string.IsNullOrEmpty(expression) ? null : expression.Trim();
			IsExpressionSimple = isExpressionSimple;
		}

		public bool Visible { get; }

		public double MinimumScale { get; }

		[CanBeNull]
		public string Expression { get; }

		public bool IsExpressionSimple { get; }
	}
}
