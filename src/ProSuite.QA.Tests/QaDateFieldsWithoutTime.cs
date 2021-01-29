using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
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

		[Doc("QaDateFieldsWithoutTime_0")]
		public QaDateFieldsWithoutTime(
			[Doc("QaDateFieldsWithoutTime_table")] [NotNull]
			ITable table)
			: this(table, GetAllDateFieldNames(table)) { }

		[Doc("QaDateFieldsWithoutTime_1")]
		public QaDateFieldsWithoutTime(
			[Doc("QaDateFieldsWithoutTime_table")] [NotNull]
			ITable table,
			[Doc("QaDateFieldsWithoutTime_dateFieldName")] [NotNull]
			string dateFieldName)
			: this(table, new[] {dateFieldName}) { }

		[Doc("QaDateFieldsWithoutTime_2")]
		public QaDateFieldsWithoutTime(
			[Doc("QaDateFieldsWithoutTime_table")] [NotNull]
			ITable table,
			[Doc("QaDateFieldsWithoutTime_dateFieldNames")] [NotNull]
			IEnumerable<string>
				dateFieldNames)
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
						              DatasetUtils.GetName(table), fieldName),
						nameof(dateFieldNames));
				}

				if (fields.get_Field(index).Type != esriFieldType.esriFieldTypeDate)
				{
					throw new ArgumentException(
						string.Format("Field {0} in table {1} is not a date field",
						              fieldName, DatasetUtils.GetName(table)),
						nameof(dateFieldNames));
				}

				_dateFieldIndices.Add(index);
			}
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

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			var errorCount = 0;

			foreach (int fieldIndex in _dateFieldIndices)
			{
				errorCount += Checkfield(row, fieldIndex);
			}

			return errorCount;
		}

		private int Checkfield([NotNull] IRow row, int fieldIndex)
		{
			object value = row.Value[fieldIndex];

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

				return ReportError(description,
				                   TestUtils.GetShapeCopy(row),
				                   Codes[Code.InvalidDateValue], fieldName,
				                   row);
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

			return ReportError(description,
			                   TestUtils.GetShapeCopy(row),
			                   Codes[Code.HasTimePart], fieldName,
			                   row);
		}

		[NotNull]
		private static IEnumerable<string> GetAllDateFieldNames([NotNull] ITable table)
		{
			return DatasetUtils.GetFields(table)
			                   .Where(f => f.Type == esriFieldType.esriFieldTypeDate)
			                   .Select(f => f.Name)
			                   .ToArray();
		}
	}
}
