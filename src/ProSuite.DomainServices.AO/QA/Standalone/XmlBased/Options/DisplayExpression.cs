using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class DisplayExpression
	{
		public DisplayExpression(bool showMapTips,
		                         [CanBeNull] string expression = null,
		                         bool isExpressionSimple = true)
		{
			ShowMapTips = showMapTips;
			Expression = string.IsNullOrEmpty(expression) ? null : expression.Trim();
			IsExpressionSimple = isExpressionSimple;
		}

		public bool ShowMapTips { get; }

		[CanBeNull]
		public string Expression { get; }

		public bool IsExpressionSimple { get; }
	}
}
