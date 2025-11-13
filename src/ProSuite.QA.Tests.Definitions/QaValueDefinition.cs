using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using System.Collections.Generic;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaValueDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public IList<string> Fields { get; }

		[Doc(nameof(DocStrings.QaValue_0))]
		public QaValueDefinition(
			[Doc(nameof(DocStrings.QaValue_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaValue_fields))] [CanBeNull]
			IList<string> fields)
			: base(table)
		{
			Table = table;
			Fields = fields;
		}

		
	}
}
