using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons
{
	public static class EnumUtils
	{
		/// <summary>
		/// Gets the list of elements of a given enumeration type.
		/// </summary>
		/// <typeparam name="T">The enumeration type</typeparam>
		/// <returns>The list of all enumeration values</returns>
		public static IList<T> GetList<T>()
			where T : struct, IComparable, IFormattable
		{
			Array values = Enum.GetValues(typeof(T));

			var items = new List<T>(values.Length);

			for (var i = 0; i < values.Length; i++)
			{
				items.Add((T) values.GetValue(i));
			}

			return items;
		}

		/// <summary>
		/// Gets the list of elements of a given enumeration type.
		/// </summary>
		/// <typeparam name="T">The enumeration type</typeparam>
		/// <param name="excludedValue">A value to exclude from the list.</param>
		/// <returns>The list of all enumeration values except the one to exclude</returns>
		public static IList<T> GetList<T>(T excludedValue)
			where T : struct, IComparable, IFormattable
		{
			Array values = Enum.GetValues(typeof(T));

			var items = new List<T>(values.Length);

			for (var i = 0; i < values.Length; i++)
			{
				var item = (T) values.GetValue(i);

				if (! Equals(item, excludedValue))
				{
					items.Add(item);
				}
			}

			return items;
		}

		public static string GetName<T>(T value) where T : struct
		{
			return Enum.GetName(value.GetType(), value);
		}

		public static bool TryParse<T>([NotNull] string value, bool ignoreCase,
		                               out T result)
			where T : struct
		{
			// NOTE: Enum.TryParse() is only available since .Net 4.0

			// use IsDefined if not case-insensitive
			if (! ignoreCase && ! Enum.IsDefined(typeof(T), value))
			{
				result = default;
				return false;
			}

			try
			{
				result = (T) Enum.Parse(typeof(T), value, ignoreCase);
				return true;
			}
			catch (Exception)
			{
				result = default;
				return false;
			}
		}

		public static T Parse<T>(string codedValue)
			where T : struct, IComparable, IFormattable
		{
			T result;

			bool canParse = TryParse(codedValue, true, out result);

			if (! canParse)
			{
				throw new ArgumentOutOfRangeException(
					$"Unable to recognize value {codedValue}. It must be one of " +
					$"{StringUtils.Concatenate(GetList<T>(), ", ")}");
			}

			return result;
		}
	}
}
