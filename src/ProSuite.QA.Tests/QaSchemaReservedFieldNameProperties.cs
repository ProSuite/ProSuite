using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaReservedFieldNameProperties : QaSchemaReservedFieldNamesBase
	{
		private readonly ITable _fieldSpecificationsTable;

		public QaSchemaReservedFieldNameProperties(
			[NotNull] ITable table,
			[NotNull] ITable reservedNamesTable,
			[NotNull] string reservedNameFieldName,
			[CanBeNull] string reservedReasonFieldName,
			[CanBeNull] string validNameFieldName,
			[CanBeNull] ITable fieldSpecificationsTable)
			: base(table, reservedNamesTable,
			       reservedNameFieldName, reservedReasonFieldName, validNameFieldName,
			       fieldSpecificationsTable)
		{
			_fieldSpecificationsTable = fieldSpecificationsTable;
		}

		[NotNull]
		private IQueryFilter GetQueryFilter()
		{
			string constraint = GetConstraint(_fieldSpecificationsTable);

			IQueryFilter result = new QueryFilter();

			if (StringUtils.IsNotEmpty(constraint))
			{
				result.WhereClause = constraint;
			}

			return result;
		}

		protected override IEnumerable<FieldSpecification> GetFieldSpecifications()
		{
			return _fieldSpecificationsTable == null
				       ? new List<FieldSpecification>()
				       : FieldSpecificationUtils.ReadFieldSpecifications(
					       _fieldSpecificationsTable, GetQueryFilter());
		}
	}
}
