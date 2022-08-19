using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class RowBasedValues : IValueList
	{
		private readonly IRow _row;

		public RowBasedValues(IRow row)
		{
			_row = row;
		}

		public object GetValue(int index, bool increaseRcwRefCount = false)
		{
			return _row.Value[index];
		}

		public void SetValue(int index, object value)
		{
			_row.Value[index] = value;
		}

		public bool HasValue(int index)
		{
			return true;
		}
	}
}
