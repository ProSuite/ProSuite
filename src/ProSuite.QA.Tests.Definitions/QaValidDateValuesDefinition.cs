using System;
using System.Collections.Generic;
using System.Globalization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Globalization;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaValidDateValuesDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public DateTime MinimumDateValue { get; }
		public DateTime MaximumDateValue { get; }
		public IEnumerable<string> DateFieldNames { get; }

		[Doc(nameof(DocStrings.QaValidDateValues_0))]
		public QaValidDateValuesDefinition(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateValue))]
			DateTime maximumDateValue)
			: this(table, minimumDateValue, maximumDateValue, GetAllDateFieldNames(table)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_1))]
		public QaValidDateValuesDefinition(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateValue))]
			DateTime maximumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNames))] [NotNull]
			IEnumerable<string> dateFieldNames)
			: base(table)
		{
			Assert.ArgumentNotNull(dateFieldNames, nameof(dateFieldNames));

			Table = table;
			MinimumDateValue = minimumDateValue;
			MaximumDateValue = maximumDateValue;
			DateFieldNames = dateFieldNames;
		}

		[Doc(nameof(DocStrings.QaValidDateValues_2))]
		public QaValidDateValuesDefinition(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateValue))]
			DateTime maximumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNamesString))] [NotNull]
			string dateFieldNamesString)
			: this(table, minimumDateValue, maximumDateValue,
			       TestDefinitionUtils.GetTokens(dateFieldNamesString)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_3))]
		public QaValidDateValuesDefinition(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateTimeRelativeToNow))] [CanBeNull]
			string maximumDateTimeRelativeToNow)
			: this(table, minimumDateValue,
			       GetDateTimeRelativeToNow(maximumDateTimeRelativeToNow),
			       GetAllDateFieldNames(table)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_4))]
		public QaValidDateValuesDefinition(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateValue))]
			DateTime minimumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateTimeRelativeToNow))] [CanBeNull]
			string maximumDateTimeRelativeToNow,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNamesString))] [NotNull]
			string dateFieldNamesString)
			: this(table, minimumDateValue,
			       GetDateTimeRelativeToNow(maximumDateTimeRelativeToNow),
			       TestDefinitionUtils.GetTokens(dateFieldNamesString)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_5))]
		public QaValidDateValuesDefinition(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateTimeRelativeToNow))] [CanBeNull]
			string minimumDateTimeRelativeToNow,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateValue))]
			DateTime maximumDateValue,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNamesString))] [NotNull]
			string dateFieldNamesString)
			: this(table,
			       GetDateTimeRelativeToNow(minimumDateTimeRelativeToNow),
			       maximumDateValue,
			       TestDefinitionUtils.GetTokens(dateFieldNamesString)) { }

		[Doc(nameof(DocStrings.QaValidDateValues_6))]
		public QaValidDateValuesDefinition(
			[Doc(nameof(DocStrings.QaValidDateValues_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaValidDateValues_minimumDateTimeRelativeToNow))] [CanBeNull]
			string minimumDateTimeRelativeToNow,
			[Doc(nameof(DocStrings.QaValidDateValues_maximumDateTimeRelativeToNow))] [CanBeNull]
			string maximumDateTimeRelativeToNow,
			[Doc(nameof(DocStrings.QaValidDateValues_dateFieldNamesString))] [NotNull]
			string dateFieldNamesString)
			: this(table,
			       GetDateTimeRelativeToNow(minimumDateTimeRelativeToNow),
			       GetDateTimeRelativeToNow(maximumDateTimeRelativeToNow),
			       TestDefinitionUtils.GetTokens(dateFieldNamesString)) { }

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
		private static IList<string> GetAllDateFieldNames([NotNull] ITableSchemaDef table)
		{
			var result = new List<string>();
			foreach (ITableField field in table.TableFields)
			{
				if (field.FieldType == FieldType.Date)
				{
					result.Add(field.Name);
				}
			}

			return result;
		}
	}
}
