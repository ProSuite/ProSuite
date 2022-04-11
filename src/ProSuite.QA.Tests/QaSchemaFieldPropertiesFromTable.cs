using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	public class QaSchemaFieldPropertiesFromTable : QaSchemaFieldPropertiesBase
	{
		private readonly ITable _fieldSpecificationsTable;

		/// <summary>
		/// Initializes a new instance of the <see cref="QaSchemaFieldPropertiesFromTable"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="fieldSpecificationsTable">The field specifications table.</param>
		/// <param name="matchAliasName">if set to <c>true</c>, a field specification is looked up by alias name also.</param>
		public QaSchemaFieldPropertiesFromTable([NotNull] ITable table,
		                                        [NotNull] ITable fieldSpecificationsTable,
		                                        bool matchAliasName)
			: base(table, matchAliasName, fieldSpecificationsTable)
		{
			Assert.ArgumentNotNull(fieldSpecificationsTable, nameof(fieldSpecificationsTable));

			_fieldSpecificationsTable = fieldSpecificationsTable;
		}

		[NotNull]
		private IQueryFilter GetQueryFilter()
		{
			string constraint = GetConstraint(_fieldSpecificationsTable);

			IQueryFilter result = new QueryFilterClass();

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
