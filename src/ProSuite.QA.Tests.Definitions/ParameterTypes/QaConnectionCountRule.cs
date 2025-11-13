using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.QA.Tests.ParameterTypes
{
	public class QaConnectionCountRule
	{
		public QaConnectionCountRule([NotNull] ITableSchemaDef table,
		                             [NotNull] string countSelectionExpression)
		{
			Table = table;
			CountSelectionExpression = countSelectionExpression;
		}

		[NotNull]
		public ITableSchemaDef Table { get; }

		[NotNull]
		public string CountSelectionExpression { get; }
	}
}
