using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Checks Constraints on a table
	/// </summary>
	[UsedImplicitly]
	[AttributeTest]
	public class QaGdbConstraintDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }

		[Doc(nameof(DocStrings.QaGdbConstraint_0))]
		public QaGdbConstraintDefinition(
			[Doc(nameof(DocStrings.QaGdbConstraint_table))]
			ITableSchemaDef table)
			: base(table)
		{
			Table = table;
		}
	}
}
