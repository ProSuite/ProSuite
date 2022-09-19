using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class FieldColumnInfo : ColumnInfo
	{
		private readonly int _fieldIndex;

		public static FieldColumnInfo Create(IReadOnlyTable table, string fieldName)
		{
			int fieldIndex = table.FindField(fieldName);
			if (fieldIndex < 0)
			{
				throw new InvalidOperationException(
					$"Unknown field {fieldName} in table {table.Name}");
			}
			IField field = table.Fields.Field[fieldIndex];
			return new FieldColumnInfo(table, field, fieldIndex);
		}

		public FieldColumnInfo([NotNull] IReadOnlyTable table,
		                       [NotNull] IField field,
		                       int fieldIndex)
			: base(table, field.Name.ToUpper(), TestUtils.GetColumnType(field))
		{
			Assert.ArgumentCondition(fieldIndex >= 0, "Invalid field index: {0}", fieldIndex);

			_fieldIndex = fieldIndex;
		}

		public override IEnumerable<string> BaseFieldNames => new[] {ColumnName};

		protected override object ReadValueCore(IReadOnlyRow row)
		{
			// TODO the table may be different in case of "undirected" MultiTableView usage - search index based on field name
			//int fieldIndex = row.Table != Table
			//					 ? row.Table.FindField(ColumnName)
			//					 : _fieldIndex;

			return row.get_Value(_fieldIndex);
		}

		protected override string FormatValueCore(IReadOnlyRow row, object rawValue)
		{
			string formattedRawValue = base.FormatValueCore(row, rawValue);

			var obj = row as IObject;

			if (obj == null)
			{
				// not an IObject, report value as is
				return formattedRawValue;
			}

			object displayValue = GdbObjectUtils.GetDisplayValue(obj, _fieldIndex);

			return Equals(displayValue, rawValue)
				       ? formattedRawValue
				       : $"{formattedRawValue} [{displayValue}]";
		}
	}
}
