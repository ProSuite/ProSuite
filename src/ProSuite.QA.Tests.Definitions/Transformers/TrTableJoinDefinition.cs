using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class TrTableJoinDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef T0 { get; }
		public ITableSchemaDef T1 { get; }
		public string RelationName { get; }
		public JoinType JoinType { get; }

		[DocTr(nameof(DocTrStrings.TrTableJoin_0))]
		public TrTableJoinDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoin_t0))]
			ITableSchemaDef t0,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoin_t1))]
			ITableSchemaDef t1,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoin_relationName))]
			string relationName,
			[DocTr(nameof(DocTrStrings.TrTableJoin_joinType))]
			JoinType joinType)
			: base(t0)  
		{
			T0 = t0;
			T1 = t1;
			RelationName = relationName;
			JoinType = joinType;
		}
	}
}
