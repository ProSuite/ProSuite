using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class TrTableJoinInMemoryDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef LeftTable { get; }
		public ITableSchemaDef RightTable { get; }
		public string LeftTableKey { get; }
		public string RightTableKey { get; }
		public JoinType JoinType { get; }

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_0))]
		public TrTableJoinInMemoryDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_leftTable))]
			ITableSchemaDef leftTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_rightTable))]
			ITableSchemaDef rightTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_leftTableKey))]
			string leftTableKey,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_rightTableKey))]
			string rightTableKey,
			[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_joinType))]
			JoinType joinType)
			: base(new[] { leftTable, rightTable })
		{
			leftTable = leftTable;
		}

		[TestParameter]
		[CanBeNull]
		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTable))]
		public ITableSchemaDef ManyToManyTable { get; set; }

		[TestParameter]
		[CanBeNull]
		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTableLeftKey))]
		public string ManyToManyTableLeftKey { get; set; }

		[TestParameter]
		[CanBeNull]
		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTableRightKey))]
		public string ManyToManyTableRightKey { get; set; }
	}
}
