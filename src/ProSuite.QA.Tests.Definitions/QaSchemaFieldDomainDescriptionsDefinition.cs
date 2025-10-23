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
	[SchemaTest]
	public class QaSchemaFieldDomainDescriptionsDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public int MaximumLength { get; }
		public bool RequireUniqueDescriptions { get; }
		public ITableSchemaDef TargetWorkspaceTable { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_0))]
		public QaSchemaFieldDomainDescriptionsDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_requireUniqueDescriptions))]
			bool requireUniqueDescriptions,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_targetWorkspaceTable))]
			[CanBeNull]
			ITableSchemaDef targetWorkspaceTable)
			: base(new[] { table }.Union(new[] { targetWorkspaceTable }))
		{
			Assert.ArgumentNotNull(table, nameof(table));

			Table = table;
			MaximumLength = maximumLength;
			RequireUniqueDescriptions = requireUniqueDescriptions;
			TargetWorkspaceTable = targetWorkspaceTable;
		}

		[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_0))]
		public QaSchemaFieldDomainDescriptionsDefinition(
				[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_maximumLength))]
				int maximumLength,
				[Doc(nameof(DocStrings.QaSchemaFieldDomainDescriptions_requireUniqueDescriptions))]
				bool requireUniqueDescriptions)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, maximumLength, requireUniqueDescriptions, null) { }
	}
}
