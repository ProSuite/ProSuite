using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaUnreferencedRowsDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef ReferencedTable { get; }
		public IList<ITableSchemaDef> ReferencingTables { get; }
		public IList<string> Relations { get; }

		[Doc(nameof(DocStrings.QaUnreferencedRows_0))]
		public QaUnreferencedRowsDefinition(
			[Doc(nameof(DocStrings.QaUnreferencedRows_referencedTable))] [NotNull]
			ITableSchemaDef referencedTable,
			[Doc(nameof(DocStrings.QaUnreferencedRows_referencingTables))] [NotNull]
			IList<ITableSchemaDef> referencingTables,
			[Doc(nameof(DocStrings.QaUnreferencedRows_relations))] [NotNull]
			IList<string> relations)
			: base(referencingTables.Append(referencedTable))
		{
			Assert.ArgumentNotNull(referencedTable, nameof(referencedTable));
			Assert.ArgumentNotNull(referencingTables, nameof(referencingTables));
			Assert.ArgumentNotNull(relations, nameof(relations));

			Assert.ArgumentCondition(relations.Count == referencingTables.Count,
			                         "# of referencers != # of foreignKeys");

			ReferencedTable = referencedTable;
			ReferencingTables = referencingTables;
			Relations = relations;
		}
	}
}
