using System;
using System.Collections.Generic;
using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Globalization;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaValidDateValues : ContainerTest
	{
		private readonly DateTime _minimumDateValue;
		private readonly DateTime _maximumDateValue;
		private readonly List<int> _dateFieldIndices = new List<int>();

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string UnrecognizedDateValue = "UnrecognizedDateValue";
			public const string ValueBeforeEarliestValidDate = "ValueBeforeEarliestValidDate";
			public const string ValueAfterLatestValidDate = "ValueAfterLatestValidDate";

			public Code() : base("DateValues") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaValidDateValues_0))]
		public QaValidDateValues(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateValue))]
			DateTime maximumDateValue)
			: this(table, minimumDateValue, maximumDateValue, GetAllDateFieldNames(table)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_1))]
		public QaValidDateValues(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateValue))]
			DateTime maximumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNames))] [NotNull]
			IEnumerable<string>
				dateFieldNames)
			: base(table)
		{
			Assert.ArgumentNotNull(dateFieldNames, nameof(dateFieldNames));

			_minimumDateValue = minimumDateValue;
			_maximumDateValue = maximumDateValue;

			foreach (string dateFieldName in dateFieldNames)
			{
				int index = table.FindField(dateFieldName);
				if (index < 0)
				{
					throw new ArgumentException(
						string.Format("Date field not found in table {0}: {1}",
						              table.Name, dateFieldName),
						nameof(dateFieldNames));
				}

				_dateFieldIndices.Add(index);
			}
		}

		[Doc(nameof(DocStrings.QaValidDateValues_2))]
		public QaValidDateValues(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateValue))]
			DateTime maximumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNamesString))] [NotNull]
			string
				dateFieldNamesString)
			: this(table, minimumDateValue, maximumDateValue,
			       TestUtils.GetTokens(dateFieldNamesString)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_3))]
		public QaValidDateValues(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateTimeRelativeToNow))] [CanBeNull]
			string
				maximumDateTimeRelativeToNow)
			: this(table, minimumDateValue,
			       GetDateTimeRelativeToNow(maximumDateTimeRelativeToNow),
			       GetAllDateFieldNames(table)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_4))]
		public QaValidDateValues(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateTimeRelativeToNow))] [CanBeNull]
			string
				maximumDateTimeRelativeToNow,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNamesString))] [NotNull]
			string
				dateFieldNamesString)
			: this(table,
			       minimumDateValue,
			       GetDateTimeRelativeToNow(maximumDateTimeRelativeToNow),
			       TestUtils.GetTokens(dateFieldNamesString)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_5))]
		public QaValidDateValues(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateTimeRelativeToNow))] [CanBeNull]
			string
				minimumDateTimeRelativeToNow,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateValue))]
			DateTime maximumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNamesString))] [NotNull]
			string
				dateFieldNamesString)
			: this(table,
			       GetDateTimeRelativeToNow(minimumDateTimeRelativeToNow),
			       maximumDateValue,
			       TestUtils.GetTokens(dateFieldNamesString)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_6))]
		public QaValidDateValues(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateTimeRelativeToNow))] [CanBeNull]
			string
				minimumDateTimeRelativeToNow,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateTimeRelativeToNow))] [CanBeNull]
			string
				maximumDateTimeRelativeToNow,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNamesString))] [NotNull]
			string
				dateFieldNamesString)
			: this(table,
			       GetDateTimeRelativeToNow(minimumDateTimeRelativeToNow),
			       GetDateTimeRelativeToNow(maximumDateTimeRelativeToNow),
			       TestUtils.GetTokens(dateFieldNamesString)) { }

		[InternallyUsedTest]
		public QaValidDateValues([NotNull] QaValidDateValuesDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.MinimumDateValue,
			       definition.MaximumDateValue, definition.DateFieldNames) { }

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

			foreach (int dateFieldIndex in _dateFieldIndices)
			{
				object dateValue = row.get_Value(dateFieldIndex);

				if (dateValue == null || dateValue is DBNull)
				{
					continue;
				}

				DateTime dateTime;
				string fieldName;
				try
				{
					dateTime = (DateTime) dateValue;
				}
				catch (Exception e)
				{
					string description = string.Format(
						"Invalid date value in field {0}: {1} ({2})",
						TestUtils.GetFieldDisplayName(row, dateFieldIndex, out fieldName),
						dateValue, e.Message);

					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row),
						TestUtils.GetShapeCopy(row), Codes[Code.UnrecognizedDateValue], fieldName);
					continue;
				}

				if (dateTime < _minimumDateValue)
				{
					string description = string.Format(
						"Date value {0} in field {1} is before earliest valid value: {2}",
						dateValue,
						TestUtils.GetFieldDisplayName(row, dateFieldIndex, out fieldName),
						_minimumDateValue);

					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row),
						TestUtils.GetShapeCopy(row), Codes[Code.ValueBeforeEarliestValidDate],
						fieldName);
				}
				else if (dateTime > _maximumDateValue)
				{
					string description = string.Format(
						"Date value {0} in field {1} is after latest valid value: {2}",
						dateValue,
						TestUtils.GetFieldDisplayName(row, dateFieldIndex, out fieldName),
						_maximumDateValue);

					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row),
						TestUtils.GetShapeCopy(row), Codes[Code.ValueAfterLatestValidDate],
						fieldName);
				}
			}

			return errorCount;
		}

		private static DateTime GetDateTimeRelativeToNow(
			[CanBeNull] string maximumDateTimeRelativeToNow)
		{
			DateTime now = DateTime.Now;

			if (string.IsNullOrEmpty(maximumDateTimeRelativeToNow))
			{
				return now;
			}

			if (StringUtils.IsNullOrEmptyOrBlank(maximumDateTimeRelativeToNow))
			{
				return now;
			}

			TimeSpan timeSpan = CultureInfoUtils.ExecuteUsing(
				CultureInfo.InvariantCulture,
				() => TimeSpan.Parse(maximumDateTimeRelativeToNow));

			return now.Add(timeSpan);
		}

		[NotNull]
		private static IEnumerable<string> GetAllDateFieldNames([NotNull] IReadOnlyTable table)
		{
			var result = new List<string>();

			foreach (IField field in DatasetUtils.GetFields(table.Fields))
			{
				if (field.Type == esriFieldType.esriFieldTypeDate)
				{
					result.Add(field.Name);
				}
			}

			return result;
		}
	}
}
