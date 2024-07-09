using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.UI.QA
{
	public interface ISqlExpressionBuilder
	{
		string BuildSqlExpression([NotNull] ITableSchemaDef tableSchema,
		                          string currentExpression);
	}
}
