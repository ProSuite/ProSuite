using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	public static class GdbObjectUtils
	{
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

		public static bool IsSameRow(Row row1, Row row2)
		{
			if (row1 == null && row2 == null)
			{
				return true;
			}

			if (row1 == null || row2 == null)
			{
				return false;
			}

			if (row1.Handle == row2.Handle)
			{
				return true;
			}

			if (row1.GetObjectID() != row2.GetObjectID())
			{
				return false;
			}

			Table table1 = row1.GetTable();
			Table table2 = row2.GetTable();

			return DatasetUtils.IsSameClass(table1, table2);
		}
	}
}
