using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Schema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldProperties : QaSchemaFieldPropertiesBase
	{
		private readonly FieldSpecification _fieldSpecification;

		[Doc("QaSchemaFieldProperties_0")]
		public QaSchemaFieldProperties(
			[Doc("QaSchemaFieldProperties_table")] [NotNull]
			ITable table,
			[Doc("QaSchemaFieldProperties_fieldName")] [NotNull]
			string fieldName,
			[Doc("QaSchemaFieldProperties_expectedFieldType")]
			esriFieldType expectedFieldType,
			[Doc("QaSchemaFieldProperties_expectedFieldLength")]
			int expectedFieldLength,
			[Doc("QaSchemaFieldProperties_expectedAliasName")] [CanBeNull]
			string
				expectedAliasName,
			[Doc("QaSchemaFieldProperties_expectedDomainName")] [CanBeNull]
			string
				expectedDomainName,
			[Doc("QaSchemaFieldProperties_fieldIsOptional")]
			bool fieldIsOptional)
			: base(table, false)
		{
			_fieldSpecification = new FieldSpecification(fieldName, expectedFieldType,
			                                             expectedFieldLength, expectedAliasName,
			                                             expectedDomainName, fieldIsOptional);
		}

		protected override IEnumerable<FieldSpecification> GetFieldSpecifications()
		{
			return new[] {_fieldSpecification};
		}
	}
}
