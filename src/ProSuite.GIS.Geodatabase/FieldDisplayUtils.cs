using System;
using System.Collections.Generic;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase
{
	public static class FieldDisplayUtils
	{
		public static string GetDefaultRowFormat([NotNull] IObjectClass objectClass,
		                                         bool includeClassAlias = false)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			int subtypeFieldIndex = DatasetUtils.GetSubtypeFieldIndex(objectClass);

			var sb = new StringBuilder();

			if (includeClassAlias)
			{
				sb.AppendFormat("{0} - ", DatasetUtils.GetAliasName(objectClass));
			}

			if (subtypeFieldIndex >= 0)
			{
				string subtypeFieldName =
					objectClass.Fields.Field[subtypeFieldIndex].Name;
				sb.AppendFormat("{{{0}}} - ", subtypeFieldName);
			}

			string displayField = StringUtils.IsNotEmpty(objectClass.OIDFieldName)
				                      ? objectClass.OIDFieldName
				                      : SelectDisplayField(objectClass);

			if (displayField != null)
			{
				sb.AppendFormat("{{{0}}}", displayField);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Tries the get a name field from an object class. If a field called "NAME" exists,
		/// that field is returned, otherwise the first field ending with "NAME" (if any) is
		/// returned.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="fieldName">The field name of the name field, if found.</param>
		/// <returns><c>true</c> if a name field was found, <c>false</c> otherwise.</returns>
		public static bool TryGetNameField([NotNull] IObjectClass objectClass,
		                                   out string fieldName)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return TryGetNameField(DatasetUtils.GetFields(objectClass), out fieldName);
		}

		private static bool TryGetNameField([NotNull] IEnumerable<IField> fields,
		                                    out string fieldName)
		{
			const string keyword = "NAME";
			string partialMatch = null;

			foreach (IField field in fields)
			{
				if (field.Type != esriFieldType.esriFieldTypeString)
				{
					continue;
				}

				if (field.Name.Equals(keyword, StringComparison.OrdinalIgnoreCase))
				{
					fieldName = field.Name;
					return true;
				}

				if (field.Name.EndsWith(keyword, StringComparison.OrdinalIgnoreCase))
				{
					partialMatch = field.Name;
				}
			}

			if (partialMatch != null)
			{
				fieldName = partialMatch;
				return true;
			}

			fieldName = string.Empty;
			return false;
		}

		[CanBeNull]
		private static string SelectDisplayField([NotNull] IObjectClass objectClass)
		{
			IList<IField> fields = DatasetUtils.GetFields(objectClass);

			string fieldName;
			if (TryGetNameField(fields, out fieldName))
			{
				return fieldName;
			}

			// no name field - use the first text field, if any

			foreach (IField field in fields)
			{
				if (field.Type == esriFieldType.esriFieldTypeString)
				{
					return field.Name;
				}
			}

			// no text field found - use the first field of any (displayable) type

			foreach (IField field in fields)
			{
				switch (field.Type)
				{
					case esriFieldType.esriFieldTypeSmallInteger:
					case esriFieldType.esriFieldTypeInteger:
					case esriFieldType.esriFieldTypeSingle:
					case esriFieldType.esriFieldTypeDouble:
					case esriFieldType.esriFieldTypeString:
					case esriFieldType.esriFieldTypeDate:
					case esriFieldType.esriFieldTypeOID:
					case esriFieldType.esriFieldTypeGUID:
					case esriFieldType.esriFieldTypeGlobalID:
						return field.Name;
				}
			}

			return null;
		}
	}
}
