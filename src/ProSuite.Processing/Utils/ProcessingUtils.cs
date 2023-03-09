using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
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

		public static ParameterInfo GetParameter(Type processType, string parameterName)
		{
			var list = GetParameters(processType);
			return list.FirstOrDefault(
				       p => string.Equals(p.Name, parameterName, StringComparison.Ordinal)) ??
			       list.FirstOrDefault(
				       p => string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));
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

			object defaultValue = GetDefaultValue(property);

			var owner = property.ReflectedType ?? property.DeclaringType;

			return new ParameterInfo(owner, name, type, required, multivalued,
			                         defaultValue, docKey: docKey,
			                         group: attr?.Group, order: order);
		}

		private static object GetDefaultValue(MemberInfo property)
		{
			var paramAttr = property.GetCustomAttributes<OptionalParameterAttribute>()
			                        .FirstOrDefault();
			if (paramAttr?.DefaultValue != null)
			{
				return paramAttr.DefaultValue;
			}

			var valueAttr = property.GetCustomAttributes<DefaultValueAttribute>()
			                        .FirstOrDefault();
			return valueAttr?.Value;
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

		public static string GetFriendlyName(Type type)
		{
			if (type is null) return "null";
			if (type == typeof(bool)) return "Boolean";
			if (type == typeof(int)) return "Integer";
			if (type == typeof(double)) return "Number";
			if (type == typeof(string)) return "String";
			if (type == typeof(ProcessDatasetName)) return "LayerName [; WhereClause]";
			if (type == typeof(FieldSetter)) return "Field=Value assignments";

			Type[] typeArgs;
			if (type.IsGenericType &&
			    type.GetGenericTypeDefinition() == typeof(ImplicitValue<>) &&
			    (typeArgs = type.GetGenericArguments()).Length == 1)
			{
				if (typeArgs[0] == typeof(object))
				{
					return "Expression";
				}

				var inner = GetFriendlyName(typeArgs[0]);
				return $"{inner} Expression";
			}

			if (type.IsEnum)
			{
				// return "Foo|Bar|Baz"
				const char sep = '|';
				const int maxLength = 120;
				var names = type.GetEnumNames();
				var sb = new StringBuilder();
				foreach (var name in names)
				{
					if (sb.Length + name.Length > maxLength)
					{
						if (sb.Length > 0) sb.Append(sep);
						sb.Append("...");
						break;
					}
					if (sb.Length > 0) sb.Append(sep);
					sb.Append(name);
				}
				return sb.ToString();
			}

			// all other types use their technical name:
			return type.Name;
		}

		public static int GetLineNumber(this XObject x)
		{
			return x is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 0;
		}

		#region Config utils

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

		#endregion

		#region Text utils

		[CanBeNull]
		public static string Canonical([CanBeNull] this string text)
		{
			if (text is null) return null;
			text = text.Trim();
			return text.Length < 1 ? null : text;
		}

		[NotNull]
		public static StringBuilder AppendSRef(this StringBuilder sb, string name, int wkid = 0)
		{
			if (sb is null)
				throw new ArgumentNullException(nameof(sb));

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

		[NotNull]
		public static StringBuilder AppendScale(this StringBuilder sb, double scaleDenom, string sep = null)
		{
			if (sb is null)
				throw new ArgumentNullException(nameof(sb));

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

		[CanBeNull]
		public static int[] ParseIntegerList([CanBeNull] string text, char separator)
		{
			if (text == null) return null;

			text = text.Trim();
			if (text.Length < 1) return null;

			string[] parts = text.Split(separator);
			if (parts.Length < 1) return null;

			const NumberStyles numberStyle = NumberStyles.Integer;
			CultureInfo invariant = CultureInfo.InvariantCulture;

			var list = parts.Select(s => s.Canonical()).Where(s => s != null)
			                .Select(s => int.Parse(s, numberStyle, invariant))
			                .ToList();

			return list.Count > 0 ? list.ToArray() : null;
		}

		#endregion

		#region Numeric utils

		/// <remarks>A number is finite if it is not NaN and not infinity</remarks>
		public static bool IsFinite(this double number)
		{
			return !double.IsNaN(number) && !double.IsInfinity(number);
		}

		public static double Clamp(this double value, double min, double max, string name = null)
		{
			if (value < min)
			{
				if (!string.IsNullOrEmpty(name))
				{
					_msg.WarnFormat("{0} was {1}, clamped to {2}", name, value, min);
				}

				return min;
			}

			if (value > max)
			{
				if (!string.IsNullOrEmpty(name))
				{
					_msg.WarnFormat("{0} was {1}, clamped to {2}", name, value, max);
				}

				return max;
			}

			return value;
		}

		public static int Clamp(this int value, int min, int max, string name = null)
		{
			Debug.Assert(min < max);

			if (value < min)
			{
				if (!string.IsNullOrEmpty(name))
				{
					_msg.WarnFormat("{0} was {1}, clamped to {2}", name, value, min);
				}

				return min;
			}

			if (value > max)
			{
				if (!string.IsNullOrEmpty(name))
				{
					_msg.WarnFormat("{0} was {1}, clamped to {2}", name, value, max);
				}

				return max;
			}

			return value;
		}

		/// <summary>
		/// Normalize the given <paramref name="angle"/> (in degrees)
		/// so that it is in the range 0 (inclusive) to 360 (exclusive).
		/// </summary>
		/// <param name="angle">in degrees</param>
		/// <returns>angle, in degrees, normalized to 0..360</returns>
		public static double ToPositiveDegrees(double angle)
		{
			angle %= 360;

			if (angle < 0)
			{
				angle += 360;
			}

			return angle;
		}

		/// <summary>
		/// Normalize the given <paramref name="angle"/> (in radians)
		/// so that it is in the range -pi to pi (both inclusive).
		/// </summary>
		/// <param name="angle">in radians</param>
		/// <returns>angle, in radians, normalized to -pi..pi</returns>
		public static double NormalizeRadians(double angle)
		{
			const double twoPi = Math.PI * 2;

			angle %= twoPi; // -2pi .. 2pi

			if (angle > Math.PI)
			{
				angle -= twoPi;
			}
			else if (angle < -Math.PI)
			{
				angle += twoPi;
			}

			return angle; // -pi .. pi
		}

		#endregion

		/// <summary>
		/// Get the subtype code that would be assigned by the given field setter.
		/// Return -1 if the subtype field would remain unassigned.
		/// Pass the same environment that will be used for the real assignment;
		/// of no environment is passed, the null environment is used internally.
		/// </summary>
		public static int GetSubtypeCode(FieldSetter attributes, string subtypeFieldName,
		                                 IEvaluationEnvironment env = null, Stack<object> stack = null)
		{
			object value = attributes?.GetFieldValue(subtypeFieldName, env, stack);
			if (value is null) return -1;

			try
			{
				return Convert.ToInt32(value);
			}
			catch
			{
				return -1;
			}

			// Usually, the subtype assigned with FieldSetter is
			// given as a literal value, so we could just parse:
			//return int.TryParse(assignment.Value.Clause, out int code) ? code : -1;
			// But only a full evaluation is guaranteed to always yield the proper result.
		}
	}
}
