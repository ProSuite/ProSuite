using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Lists
{
	internal class PropertyComparer<T> : IComparer<T>
	{
		// The following code is adapted code based on code that contains code implemented by Rockford Lhotka:
		// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnadvnet/html/vbnet01272004.asp

		private readonly ListSortDirection _direction;
		[NotNull] private readonly PropertyDescriptor _property;

		public PropertyComparer([NotNull] PropertyDescriptor property,
		                        ListSortDirection direction)
		{
			_property = property;
			_direction = direction;
		}

		#region IComparer<T>

		public int Compare(T xWord, T yWord)
		{
			// Get property values
			object xValue = null;
			object yValue = null;
			if (xWord != null)
			{
				xValue = GetPropertyValue(xWord, _property.Name);
			}

			if (yWord != null)
			{
				yValue = GetPropertyValue(yWord, _property.Name);
			}

			if ((xValue as Image)?.Tag != null)
			{
				xValue = ((Image) xValue).Tag;
			}

			if ((yValue as Image)?.Tag != null)
			{
				yValue = ((Image) yValue).Tag;
			}

			// Determine sort order
			return _direction == ListSortDirection.Ascending
				       ? CollectionUtils.CompareAscending(xValue, yValue)
				       : CollectionUtils.CompareDescending(xValue, yValue);
		}

		public bool Equals(T xWord, T yWord)
		{
			return xWord.Equals(yWord);
		}

		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}

		#endregion

		// Compare two property values of any type

		private static object GetPropertyValue([NotNull] T value, [NotNull] string property)
		{
			var type = value.GetType();

			// Get property
			PropertyInfo propertyInfo = type.GetProperty(property);
			Assert.NotNull(propertyInfo, "Property {0} not found in type {1}",
			               property, type);

			// Return value
			return propertyInfo.GetValue(value, null);
		}
	}
}
