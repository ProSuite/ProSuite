using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public abstract class CoordinateSystemProperties
	{
		[NotNull]
		protected static string GetParameterValue<T>([NotNull] Func<T> function)
		{
			T value;
			try
			{
				value = function();
			}
			catch (Exception)
			{
				return "n/a";
			}

			return value.ToString();
		}
	}
}
