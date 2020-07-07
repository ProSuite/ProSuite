using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public static class CoercionUtility
	{
		[CanBeNull]
		public static object Coerce([NotNull] IPropertyAccessor property,
		                            [CanBeNull] object rawValue)
		{
			Type propertyType = GetUnderlyingType(property);

			return Coerce(propertyType, rawValue);
		}

		[CanBeNull]
		public static object Coerce([NotNull] Type propertyType,
		                            [CanBeNull] object rawValue)
		{
			if (rawValue == null || rawValue.Equals(string.Empty))
			{
				return null;
			}

			if (propertyType.IsEnum)
			{
				return Enum.Parse(propertyType, rawValue.ToString());
			}

			if (propertyType == typeof(Guid))
			{
				var stringValue = rawValue as string;
				if (stringValue != null)
				{
					return new Guid(stringValue);
				}
			}

			return Convert.ChangeType(rawValue, propertyType);
		}

		[NotNull]
		public static Type GetUnderlyingType([NotNull] IPropertyAccessor property)
		{
			Type propertyType = property.PropertyType;

			if (propertyType.IsGenericType)
			{
				if (Equals(propertyType.GetGenericTypeDefinition(), (typeof(Nullable<>))))
				{
					propertyType = propertyType.GetGenericArguments()[0];
				}
			}

			return propertyType;
		}

		public static int? CoerceToPositiveInteger([CanBeNull] string textValue)
		{
			if (string.IsNullOrEmpty(textValue))
			{
				return null;
			}

			int returnValue;
			if (int.TryParse(textValue, out returnValue))
			{
				if (returnValue >= 0)
				{
					return returnValue;
				}
			}

			return null;
		}

		public static bool IsNumeric([NotNull] Type type)
		{
			var numericTypes = new object[]
			                   {
				                   typeof(double),
				                   typeof(float),
				                   typeof(long),
				                   typeof(int),
				                   typeof(short),
				                   typeof(decimal)
			                   };

			return Array.IndexOf(numericTypes, type) > -1;
		}
	}
}
