using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaReservedFieldNameProperties : QaSchemaReservedFieldNamesBase
	{
		private readonly IReadOnlyTable _fieldSpecificationsTable;

		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_2))]
		public QaSchemaReservedFieldNameProperties(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNamesTable))] [NotNull]
			IReadOnlyTable reservedNamesTable,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNameFieldName))] [NotNull]
			string reservedNameFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedReasonFieldName))] [CanBeNull]
			string reservedReasonFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_validNameFieldName))] [CanBeNull]
			string validNameFieldName,
			[CanBeNull] IReadOnlyTable fieldSpecificationsTable)
			: base(table, reservedNamesTable,
			       reservedNameFieldName, reservedReasonFieldName, validNameFieldName,
			       fieldSpecificationsTable)
		{
			_fieldSpecificationsTable = fieldSpecificationsTable;
		}

		[InternallyUsedTest]
		public QaSchemaReservedFieldNameProperties(
			[NotNull] QaSchemaReservedFieldNamePropertiesDefinition definition)
			: this((IReadOnlyTable) definition.Table,
			       (IReadOnlyTable) definition.ReservedNamesTable,
			       definition.ReservedNameFieldName, definition.ReservedReasonFieldName,
			       definition.ValidNameFieldName,
			       (IReadOnlyTable) definition.FieldSpecificationsTable) { }

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
			return _fieldSpecificationsTable == null
				       ? Enumerable.Empty<FieldSpecification>()
				       : FieldSpecificationUtils.ReadFieldSpecifications(
					       _fieldSpecificationsTable, GetQueryFilter());
		}
	}
}
