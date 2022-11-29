using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// The Carto Processing stuff that does not fit anywhere else...
	/// My be eventually moved to other/better places...
	/// </summary>
	public static class ProcessingUtils
	{
		public static IList<Type> GetDerivedTypes<T>()
		{
			var assembly = Assembly.GetAssembly(typeof(T));
			return GetDerivedTypes<T>(assembly);
		}

		public static IList<Type> GetDerivedTypes<T>(Assembly assembly)
		{
			var baseType = typeof(T);
			return assembly.GetTypes()
			               .Where(t => t != baseType && baseType.IsAssignableFrom(t))
			               .ToList();
		}

		public static int GetLineNumber(this XObject x)
		{
			return x is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 0;
		}
	}
}
