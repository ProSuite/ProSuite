using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class TrMakeTableDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef BaseTable { get; }
		public string ViewOrTableName { get; }
		public string Sql { get; }
		public string ObjectIdField { get; }

		private readonly string _viewOrTableName;

		private readonly List<ITableSchemaDef> _involvedTables;

		[DocTr(nameof(DocTrStrings.TrMakeTable_0))]
		public TrMakeTableDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_baseTable))]
			ITableSchemaDef baseTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_viewOrTableName))]
			string viewOrTableName)
			: base(baseTable)
		{
			BaseTable = baseTable;
			ViewOrTableName = viewOrTableName;
		}

		[DocTr(nameof(DocTrStrings.TrMakeTable_1))]
		public TrMakeTableDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_baseTable))]
			ITableSchemaDef baseTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_sql))]
			string sql,
			[NotNull] [DocTr(nameof(DocTrStrings.TrMakeTable_objectIdField))]
			string objectIdField)
			: base(baseTable)
		{
			BaseTable = baseTable;
			Sql = sql;
			ObjectIdField = objectIdField;
		}
	}
}
