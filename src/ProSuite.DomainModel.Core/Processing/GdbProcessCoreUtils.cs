using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.Text;

namespace ProSuite.DomainModel.Core.Processing
{
	public static class GdbProcessCoreUtils
	{
		public static bool IsObsolete([NotNull] Type processType, out string message)
		{
			return ReflectionUtils.IsObsolete(processType, out message);
		}

		/// <summary>
		/// Get the process's description (as specified with the Doc attribute).
		/// </summary>
		[NotNull]
		public static string GetProcessDescription(Type processType)
		{
			return ReflectionUtils.GetDescription(processType) ?? string.Empty;
		}

		#region Process parameters

		public static bool IsProcessParameter(PropertyInfo property)
		{
			if (property == null) return false;

			// Process parameters are public get/set properties with the Parameter attribute:
			// TODO - How to check for public?
			return property.CanRead && property.CanWrite &&
			       property.IsDefined(typeof(ParameterAttribute), false);
		}

		public static IList<PropertyInfo> GetProcessParameters(Type type)
		{
			// TODO Merge this method with GdbProcessUtils.GetProcessParameters (once possible)

			if (type == null) return Array.Empty<PropertyInfo>();

			// TODO check if GdbProcessUtils.IsGdbProcessType(type)

			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			           .Where(IsProcessParameter)
			           .OrderBy(GetParameterOrder)
			           .ToList();
		}

		public static int GetParameterOrder([CanBeNull] ICustomAttributeProvider property)
		{
			return GetParameterAttribute(property)?.Order ?? int.MaxValue;
		}

		public static string GetParameterGroup([CanBeNull] ICustomAttributeProvider property)
		{
			return GetParameterAttribute(property)?.Group ?? string.Empty;
		}

		[NotNull]
		public static string GetParameterDisplayType([NotNull] PropertyInfo property)
		{
			ParameterAttribute attr = GetParameterAttribute(property);
			if (attr != null && ! string.IsNullOrEmpty(attr.DisplayType))
			{
				return attr.DisplayType;
			}

			// Translate a few common types to "user friendly" names:

			if (property.PropertyType == typeof(bool))
			{
				return "Boolean";
			}

			if (property.PropertyType == typeof(int))
			{
				return "Integer";
			}

			if (property.PropertyType == typeof(double))
			{
				return "Number";
			}

			if (property.PropertyType == typeof(string))
			{
				return "String";
			}

			// All other types use their technical name:

			return property.PropertyType.Name;
		}

		[NotNull]
		public static string GetParameterInfoRTF([NotNull] PropertyInfo property)
		{
			Assert.ArgumentNotNull(property, nameof(property));

			string displayType = GetParameterDisplayType(property);
			string description = GetParameterDescription(property);

			var rtf = new RichTextBuilder();
			rtf.FontSize(8);
			rtf.Bold(property.Name).Text(" (").Text(displayType).Text(")");
			rtf.LineBreak();
			rtf.Text(description);

			return rtf.ToRtf();
		}

		/// <summary>
		/// Get the parameter's description (as specified with the Doc attribute).
		/// </summary>
		[NotNull]
		public static string GetParameterDescription([NotNull] PropertyInfo property)
		{
			string raw = ReflectionUtils.GetDescription(property) ?? string.Empty;

			return DescriptionPlaceholderRegex.Replace(
				raw, m => ExpandDescriptionPlaceholder(m, property));
		}

		private static readonly Regex DescriptionPlaceholderRegex =
			new Regex(@"{{\s*([A-Za-z0-9_. ]+)\s*}}");

		private static string ExpandDescriptionPlaceholder(Match match,
		                                                   PropertyInfo property)
		{
			string text = match.Groups[1].Value;

			// recognize: [parameter.]Name, [parameter.]Type, [parameter.]Values

			string[] parts = Regex.Split(text, @"\s*\.\s*");
			string key = null;

			if (parts.Length == 2 && parts[0] == "parameter")
				key = parts[1].Trim();
			else if (parts.Length == 1)
				key = parts[0].Trim();

			switch (key)
			{
				case "Name":
					return property.Name;
				case "Type":
					return property.PropertyType.Name;
				case "Values":
					return GetParameterDisplayValues(property) ?? match.Value;
			}

			return match.Value; // do not expand
		}

		[CanBeNull]
		private static ParameterAttribute GetParameterAttribute(
			[CanBeNull] ICustomAttributeProvider provider)
		{
			if (provider == null)
			{
				return null;
			}

			const bool inherit = true;
			object[] attributes = provider.GetCustomAttributes(
				typeof(ParameterAttribute), inherit);

			// ParameterAttribute has AttributeUsage AllowMultiple=false,
			// so the first ParameterAttribute we find will be the only one.
			return attributes.OfType<ParameterAttribute>().FirstOrDefault();
		}

		[CanBeNull]
		private static string GetParameterDisplayValues(PropertyInfo property)
		{
			if (property != null)
			{
				if (property.PropertyType == typeof(bool))
				{
					return "False, True";
				}

				if (property.PropertyType.IsEnum)
				{
					return string.Join(", ", Enum.GetNames(property.PropertyType));
				}
			}

			return null; // cannot enumerate values
		}

		#endregion
	}
}
