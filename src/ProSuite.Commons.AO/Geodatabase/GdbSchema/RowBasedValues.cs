using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class RowBasedValues : IValueList
	{
		private readonly IRow _row;
		private readonly int _oidFieldIndex;

		public RowBasedValues(IRow row, int oidFieldIndex)
		{
			_row = row;
			_oidFieldIndex = oidFieldIndex;
		}

		public object GetValue(int index, bool increaseRcwRefCount = false)
		{
			return _row.Value[index];
		}

		public void SetValue(int index, object value)
		{
			if (index == _oidFieldIndex)
			{
				// In the normal GdbRow constructor, the OID field value is set
				// which is not allowed on a real row.
				return;
			}

			// Important in case it is a read-only field (such as the OID), compare values first:
			// Attention: long object != int object, even if the values are the same!
			object sourceValue = _row.Value[index];

			if (! FieldUtils.AreValuesEqual(sourceValue, value))
			{
				_row.Value[index] = value;
			}
		}

		public bool HasValue(int index)
		{
			return true;
		}
	}
}
