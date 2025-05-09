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

		/// <summary>
		/// Gets or sets the value at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the value to get or set.</param>
		/// <returns>The value at the specified index.</returns>
		public object this[int index]
		{
			get => GetValue(index);
			set => SetValue(index, value);
		}

		/// <summary>
		/// Gets the number of elements in the value list.
		/// </summary>
		public int Count => _values.Length;

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
