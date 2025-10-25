using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldProperties : QaSchemaFieldPropertiesBase
	{
		private readonly FieldSpecification _fieldSpecification;

		[Doc(nameof(DocStrings.QaSchemaFieldProperties_0))]
		public QaSchemaFieldProperties(
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_fieldName))] [NotNull]
			string fieldName,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_expectedFieldType))]
			esriFieldType expectedFieldType,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_expectedFieldLength))]
			int expectedFieldLength,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_expectedAliasName))] [CanBeNull]
			string expectedAliasName,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_expectedDomainName))] [CanBeNull]
			string expectedDomainName,
			[Doc(nameof(DocStrings.QaSchemaFieldProperties_fieldIsOptional))]
			bool fieldIsOptional)
			: base(table, matchAliasName: false, referenceTable: null)
		{
			_fieldSpecification = new FieldSpecification(fieldName, expectedFieldType,
			                                             expectedFieldLength, expectedAliasName,
			                                             expectedDomainName, fieldIsOptional);
		}

		[InternallyUsedTest]
		public QaSchemaFieldProperties([NotNull] QaSchemaFieldPropertiesDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.FieldName,
			       (esriFieldType) definition.ExpectedFieldType, definition.ExpectedFieldLength,
			       definition.ExpectedAliasName, definition.ExpectedDomainName,
			       definition.FieldIsOptional) { }

		protected override IEnumerable<FieldSpecification> GetFieldSpecifications()
		{
			return new[] {_fieldSpecification};
		}
	}
}
