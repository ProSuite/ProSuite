using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
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
			[NotNull] ITable table, [NotNull] IQueryFilter queryFilter)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));

			int nameFieldIndex = GetRequiredFieldIndex(table, "ATTRIBUTE");

			int typeFieldIndex = GetRequiredFieldIndex(table,
			                                           "FIELDTYPE_ARCGIS",
			                                           "ARCGIS_FIELDTYPE");

			int oracleTypeFieldIndex = GetRequiredFieldIndex(table,
			                                                 "FIELDTYPE_ORACLE",
			                                                 "ORACLE_FIELDTYPE");

			int aliasFieldIndex = GetRequiredFieldIndex(table, "ALIAS");

			foreach (IRow row in GdbQueryUtils.GetRows(table, queryFilter, recycle: true))
			{
				var name = row.Value[nameFieldIndex] as string;
				if (string.IsNullOrEmpty(name))
				{
					continue;
				}

				var typeString = row.Value[typeFieldIndex] as string;
				Assert.NotNull(typeString, "Undefined value in field {0} (for ATTRIBUTE={1})",
				               row.Fields.Field[typeFieldIndex].Name, name);
				esriFieldType expectedType = GetFieldType(typeString);

				var oracleTypeString = row.Value[oracleTypeFieldIndex] as string;
				Assert.NotNull(oracleTypeString, "Undefined value in field {0} (for ATTRIBUTE={1})",
				               row.Fields.Field[oracleTypeFieldIndex].Name, name);

				const string oracleTextType = "NVARCHAR2";
				int length = expectedType != esriFieldType.esriFieldTypeString
					             ? -1
					             : GetTextLength(oracleTypeString, oracleTextType);

				var alias = row.Value[aliasFieldIndex] as string;

				yield return new FieldSpecification(name, expectedType, length, alias, null, true);
			}
		}

		private static int GetRequiredFieldIndex([NotNull] ITable table,
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

			string name = DatasetUtils.GetName(table);
			throw new InvalidConfigurationException(
				fieldNames.Length == 1
					? $"Field '{fieldNames[0]}' does not exist in table {name}"
					: $"None of the fields {StringUtils.Concatenate(fieldNames, ",")} exists in table {name}");
		}

		private static int GetTextLength([NotNull] string typeString, [NotNull] string typePrefix)
		{
			string prefix = $"{typePrefix.ToUpper()}(";

			string trimmed = typeString.ToUpper()
			                           .Replace(" ", string.Empty)
			                           .Replace(prefix, string.Empty)
			                           .Replace(")", string.Empty)
			                           .Trim();

			if (! int.TryParse(trimmed, out int result))
			{
				throw new InvalidConfigurationException(
					$"Unable to parse text length from string '{typeString}'");
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
