using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
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
			[NotNull] ITable table,
			[NotNull] IQueryFilter queryFilter)
		{
			int nameFieldIndex = GetRequiredFieldIndex(table, "ATTRIBUTE");
			int typeStringFieldIndex = GetRequiredFieldIndex(table,
			                                                 "FIELDTYPE_ARCGIS",
			                                                 "ARCGIS_FIELDTYPE");
			int aliasFieldIndex = GetRequiredFieldIndex(table, "ALIAS");
			int accessTypeFieldIndex = GetRequiredFieldIndex(table,
			                                                 "FIELDTYPE_ACCESS",
			                                                 "ACCESS_FIELDTYPE");
			int oracleTypeFieldIndex = GetRequiredFieldIndex(table,
			                                                 "FIELDTYPE_ORACLE",
			                                                 "ORACLE_FIELDTYPE");

			IWorkspace workspace = DatasetUtils.GetWorkspace(table);
			bool isAccessWorkspace = WorkspaceUtils.IsPersonalGeodatabase(workspace);
			bool isSdeWorkspace = WorkspaceUtils.IsSDEGeodatabase(workspace);

			foreach (IRow row in GdbQueryUtils.GetRows(table, queryFilter, recycle : true))
			{
				var name = row.Value[nameFieldIndex] as string;

				if (string.IsNullOrEmpty(name))
				{
					continue;
				}

				object type = row.Value[typeStringFieldIndex];
				var typeString = type as string;

				if (string.IsNullOrEmpty(typeString))
				{
					throw new InvalidConfigurationException(
						$"Expected type is undefined: '{type}'");
				}

				var alias = row.Value[aliasFieldIndex] as string;
				esriFieldType expectedType = GetFieldType(typeString);

				var accessTypeString = row.Value[accessTypeFieldIndex] as string;
				var oracleTypeString = row.Value[oracleTypeFieldIndex] as string;

				Assert.NotNull(accessTypeString, "Undefined value in field {0}",
				               row.Fields.Field[accessTypeFieldIndex].Name);
				Assert.NotNull(oracleTypeString, "Undefined value in field {0}",
				               row.Fields.Field[oracleTypeFieldIndex].Name);

				int length = expectedType != esriFieldType.esriFieldTypeString
					             ? -1
					             : GetTextLength(accessTypeString,
					                             oracleTypeString,
					                             isAccessWorkspace,
					                             isSdeWorkspace);

				yield return new FieldSpecification(name, expectedType,
				                                    length, alias,
				                                    null,
				                                    true);
			}
		}

		private static int GetRequiredFieldIndex([NotNull] ITable table,
		                                         [NotNull] params string[] fieldNames)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentCondition(fieldNames.Length > 0,
			                         "At least one field name required",
			                         nameof(fieldNames));

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

		private static int GetTextLength([NotNull] string accessTypeString,
		                                 [NotNull] string oracleTypeString,
		                                 bool isAccessWorkspace,
		                                 bool isSdeWorkspace)
		{
			const int accessMemoLength = 2147483647;
			const string oracleTextType = "NVARCHAR2";
			const string accessTextType = "Text";
			const string accessMemoType = "Memo";

			if (isSdeWorkspace)
			{
				return GetTextLength(oracleTypeString, oracleTextType);
			}

			if (! string.Equals(accessTypeString.Trim(), accessMemoType,
			                    StringComparison.OrdinalIgnoreCase))
			{
				// access type is Text(length), get text length from it
				return GetTextLength(accessTypeString, accessTextType);
			}

			// access type is Memo. Use memo length if the tested workspace is pgdb, 
			// otherwise get text length from oracle length
			return isAccessWorkspace
				       ? accessMemoLength
				       : GetTextLength(oracleTypeString, oracleTextType);
		}

		private static int GetTextLength([NotNull] string typeString,
		                                 [NotNull] string typePrefix)
		{
			string prefix = $"{typePrefix.ToUpper()}(";

			string trimmed = typeString.ToUpper()
			                           .Replace(" ", string.Empty)
			                           .Replace(prefix, string.Empty)
			                           .Replace(")", string.Empty)
			                           .Trim();

			int result;
			if (! int.TryParse(trimmed, out result))
			{
				throw new InvalidConfigurationException(
					$"Unable to parse text length from string '{typeString}'");
			}

			return result;
		}

		private static esriFieldType GetFieldType([NotNull] string typeString)
		{
			Assert.ArgumentNotNullOrEmpty(typeString, nameof(typeString));

			switch (typeString.ToUpper())
			{
				case "SHORT INTEGER":
					return esriFieldType.esriFieldTypeSmallInteger;

				case "LONG INTEGER":
					return esriFieldType.esriFieldTypeInteger;

				case "FLOAT":
					return esriFieldType.esriFieldTypeSingle;

				case "DOUBLE":
					return esriFieldType.esriFieldTypeDouble;

				case "TEXT":
					return esriFieldType.esriFieldTypeString;

				case "DATE":
					return esriFieldType.esriFieldTypeDate;

				case "GUID":
					return esriFieldType.esriFieldTypeGUID;

				case "GLOBAL ID":
					return esriFieldType.esriFieldTypeGlobalID;

				case "OBJECT ID":
					return esriFieldType.esriFieldTypeOID;

				case "BLOB":
					return esriFieldType.esriFieldTypeBlob;

				case "XML":
					return esriFieldType.esriFieldTypeXML;

				case "GEOMETRY":
					return esriFieldType.esriFieldTypeGeometry;

				case "RASTER":
					return esriFieldType.esriFieldTypeRaster;
			}

			throw new ArgumentException(
				$@"Unsupported type string: {typeString}", nameof(typeString));
		}
	}
}
