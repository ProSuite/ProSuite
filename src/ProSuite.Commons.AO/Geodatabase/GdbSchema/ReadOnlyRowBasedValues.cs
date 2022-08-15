using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class ReadOnlyRowBasedValues : IValueList
	{
		protected IReadOnlyRow BaseRow { get; }

		public ReadOnlyRowBasedValues([NotNull] IReadOnlyRow row)
		{
			BaseRow = row;
		}

		/// <summary>
		/// A bonus value that can be optionally added to the values provided by the base row.
		/// It will always be at the last index, i.e. after the values provided by the
		/// <see cref="BaseRow"/>.
		/// </summary>
		public object ExtraValue { get; set; }

		public object GetValue(int index, bool ensureRcwRefCountIncrease = false)
		{
			if (ExtraValue != null && index == BaseRow.Table.Fields.FieldCount)
			{
				return ExtraValue;
			}

			return BaseRow.get_Value(index);
		}

		public void SetValue(int fieldIndex, object value)
		{
			object sourceValue = BaseRow.get_Value(fieldIndex) ?? DBNull.Value;

			if (! sourceValue.Equals(value))
			{
				throw new InvalidOperationException("Cannot update read-only row");
			}
		}

		public bool HasValue(int index)
		{
			return true;
		}
	}
}
