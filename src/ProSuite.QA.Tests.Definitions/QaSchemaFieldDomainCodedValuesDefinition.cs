using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldDomainCodedValuesDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public int MaximumNameLength { get; }
		public UniqueStringsConstraint UniqueNamesConstraint { get; }
		public int MinimumValueCount { get; }
		public int MinimumNonEqualNameValueCount { get; }
		public bool AllowEmptyName { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_0))]
		public QaSchemaFieldDomainCodedValuesDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_maximumNameLength))]
			int maximumNameLength,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_uniqueNamesConstraint))]
			UniqueStringsConstraint uniqueNamesConstraint,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_minimumValueCount))]
			int minimumValueCount,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_minimumNonEqualNameValueCount))]
			int minimumNonEqualNameValueCount,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_allowEmptyName))]
			bool allowEmptyName)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			Table = table;
			MaximumNameLength = maximumNameLength;
			UniqueNamesConstraint = uniqueNamesConstraint;
			MinimumValueCount = minimumValueCount;
			MinimumNonEqualNameValueCount = minimumNonEqualNameValueCount;
			AllowEmptyName = allowEmptyName;
		}
	}
}
