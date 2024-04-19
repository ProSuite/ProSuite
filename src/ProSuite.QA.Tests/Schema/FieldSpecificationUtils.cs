using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests.Schema
{
	internal static class FieldSpecificationUtils
	{
		[NotNull]
		public static IEnumerable<FieldSpecification> ReadFieldSpecifications(
			[NotNull] IReadOnlyTable table, [NotNull] ITableFilter queryFilter)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));

			int nameFieldIndex = GetFieldIndex(table, false, "FIELDNAME", "ATTRIBUTE");
			int typeFieldIndex = GetFieldIndex(table, false, "FIELDTYPE", "FIELDTYPE_ARCGIS");
			int lengthFieldIndex = GetFieldIndex(table, false, "FIELDLENGTH", "FIELDTYPE_ORACLE");
			int aliasFieldIndex = GetFieldIndex(table, true, "ALIASNAME", "ALIAS");

			foreach (IReadOnlyRow row in table.EnumRows(queryFilter, recycle: true))
			{
				var name = row.get_Value(nameFieldIndex) as string;
				if (string.IsNullOrEmpty(name))
				{
					continue;
				}

				var typeString = row.get_Value(typeFieldIndex) as string;
				Assert.NotNull(typeString, "Undefined value in field {0} (for FIELDNAME={1})",
				               table.Fields.Field[typeFieldIndex].Name, name);
				esriFieldType expectedType = GetFieldType(typeString);

				var lengthString = row.get_Value(lengthFieldIndex) as string;
				Assert.NotNull(lengthString, "Undefined value in field {0} (for FIELDNAME={1})",
				               table.Fields.Field[lengthFieldIndex].Name, name);

				int length = expectedType != esriFieldType.esriFieldTypeString
					             ? -1
					             : GetTextLength(lengthString);

				var alias = aliasFieldIndex == -1
					            ? null
					            : row.get_Value(aliasFieldIndex) as string;

				yield return new FieldSpecification(name, expectedType, length, alias, null, true);
			}
		}

		private static int GetFieldIndex([NotNull] IReadOnlyTable table, bool fieldIsOptional,
		                                 [NotNull] params string[] fieldNames)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentCondition(fieldNames.Length > 0,
			                         "At least one field name required", nameof(fieldNames));

			foreach (var fieldName in fieldNames)
			{
				var result = table.FindField(fieldName);

				if (result >= 0)
				{
					return result;
				}
			}

			if (fieldIsOptional)
			{
				return -1;
			}

			string name = table.Name;
			throw new InvalidConfigurationException(
				fieldNames.Length == 1
					? $"Field '{fieldNames[0]}' does not exist in table {name}"
					: $"None of the fields {StringUtils.Concatenate(fieldNames, ",")} exists in table {name}");
		}

		private static int GetTextLength([NotNull] string lengthString)
		{
			if (int.TryParse(lengthString, out int result))
			{
				return result;
			}

			//LEGACY
			const string oracleTextType = "NVARCHAR2";

			string prefix = $"{oracleTextType.ToUpper()}(";

			string trimmed = lengthString.ToUpper()
			                             .Replace(" ", string.Empty)
			                             .Replace(prefix, string.Empty)
			                             .Replace(")", string.Empty)
			                             .Trim();

			if (! int.TryParse(trimmed, out result))
			{
				throw new InvalidConfigurationException(
					$"Unable to parse text length from string '{lengthString}'");
			}

			return result;
		}

		private static esriFieldType GetFieldType([NotNull] string fieldTypeString)
		{
			Assert.ArgumentNotNullOrEmpty(fieldTypeString, nameof(fieldTypeString));

			switch (fieldTypeString.ToUpper())
			{
				case "SHORT INTEGER":
				case "SHORTINTEGER":
					return esriFieldType.esriFieldTypeSmallInteger;

				case "LONG INTEGER":
				case "LONGINTEGER":
					return esriFieldType.esriFieldTypeInteger;

				case "FLOAT":
					return esriFieldType.esriFieldTypeSingle;

				case "DOUBLE":
					return esriFieldType.esriFieldTypeDouble;

				case "TEXT":
					return esriFieldType.esriFieldTypeString;

				case "DATE":
					return esriFieldType.esriFieldTypeDate;

				case "OBJECT ID":
				case "OBJECTID":
					return esriFieldType.esriFieldTypeOID;

				case "GEOMETRY":
					return esriFieldType.esriFieldTypeGeometry;

				case "BLOB":
					return esriFieldType.esriFieldTypeBlob;

				case "RASTER":
					return esriFieldType.esriFieldTypeRaster;

				case "GUID":
					return esriFieldType.esriFieldTypeGUID;

				case "GLOBAL ID":
				case "GLOBALID":
					return esriFieldType.esriFieldTypeGlobalID;

				case "XML":
					return esriFieldType.esriFieldTypeXML;
			}

			if (EnumUtils.TryParse(fieldTypeString, true, out esriFieldType result))
			{
				return result;
			}

			throw new ArgumentException(
				$@"Unsupported field type string: {fieldTypeString}", nameof(fieldTypeString));
		}
	}
}
