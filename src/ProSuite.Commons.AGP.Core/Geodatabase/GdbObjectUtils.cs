using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
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
			var classDefinition = row.GetTable().GetDefinition();

			string subtypeField = null;
			try
			{
				subtypeField = classDefinition.GetSubtypeField();
			}
			catch (NotSupportedException notSupportedException)
			{
				// Shapefiles throw a NotSupportedException
				_msg.Debug("Subtypes not supported", notSupportedException);
			}

			string subtypeName = null;
			if (! string.IsNullOrEmpty(subtypeField))
			{
				int? subtypeCode = row[subtypeField] as int?;
				Subtype subtype = classDefinition.GetSubtypes()
				                                 .FirstOrDefault(st => st.GetCode() == subtypeCode);

				if (subtype != null)
				{
					subtypeName = subtype.GetName();
				}
			}

			return string.IsNullOrEmpty(subtypeName)
				       ? $"{className} ID: {row.GetObjectID()}"
				       : $"{className} ({subtypeName}) ID: {row.GetObjectID()}";
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
	}
}
