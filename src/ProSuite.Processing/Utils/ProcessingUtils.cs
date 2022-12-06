using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Text;

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

		public static StringBuilder AppendScale(this StringBuilder sb, double scaleDenom, string sep = null)
		{
			if (sb == null) return null;

			if (scaleDenom > 0)
			{
				if (scaleDenom < 1 || scaleDenom > Math.Floor(scaleDenom))
				{
					// has decimals, just append using default format
					sb.AppendFormat("1:{0}", scaleDenom);
				}
				else
				{
					sb.Append("1:");

					// scale denom is an integer, append with separators
					const string digits = "0123456789";
					if (sep == null) sep = "\u2009"; // THIN SPACE

					int r = (int) Math.Floor(scaleDenom);

					if (r < 1)
					{
						throw new AssertionException("Bug");
					}

					int start = sb.Length;
					int n = 0;

					while (r > 0)
					{
						if (n > 0 && n % 3 == 0)
						{
							sb.Append(sep);
						}

						sb.Append(digits[r % 10]);

						r /= 10;
						n += 1;
					}

					sb.Reverse(start, sb.Length - start);
				}
			}
			else
			{
				sb.Append("none");
			}

			return sb;
		}
	}
}
