using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO
{
	public static class PropertySetUtils
	{
		[NotNull]
		public static IPropertySet Clone([NotNull] IPropertySet propertySet)
		{
			Assert.ArgumentNotNull(propertySet, nameof(propertySet));

			return (IPropertySet) ((IClone) propertySet).Clone();
		}

		[NotNull]
		public static IDictionary<string, object> GetDictionary(
			[NotNull] IPropertySet propertySet)
		{
			Assert.ArgumentNotNull(propertySet, nameof(propertySet));

			var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			object objkeys;
			object objvalues;
			propertySet.GetAllProperties(out objkeys, out objvalues);

			var keys = (string[]) objkeys;
			var vals = (object[]) objvalues;

			for (int i = 0; i < keys.Length; i++)
			{
				string key = keys[i];
				object val = vals[i];

				result.Add(key, val);
			}

			return result;
		}

		[NotNull]
		public static IPropertySet GetPropertySet(
			[NotNull] IDictionary<string, object> propertyMap)
		{
			Assert.ArgumentNotNull(propertyMap, nameof(propertyMap));

			IPropertySet propertySet = new PropertySet();

			foreach (KeyValuePair<string, object> keyValuePair in propertyMap)
			{
				propertySet.SetProperty(keyValuePair.Key, keyValuePair.Value);
			}

			return propertySet;
		}

		[CanBeNull]
		public static object GetValue([NotNull] IPropertySet propertySet,
		                              [NotNull] string propertyName)
		{
			Assert.ArgumentNotNull(propertySet, nameof(propertySet));
			Assert.ArgumentNotNull(propertyName, nameof(propertyName));

			// Background: props.GetProperty(name) throws exception if property is not applicable

			object objkeys;
			object objvalues;
			propertySet.GetAllProperties(out objkeys, out objvalues);

			var keys = (string[]) objkeys;
			var vals = (object[]) objvalues;

			for (int i = 0; i < keys.Length; i++)
			{
				string key = keys[i];
				if (key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
				{
					return vals[i];
				}
			}

			// not found
			return null;
		}

		/// <summary>
		/// Gets the string value for a given property name in a property dictionary.
		/// </summary>
		/// <param name="propertyMap">The property dictionary.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>The property value converted to a string. 
		/// An empty string is returned if the property does not exist or has a value of null (never return null).</returns>
		[NotNull]
		public static string GetStringValue(
			[NotNull] IDictionary<string, object> propertyMap,
			[NotNull] string propertyName)
		{
			Assert.ArgumentNotNull(propertyMap, nameof(propertyMap));
			Assert.ArgumentNotNull(propertyName, nameof(propertyName));

			object value;
			if (! propertyMap.TryGetValue(propertyName, out value))
			{
				return string.Empty;
			}

			return value == null
				       ? string.Empty
				       : value.ToString();
		}

		/// <summary>
		/// Gets the string value for a given property name in a property set.
		/// </summary>
		/// <param name="propertySet">The property set.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>The property value converted to a string. 
		/// An empty string is returned if the property does not exist or has a value of null (never return null).</returns>
		[NotNull]
		public static string GetStringValue([NotNull] IPropertySet propertySet,
		                                    [NotNull] string propertyName)
		{
			Assert.ArgumentNotNull(propertySet, nameof(propertySet));
			Assert.ArgumentNotNull(propertyName, nameof(propertyName));

			object value = GetValue(propertySet, propertyName);

			return value == null
				       ? string.Empty
				       : value.ToString();
		}

		public static bool HasProperty([NotNull] IPropertySet propertySet,
		                               [NotNull] string propertyName)
		{
			Assert.ArgumentNotNull(propertySet, nameof(propertySet));
			Assert.ArgumentNotNull(propertyName, nameof(propertyName));

			object objkeys;
			object objvalues;
			propertySet.GetAllProperties(out objkeys, out objvalues);

			var keys = (string[]) objkeys;

			return keys.Any(key => key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
		}
	}
}
