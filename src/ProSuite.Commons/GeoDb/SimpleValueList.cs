namespace ProSuite.Commons.GeoDb
{
	/// <summary>
	/// The most basic value list implementation with no regards to COM interop.
	/// </summary>
	public class SimpleValueList : IValueList
	{
		private readonly object[] _values;

		public SimpleValueList(int valueCount)
		{
			_values = new object[valueCount];
		}

		#region Implementation of IValueList

		public object GetValue(int index, bool increaseRcwRefCount = false)
		{
			return _values[index];
		}

		public void SetValue(int index, object value)
		{
			_values[index] = value;
		}

		public bool HasValue(int index)
		{
			return _values[index] != null;
		}

		#endregion
	}
}
