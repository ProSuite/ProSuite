using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldPropertiesDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public string FieldName { get; }
		public FieldType ExpectedFieldType { get; }
		public int ExpectedFieldLength { get; }
		public string ExpectedAliasName { get; }
		public string ExpectedDomainName { get; }
		public bool FieldIsOptional { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldProperties_0))]
		public QaSchemaFieldPropertiesDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_fieldName))] [NotNull]
			string fieldName,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_expectedFieldType))]
			FieldType expectedFieldType,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_expectedFieldLength))]
			int expectedFieldLength,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_expectedAliasName))] [CanBeNull]
			string expectedAliasName,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_expectedDomainName))] [CanBeNull]
			string expectedDomainName,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_fieldIsOptional))]
			bool fieldIsOptional)
			: base(table)
		{
			Table = table;
			FieldName = fieldName;
			ExpectedFieldType = expectedFieldType;
			ExpectedFieldLength = expectedFieldLength;
			ExpectedAliasName = expectedAliasName;
			ExpectedDomainName = expectedDomainName;
			FieldIsOptional = fieldIsOptional;
		}
	}
}
