using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaDateFieldsWithoutTime : ContainerTest
	{
		[NotNull] private readonly List<int> _dateFieldIndices = new List<int>();

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string InvalidDateValue = "InvalidDateValue";
			public const string HasTimePart = "HasTimePart";

			public Code() : base("DateFieldsWithoutTime") { }
		}

		#endregion

		#region constructors

		[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_0))]
		public QaDateFieldsWithoutTime(
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_table))] [NotNull]
			IReadOnlyTable table)
			: this(table, GetAllDateFieldNames(table)) { }

		[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_1))]
		public QaDateFieldsWithoutTime(
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_dateFieldName))] [NotNull]
			string dateFieldName)
			: this(table, new[] {dateFieldName}) { }

		[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_2))]
		public QaDateFieldsWithoutTime(
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_dateFieldNames))] [NotNull]
			IEnumerable<string> dateFieldNames)
			: base(table)
		{
			Assert.ArgumentNotNull(dateFieldNames, nameof(dateFieldNames));

			IFields fields = table.Fields;

			foreach (string fieldName in dateFieldNames)
			{
				Assert.NotNull(fieldName, "field name is null");

				int index = table.FindField(fieldName);
				if (index < 0)
				{
					throw new ArgumentException(
						string.Format("Field not found in table {0}: {1}",
						              table.Name, fieldName),
						nameof(dateFieldNames));
				}

				if (fields.get_Field(index).Type != esriFieldType.esriFieldTypeDate)
				{
					throw new ArgumentException(
						string.Format("Field {0} in table {1} is not a date field",
						              fieldName, table.Name),
						nameof(dateFieldNames));
				}

				_dateFieldIndices.Add(index);
			}
		}

		[InternallyUsedTest]
		public QaDateFieldsWithoutTime(
			[NotNull] QaDateFieldsWithoutTimeDefinition definition)
			: this((IReadOnlyTable) definition.Table,
			       definition.DateFieldNames)
		{
		}

		#endregion

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool IsGeometryUsedTable(int tableIndex)
		{
			return AreaOfInterest != null;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var errorCount = 0;

			foreach (int fieldIndex in _dateFieldIndices)
			{
				errorCount += Checkfield(row, fieldIndex);
			}

			return errorCount;
		}

		private int Checkfield([NotNull] IReadOnlyRow row, int fieldIndex)
		{
			object value = row.get_Value(fieldIndex);

			if (value == null || value is DBNull)
			{
				return NoError;
			}

			string fieldName;
			DateTime dateTimeValue;
			string description;
			try
			{
				dateTimeValue = (DateTime) value;
			}
			catch (Exception e)
			{
				description = string.Format(
					"Invalid date value in field {0}: {1} ({2})",
					TestUtils.GetFieldDisplayName(row, fieldIndex, out fieldName),
					value, e.Message);

				return ReportError(description, InvolvedRowUtils.GetInvolvedRows(row),
				                   TestUtils.GetShapeCopy(row),
				                   Codes[Code.InvalidDateValue], fieldName);
			}

			// check for time part
			if (Equals(dateTimeValue, dateTimeValue.Date))
			{
				// date value equal to field value --> no time part
				return NoError;
			}

			description = string.Format(
				"The value in field {0} has a time part: {1}",
				TestUtils.GetFieldDisplayName(row, fieldIndex, out fieldName),
				dateTimeValue);

			return ReportError(description, InvolvedRowUtils.GetInvolvedRows(row),
			                   TestUtils.GetShapeCopy(row),
			                   Codes[Code.HasTimePart], fieldName);
		}

		[NotNull]
		private static IEnumerable<string> GetAllDateFieldNames([NotNull] IReadOnlyTable table)
		{
			return DatasetUtils.GetFields(table.Fields)
			                   .Where(f => f.Type == esriFieldType.esriFieldTypeDate)
			                   .Select(f => f.Name)
			                   .ToArray();
		}
	}
}
