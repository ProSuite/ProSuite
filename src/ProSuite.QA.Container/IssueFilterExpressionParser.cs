using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.QA.Container
{
	public class IssueFilterExpressionParser : IIssueFilterExpressionParser
	{
		[NotNull]
		public IList<string> GetFilterNames(string filterExpression)
		{
			return FilterUtils.GetFilterNames(filterExpression);
		}
	}
}
