using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	public static class GdbObjectUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static string ToString([NotNull] Row row)
		{
			string oid;
			try
			{
				oid = row.GetObjectID().ToString(CultureInfo.InvariantCulture);
			}
			catch (Exception e)
			{
				oid = string.Format("[error getting OID: {0}]", e.Message);
			}

			string tableName;
			try
			{
				using (Table table = row.GetTable())
				{
					tableName = table.GetName();
				}
			}
			catch (Exception e)
			{
				tableName = string.Format("[error getting table name: {0}]", e.Message);
			}

			return string.Format("oid={0} table={1}", oid, tableName);
		}

		[NotNull]
		public static string GetDisplayValue(Row row)
		{
			using (Table table = row.GetTable())
			{
				string className = DatasetUtils.GetAliasName(table);

				return GetDisplayValue(row, className);
			}
		}

		[NotNull]
		public static string GetDisplayValue(Row row, string className)
		{
			string subtypeName = null;

			Subtype subtype = GetSubtype(row);
			if (subtype != null)
			{
				subtypeName = subtype.GetName();
			}

			return string.IsNullOrEmpty(subtypeName)
				       ? $"{className} ID: {row.GetObjectID()}"
				       : $"{className} ({subtypeName}) ID: {row.GetObjectID()}";
		}

		[CanBeNull]
		public static int? GetSubtypeCode([NotNull] Row row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			using (Table table = row.GetTable())
			{
				string subtypeFieldName = DatasetUtils.GetSubtypeFieldName(table);

				if (! string.IsNullOrEmpty(subtypeFieldName))
				{
					int subtypeFieldIndex = row.FindField(subtypeFieldName);
					return GetSubtypeCode(row, subtypeFieldIndex);
				}

				return null;
			}
		}

		public static int? GetSubtypeCode([NotNull] Row row, int subtypeFieldIndex)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			if (subtypeFieldIndex < 0)
			{
				return null;
			}

			object code = row[subtypeFieldIndex];

			return code == null || code == DBNull.Value
				       ? null
				       : Convert.ToInt32(code);
		}

		[CanBeNull]
		public static Subtype GetSubtype([NotNull] Row row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			int? subtypeCode = GetSubtypeCode(row);
			if (! subtypeCode.HasValue)
			{
				return null;
			}

			try
			{
				using (Table table = row.GetTable())
				{
					using (TableDefinition definition = table.GetDefinition())
					{
						return DatasetUtils.GetSubtype(definition, subtypeCode.Value);
					}
				}
			}
			catch (NotSupportedException notSupportedException)
			{
				// Shapefiles throw a NotSupportedException
				_msg.Debug("Subtypes not supported", notSupportedException);
			}

			return null;
		}

		public static void SetSubtypeCode([NotNull] Row row, int subTypeCode)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			using (Table table = row.GetTable())
			{
				string subtypeFieldName = DatasetUtils.GetSubtypeFieldName(table);

				if (! string.IsNullOrEmpty(subtypeFieldName))
				{
					row[subtypeFieldName] = subTypeCode;
				}
			}
		}

		/// <summary>
		/// Sets the values of the <see cref="RowBuffer"/> which are not yet initialized to the
		/// default values defined in the Geodatabase.
		/// </summary>
		/// <param name="rowBuffer"></param>
		/// <param name="tableDefinition"></param>
		/// <param name="subtype"></param>
		public static void SetNullValuesToGdbDefault(
			[NotNull] RowBuffer rowBuffer,
			[NotNull] TableDefinition tableDefinition,
			[CanBeNull] Subtype subtype)
		{
			DoForAllFields(
				field =>
				{
					object currentValue = rowBuffer[field.Name];

					if (currentValue == null || currentValue == DBNull.Value)
					{
						rowBuffer[field.Name] = field.GetDefaultValue(subtype);
					}
				}, tableDefinition);
		}

		/// <summary>
		/// Sets the values of the <see cref="RowBuffer"/> which are not yet initialized to the
		/// default values defined in the Geodatabase.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="tableDefinition"></param>
		/// <param name="subtype"></param>
		public static void SetNullValuesToGdbDefault(
			[NotNull] Row row,
			[NotNull] TableDefinition tableDefinition,
			[CanBeNull] Subtype subtype)
		{
			DoForAllFields(
				field =>
				{
					// If the value has not been set (e.g. by the subclass), use the GDB default:
					object currentValue = row[field.Name];

					if (currentValue == null || currentValue == DBNull.Value)
					{
						row[field.Name] = field.GetDefaultValue(subtype);
					}
				}, tableDefinition);
		}

		[NotNull]
		public static IList<Geometry> GetGeometries(
			[NotNull] IEnumerable<Feature> features)
		{
			return features.Select(feature => feature.GetShape()).ToList();
		}

		[NotNull]
		public static IEnumerable<Feature> Filter([NotNull] IEnumerable<Feature> features,
		                                          GeometryType byGeometryType)
		{
			foreach (Feature feature in features)
			{
				using FeatureClass featureClass = feature.GetTable();

				if (featureClass.GetShapeType() == byGeometryType)
				{
					yield return feature;
				}
			}
		}

		public static bool IsSameFeature(Feature feature1, Feature feature2)
		{
			return IsSameRow(feature1, feature2);
		}

		/// <returns>True iff the two rows are the same</returns>
		/// <remarks>
		/// This is a cheap test, but it assumes that both rows are from
		/// the <b>same workspace</b>.  If the two rows are from different workspaces,
		/// this method <em>may</em> return true even though the rows are different!
		/// </remarks>
		public static bool IsSameRow(Row row1, Row row2)
		{
			if (ReferenceEquals(row1, row2)) return true;
			if (Equals(row1.Handle, row2.Handle)) return true;

			if (row1.Handle == row2.Handle)
			{
				return true;
			}

			if (row1.GetObjectID() != row2.GetObjectID())
			{
				return false;
			}

			using (var table1 = row1.GetTable())
			using (var table2 = row2.GetTable())
			{
				return DatasetUtils.IsSameTable(table1, table2);
			}
		}

		[CanBeNull]
		public static T? ReadRowValue<T>([NotNull] Row row, int fieldIndex)
			where T : struct
		{
			Assert.ArgumentNotNull(row, nameof(row));

			object value = row[fieldIndex];
			return ReadRowValue<T>(value, fieldIndex,
			                       () => row.GetObjectID(),
			                       () => row.GetTable().GetName());
		}

		[CanBeNull]
		private static T? ReadRowValue<T>(object value,
		                                  int fieldIndex,
		                                  Func<long?> getOid,
		                                  Func<string> getTableName)
			where T : struct
		{
			// TODO: Duplication in Commons.AO.GdbObjectUtils! Refactor to common place.
			if (value == DBNull.Value || value == null)
			{
				_msg.VerboseDebug(
					() => $"ReadRowValue: Field value at <index> {fieldIndex} of row is null.");

				return null;
			}

			try
			{
				if (typeof(T) == typeof(int))
				{
					if (value is short)
					{
						// Short Integer field type is returned as short, cannot unbox directly to int:
						return Convert.ToInt32(value) as T?;
					}

					if (value is long)
					{
						// Typically for long OID type that currently still is known to be an int.
						// But long object cannot unbox directly to int:
						return Convert.ToInt32(value) as T?;
					}
				}

				if (typeof(T) == typeof(Guid))
				{
					// Guids come back as string
					var guidString = value as string;

					if (string.IsNullOrEmpty(guidString))
					{
						return null;
					}

					TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

					return (T) Assert.NotNull(converter.ConvertFrom(guidString));
				}

				return (T) value;
			}
			catch (Exception ex)
			{
				long? rowOid = getOid();

				_msg.ErrorFormat(
					"ReadRowValue: Error casting value {0} of type {1} into type {2} for row <oid> {3} at field index {4} in {5}: {6}",
					value, value.GetType(), typeof(T), fieldIndex, rowOid, getTableName(),
					ex.Message);

				throw;
			}
		}

		public static bool IsNullOrEmpty(Row row, int fieldIndex)
		{
			object value = row[fieldIndex];

			if (value == null)
			{
				return true;
			}

			if (value is DBNull)
			{
				return true;
			}

			if (value is string textValue)
			{
				if (Guid.TryParseExact(textValue.ToUpper(), "B", out Guid uuid) &&
				    uuid.Equals(Guid.Empty))
				{
					return true;
				}

				return string.IsNullOrEmpty(value.ToString());
			}

			return false;
		}

		[CanBeNull]
		public static CodedValueDomain GetCodedValueDomain([NotNull] Row row, int fieldIndex,
		                                                   [CanBeNull] int? subtypeCode = null)
		{
			// todo daro using?
			try
			{
				return (CodedValueDomain) GetDomain(row, fieldIndex, subtypeCode);
			}
			catch (Exception ex)
			{
				_msg.Debug($"Not a coded value domain: {ex.Message}", ex);
			}

			return null;
		}

		[CanBeNull]
		public static CodedValueDomain GetCodedValueDomain([NotNull] Table table, int fieldIndex,
		                                                   [CanBeNull] int? subtypeCode = null)
		{
			try
			{
				return (CodedValueDomain) GetDomain(table, fieldIndex, subtypeCode);
			}
			catch (Exception ex)
			{
				_msg.Debug($"Not a coded value domain: {ex.Message}", ex);
			}

			return null;
		}

		[CanBeNull]
		public static CodedValueDomain GetCodedValueDomain([NotNull] TableDefinition definition,
		                                                   int fieldIndex,
		                                                   [CanBeNull] int? subtypeCode = null)
		{
			try
			{
				return (CodedValueDomain) GetDomain(definition, fieldIndex, subtypeCode);
			}
			catch (Exception ex)
			{
				_msg.Debug($"Not a coded value domain: {ex.Message}", ex);
			}

			return null;
		}

		[CanBeNull]
		public static Domain GetDomain([NotNull] Row row, int fieldIndex,
		                               [CanBeNull] int? subtypeCode = null)
		{
			// todo daro using?
			try
			{
				Table table = row.GetTable();

				return subtypeCode == null
					       ? GetDomain(table, fieldIndex, GetSubtypeCode(row))
					       : GetDomain(table, fieldIndex, subtypeCode);
			}
			catch (GeodatabaseException ex)
			{
				_msg.Debug(ex.Message, ex);
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return null;
		}

		[CanBeNull]
		public static Domain GetDomain([NotNull] Table table, int fieldIndex,
		                               [CanBeNull] int? subtypeCode = null)
		{
			try
			{
				using TableDefinition definition = table.GetDefinition();

				Field subtypeField = definition.GetFields()[fieldIndex];

				Subtype subtype = null;
				if (subtypeCode != null)
				{
					subtype = definition.GetSubtypes()
					                    .FirstOrDefault(st => st.GetCode() == subtypeCode);
				}

				return subtypeField.GetDomain(subtype);
			}
			catch (GeodatabaseException ex)
			{
				_msg.Debug(ex.Message, ex);
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return null;
		}

		[CanBeNull]
		public static Domain GetDomain([NotNull] TableDefinition definition, int fieldIndex,
		                               [CanBeNull] int? subtypeCode = null)
		{
			try
			{
				Field subtypeField = definition.GetFields()[fieldIndex];

				Subtype subtype = null;
				if (subtypeCode != null)
				{
					subtype = definition.GetSubtypes()
					                    .FirstOrDefault(st => st.GetCode() == subtypeCode);
				}

				return subtypeField.GetDomain(subtype);
			}
			catch (GeodatabaseException ex)
			{
				_msg.Debug(ex.Message, ex);
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return null;
		}

		/// <summary>
		/// Creates a matrix holding the target row field index as key and
		/// the matching source row field index in the value. If none matching
		/// source field is found for the target feature, the index will be -1.
		/// </summary>
		/// <param name="sourceTable">Source table</param>
		/// <param name="targetTable">Target table</param>
		/// <param name="includeReadOnlyFields">include readonly fields in matrix</param> 
		/// <param name="searchJoinedFields">match fields in source table, that start with 
		/// the table name of the target class</param>
		/// <param name="includeTargetField">Predicate to determine the inclusion of a given 
		/// target field (optional; field is included if null)</param>
		/// <returns>Dictionary with the indices of the targetClass as key
		/// and the matching index of the sourceClass as value (could be -1)</returns>
		/// <param name="requireMatchingDomainNames"> Whether the name of the domain must match too.
		/// Only the main domain of the field will be compared. Domains varying by subtype are ignored.
		/// Domain names are also ignored when a <paramref name="searchJoinedFields"/> is true.</param>
		[NotNull]
		public static IDictionary<int, int> CreateMatchingIndexMatrix(
			[NotNull] TableDefinition sourceTable,
			[NotNull] TableDefinition targetTable,
			bool includeReadOnlyFields = false,
			bool searchJoinedFields = false,
			[CanBeNull] Predicate<Field> includeTargetField = null,
			bool requireMatchingDomainNames = true)
		{
			Assert.ArgumentNotNull(sourceTable, nameof(sourceTable));
			Assert.ArgumentNotNull(targetTable, nameof(targetTable));

			IReadOnlyList<Field> sourceFields = sourceTable.GetFields();
			IReadOnlyList<Field> targetFields = targetTable.GetFields();

			string targetTableName = null;

			if (searchJoinedFields)
			{
				targetTableName = targetTable.GetName();
			}

			return CreateMatchingIndexMatrix(sourceFields, targetFields, includeReadOnlyFields,
			                                 targetTableName, includeTargetField,
			                                 requireMatchingDomainNames);
		}

		/// <summary>
		/// Creates a matrix holding the target row field index as key and
		/// the matching source row field index in the value. If none matching
		/// source field is found for the target feature, the index will be -1.
		/// </summary>
		/// <param name="sourceFields">Source feature class</param>
		/// <param name="targetFields">Target feature class</param>
		/// <param name="includeReadOnlyFields">include readonly fields in matrix</param> 
		/// <param name="targetTableName">match fields in source fields, that start with 
		/// the specified table name of the target table.</param>
		/// <param name="includeTargetField">Predicate to determine the inclusion of a given 
		/// target field (optional; field is included if null)</param>
		/// <returns>Dictionary with the indices of the targetClass as key
		/// and the matching index of the sourceClass as value (could be -1)</returns>
		/// <param name="requireMatchingDomainNames"> Whether the name of the domain must match too.
		/// Only the main domain of the field will be compared. Domains varying by subtype are ignored.
		/// Domain names are also ignored when a <paramref name="targetTableName"/> is provided.</param>
		[NotNull]
		public static IDictionary<int, int> CreateMatchingIndexMatrix(
			[NotNull] IReadOnlyList<Field> sourceFields,
			[NotNull] IReadOnlyList<Field> targetFields,
			bool includeReadOnlyFields = false,
			[CanBeNull] string targetTableName = null,
			[CanBeNull] Predicate<Field> includeTargetField = null,
			bool requireMatchingDomainNames = true)
		{
			var result = new Dictionary<int, int>();

			int targetFieldCount = targetFields.Count;

			for (var index = 0; index < targetFieldCount; index++)
			{
				Field targetField = targetFields[index];

				if (! includeReadOnlyFields && ! targetField.IsEditable)
				{
					continue;
				}

				if (includeTargetField != null && ! includeTargetField(targetField))
				{
					continue;
				}

				int matchingSourceFieldIndex =
					DatasetUtils.FindMatchingFieldIndex(
						targetField, sourceFields, targetTableName, requireMatchingDomainNames);

				if (matchingSourceFieldIndex < 0)
				{
					if (_msg.IsVerboseDebugEnabled)
					{
						_msg.DebugFormat(
							"field not found: {0} (type: {1} table name: {2} compare domains: {3})",
							targetField.Name, targetField.FieldType,
							targetTableName ?? "<null>", requireMatchingDomainNames);

						int fieldCount = sourceFields.Count;

						for (var sourceFieldIndex = 0;
						     sourceFieldIndex < fieldCount;
						     sourceFieldIndex++)
						{
							Field sourceField = sourceFields[sourceFieldIndex];
							_msg.DebugFormat("- {0}: {1}", sourceField.Name,
							                 sourceField.FieldType);
						}
					}
				}

				result.Add(index, matchingSourceFieldIndex);
			}

			return result;
		}

		private static void DoForAllFields([NotNull] Action<Field> action,
		                                   [NotNull] TableDefinition tableDefinition,
		                                   bool exceptShape = true)
		{
			foreach (Field field in tableDefinition.GetFields())
			{
				if (! field.IsEditable)
				{
					continue;
				}

				if (exceptShape && field.FieldType == FieldType.Geometry)
				{
					continue;
				}

				action(field);
			}
		}
	}
}
