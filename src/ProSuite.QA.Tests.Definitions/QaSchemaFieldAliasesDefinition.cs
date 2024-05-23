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
	public class QaSchemaFieldAliasesDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public int MaximumLength { get; }
		public ExpectedCase ExpectedCase { get; }
		public bool RequireUniqueAliasNames { get; }
		public bool AllowCustomSystemFieldAlias { get; }
		public ExpectedStringDifference ExpectedDifference { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldAliases_0))]
		public QaSchemaFieldAliasesDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_expectedCase))]
			ExpectedCase expectedCase,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_requireUniqueAliasNames))]
			bool requireUniqueAliasNames,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_allowCustomSystemFieldAlias))]
			bool allowCustomSystemFieldAlias)
			: this(table, maximumLength, expectedCase, requireUniqueAliasNames,
			       allowCustomSystemFieldAlias,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       ExpectedStringDifference.Any) { }

		[Doc(nameof(DocStrings.QaSchemaFieldAliases_0))]
		public QaSchemaFieldAliasesDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_expectedCase))]
			ExpectedCase expectedCase,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_requireUniqueAliasNames))]
			bool requireUniqueAliasNames,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_allowCustomSystemFieldAlias))]
			bool allowCustomSystemFieldAlias,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_expectedDifference))]
			ExpectedStringDifference expectedDifference)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			Table = table;
			MaximumLength = maximumLength;
			ExpectedCase = expectedCase;
			RequireUniqueAliasNames = requireUniqueAliasNames;
			AllowCustomSystemFieldAlias = allowCustomSystemFieldAlias;
			ExpectedDifference = expectedDifference;
		}
	}
}
