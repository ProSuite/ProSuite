using System;
using System.Collections.Generic;
using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	[CLSCompliant(false)]
	public abstract class ColumnInfo
	{
		private const string _nullString = "<null>";

		protected ColumnInfo([NotNull] ITable table,
		                     [NotNull] string columnName,
		                     [NotNull] Type columnType)
		{
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));
			Assert.ArgumentNotNull(columnType, nameof(columnType));

			ColumnName = columnName;
			ColumnType = columnType;
			Table = table;
		}

		[NotNull]
		public string ColumnName { get; }

		[NotNull]
		public Type ColumnType { get; }

		[NotNull]
		public ITable Table { get; }

		[NotNull]
		public abstract IEnumerable<string> BaseFieldNames { get; }

		[NotNull]
		public object ReadValue([NotNull] IRow row)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			AssertExpectedTable(row);

			return ReadValueCore(row) ?? DBNull.Value;
		}

		public string FormatValue([NotNull] IRow row)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			AssertExpectedTable(row);

			object value = ReadValue(row);

			return value is DBNull
				       ? _nullString
				       : FormatValueCore(row, value);
		}

		[NotNull]
		protected virtual string FormatValueCore([NotNull] IRow row,
		                                         [NotNull] object rawValue)
		{
			return FormatFieldValue(rawValue);
		}

		[CanBeNull]
		protected abstract object ReadValueCore([NotNull] IRow row);

		[NotNull]
		protected static string FormatFieldValue([NotNull] object value)
		{
			return value is string
				       ? $"'{value}'"
				       : string.Format(CultureInfo.InvariantCulture, "{0}", value);
		}

		private void AssertExpectedTable([NotNull] IRow row)
		{
			if (Table == row.Table)
			{
				return;
			}

			throw new AssertionException(
				string.Format("Row is from unexpected table: {0} - expected: {1}",
				              DatasetUtils.GetName(row.Table),
				              DatasetUtils.GetName(Table)));
		}
	}
}