using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Processing.Domain;
using ProSuite.Processing.Evaluation;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// The Carto Processing stuff that does not fit anywhere else...
	/// My be eventually moved to other/better places...
	/// </summary>
	public static class ProcessingUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static IList<Type> GetExportedDerivedTypes<T>(Assembly assembly)
		{
			var baseType = typeof(T);

			try
			{
				return assembly.GetExportedTypes()
				               .Where(t => t != baseType && baseType.IsAssignableFrom(t))
				               .ToList();
			}
			catch (Exception ex)
			{
				_msg.Error($"{nameof(GetExportedDerivedTypes)}: {ex.Message}", ex);

				return Array.Empty<Type>();
			}
		}

		public static IList<ParameterInfo> GetParameters(Type processType)
		{
			// TODO check if processType is a valid CP type

			if (processType == null) return Array.Empty<ParameterInfo>();

			return processType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			                  .Where(IsProcessParameter)
			                  .Select(GetParameterInfo)
			                  .OrderBy(info => info.Order)
			                  .ToList();
		}

		private static ParameterInfo GetParameterInfo(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException(nameof(property));

			var attr = property.GetCustomAttributes<ParameterAttribute>()
			                   .FirstOrDefault();

			var name = property.Name;
			var type = property.PropertyType;
			var required = attr?.Required ?? false;
			var multivalued = attr?.Multivalued ?? false;
			var order = attr?.Order ?? 0;

			var className = property.DeclaringType?.Name ?? "?";
			var docKey = $"{className}_{property.Name}";

			if (type.IsArray && type.HasElementType)
			{
				type = type.GetElementType() ?? type;
				multivalued = true;
			}

			return new ParameterInfo(name, type, required, multivalued,
			                             attr?.Group, order, docKey);
		}

		private static bool IsProcessParameter(PropertyInfo property)
		{
			if (property == null) return false;
			// Carto Process parameters are public get/set properties
			// with the Parameter attribute:
			// TODO How to check for public? Caller's task...
			return property.CanRead && property.CanWrite &&
			       property.IsDefined(typeof(ParameterAttribute), false);
		}

		public static int GetLineNumber(this XObject x)
		{
			return x is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 0;
		}

		public static ImplicitValue<T> GetExpression<T>(
			this CartoProcessConfig config, string parameterName)
		{
			var text = config.GetValue<string>(parameterName);
			return ((ImplicitValue<T>) text).SetName(parameterName);
		}

		public static ImplicitValue<T> GetExpression<T>(
			this CartoProcessConfig config, string parameterName, string defaultValue)
		{
			var text = config.GetValue(parameterName, defaultValue);
			return ((ImplicitValue<T>) text)?.SetName(parameterName);
		}

		public static ImplicitValue<double> GetExpression(
			this CartoProcessConfig config, string parameterName, double defaultValue)
		{
			var text = config.GetValue<string>(parameterName, null);
			var expr = (ImplicitValue<double>) text ?? ImplicitValue<double>.Literal(defaultValue);
			return expr.SetName(parameterName);
		}

		public static ImplicitValue<bool> GetExpression(
			this CartoProcessConfig config, string parameterName, bool defaultValue)
		{
			var text = config.GetValue<string>(parameterName, null);
			var expr = (ImplicitValue<bool>) text ?? ImplicitValue<bool>.Literal(defaultValue);
			return expr.SetName(parameterName);
		}

		public static ImplicitValue<int> GetExpression(
			this CartoProcessConfig config, string parameterName, int defaultValue)
		{
			var text = config.GetValue<string>(parameterName, null);
			var expr = (ImplicitValue<int>) text ?? ImplicitValue<int>.Literal(defaultValue);
			return expr.SetName(parameterName);
		}

		public static FieldSetter GetFieldSetter(
			this CartoProcessConfig config, string parameterName, string defaultValue)
		{
			var text = config.GetJoined(parameterName, "; ");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = defaultValue;
			}

			return new FieldSetter(text, parameterName);
		}

		public static FieldSetter GetFieldSetter(
			this CartoProcessConfig config, string parameterName)
		{
			var text = config.GetJoined(parameterName, "; ");
			if (string.IsNullOrWhiteSpace(text))
			{
				throw new CartoConfigException($"Required parameter {parameterName} is missing");
			}

			return new FieldSetter(text, parameterName);
		}
		
		public static StringBuilder AppendSRef(this StringBuilder sb, string name, int wkid = 0)
		{
			if (name != null && wkid > 0)
			{
				sb.AppendFormat("{0} SRID {1}", name, wkid);
				return sb;
			}

			if (name != null)
			{
				sb.Append(name);
				return sb;
			}

			if (wkid > 0)
			{
				sb.AppendFormat("SRID {0}", wkid);
				return sb;
			}

			sb.Append("no sref");
			return sb;
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
