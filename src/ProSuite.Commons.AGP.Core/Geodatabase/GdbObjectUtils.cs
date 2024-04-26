using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using ArcGIS.Core.Data;
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
				tableName = row.GetTable().GetName();
			}
			catch (Exception e)
			{
				tableName = string.Format("[error getting table name: {0}]", e.Message);
			}

			return string.Format("oid={0} table={1}", oid, tableName);
		}

		public static string GetDisplayValue(Row row)
		{
			string className = DatasetUtils.GetAliasName(row.GetTable());

			return GetDisplayValue(row, className);
		}

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

			Table table = row.GetTable();

			string subtypeFieldName = DatasetUtils.GetSubtypeFieldName(table);

			if (! string.IsNullOrEmpty(subtypeFieldName))
			{
				int subtypeFieldIndex = row.FindField(subtypeFieldName);
				return GetSubtypeCode(row, subtypeFieldIndex);
			}

			return null;
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
				       ? (int?) null
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

			Subtype subtype = null;
			try
			{
				subtype = row.GetTable().GetDefinition().GetSubtypes()
				             ?.FirstOrDefault(st => st.GetCode() == subtypeCode.Value);
			}
			catch (NotSupportedException notSupportedException)
			{
				// Shapefiles throw a NotSupportedException
				_msg.Debug("Subtypes not supported", notSupportedException);
			}

			return subtype;
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
			return features.Where(
				f => DatasetUtils.GetShapeType(f.GetTable()) == byGeometryType);
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
		private static T? ReadRowValue<T>([NotNull] object value,
		                                  int fieldIndex,
		                                  Func<long?> getOid,
		                                  Func<string> getTableName)
			where T : struct
		{
			// TODO: Duplication in Commons.AO.GdbObjectUtils! Refactor to common place.
			if (value == DBNull.Value)
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
	}
}
