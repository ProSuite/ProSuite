using System;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Value list that ensures correct RCW reference count by storing EVERY value in a
	/// property set. This implementation is relatively slow due to the creation of the
	/// PropertySet for each row.
	/// </summary>
	public class PropertySetValueList : IValueList
	{
		// Using property set to support the same COM-release calls (e.g. on GdbFeature.Shape) as
		// on a real feature. Otherwise InvalidComObjectException can happen when accessing the same
		// object again (e.g. by calling GdbFeature.Shape again)
		private IPropertySet ValueSet { get; } = new PropertySet();

		public object GetValue(int index, bool increaseRcwRefCount = false)
		{
			var name = Convert.ToString(index);

			if (increaseRcwRefCount)
			{
				// Make sure that the marshal-reference-count is increased by exactly 1,
				// i.e. do not call PropertySetUtils.HasProperty
				return ValueSet.GetProperty(name);
			}

			var result = PropertySetUtils.HasProperty(ValueSet, name)
				             ? ValueSet.GetProperty(name)
				             : null;

			return result;
		}

		public void SetValue(int index, object value)
		{
			if (value == null)
			{
				value = DBNull.Value;
			}

			ValueSet.SetProperty(Convert.ToString(index), value);
		}

		public bool HasValue(int index)
		{
			string name = Convert.ToString(index);

			return PropertySetUtils.HasProperty(ValueSet, name);
		}
	}
}
