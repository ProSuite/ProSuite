using System;
using System.Threading;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Value list without COM-interop performance overhead.
	/// For getting the geometry (or other com objects) the reference count of the RCW can
	/// be enforced to be increased to allow for consistent behaviour w.r.t. Marshal.ReleaseComObject.
	/// </summary>
	public class ValueList : IValueList
	{
		private readonly object[] _values;

		private static readonly ThreadLocal<IPropertySet> _comContainer =
			new ThreadLocal<IPropertySet>(() => new PropertySetClass());

		public ValueList(int valueCount)
		{
			_values = new object[valueCount];
		}

		public object GetValue(int index, bool increaseRcwRefCount = false)
		{
			var result = HasValue(index)
				             ? _values[index]
				             : null;

			if (increaseRcwRefCount && result != null)
			{
				result = IncrementRcwReferenceCount(result);
			}

			return result;
		}

		public void SetValue(int index, object value)
		{
			if (value == null)
			{
				value = DBNull.Value;
			}

			_values[index] = value;
		}

		public bool HasValue(int index)
		{
			return _values[index] != null;
		}

		private static object IncrementRcwReferenceCount(object result)
		{
			const string key = "name";

			_comContainer.Value.SetProperty(key, result);

			result = _comContainer.Value.GetProperty(key);

			return result;
		}
	}
}
