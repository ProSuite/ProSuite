using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core
{
	public interface IIssueFilterExpressionParser
	{
		IList<string> GetFilterNames([CanBeNull] string filterExpression);
	}
}
