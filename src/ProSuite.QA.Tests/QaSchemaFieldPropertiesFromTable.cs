using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldPropertiesFromTable : QaSchemaFieldPropertiesBase
	{
		private readonly IReadOnlyTable _fieldSpecificationsTable;

		[Doc(nameof(DocStrings.QaSchemaFieldPropertiesFromTable_0))]
		public QaSchemaFieldPropertiesFromTable(
			[Doc(nameof(DocStrings.QaSchemaFieldPropertiesFromTable_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldPropertiesFromTable_fieldSpecificationsTable))]
			[NotNull]
			IReadOnlyTable fieldSpecificationsTable,
			[Doc(nameof(DocStrings.QaSchemaFieldPropertiesFromTable_matchAliasName))]
			bool matchAliasName)
			: base(table, matchAliasName, fieldSpecificationsTable)
		{
			Assert.ArgumentNotNull(fieldSpecificationsTable, nameof(fieldSpecificationsTable));

			_fieldSpecificationsTable = fieldSpecificationsTable;
		}

		[InternallyUsedTest]
		public QaSchemaFieldPropertiesFromTable(
			[NotNull] QaSchemaFieldPropertiesFromTableDefinition definition)
			: this((IReadOnlyTable) definition.Table,
			       (IReadOnlyTable) definition.FieldSpecificationsTable,
			       definition.MatchAliasName) { }

		[NotNull]
		private ITableFilter GetQueryFilter()
		{
			string constraint = GetConstraint(_fieldSpecificationsTable);

			ITableFilter result = new AoTableFilter();

			if (StringUtils.IsNotEmpty(constraint))
			{
				result.WhereClause = constraint;
			}

			return result;
		}

		protected override IEnumerable<FieldSpecification> GetFieldSpecifications()
		{
			return FieldSpecificationUtils.ReadFieldSpecifications(
				_fieldSpecificationsTable, GetQueryFilter());
		}
	}
}
