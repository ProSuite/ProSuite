using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.UI.Core.QA
{
	public interface ISqlExpressionBuilder
	{
		[CanBeNull]
		string BuildSqlExpression([NotNull] ITableSchemaDef tableSchema,
		                          string currentExpression);
	}
}
