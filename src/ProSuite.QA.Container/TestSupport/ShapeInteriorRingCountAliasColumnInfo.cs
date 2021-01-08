using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	[CLSCompliant(false)]
	public class ShapeInteriorRingCountAliasColumnInfo : ColumnInfo
	{
		private readonly List<string> _baseFieldNames = new List<string>();
		private readonly bool _canHaveInteriorRings;

		public ShapeInteriorRingCountAliasColumnInfo([NotNull] ITable table,
		                                             [NotNull] string columnName)
			: base(table, columnName, typeof(int))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));

			var featureClass = table as IFeatureClass;
			if (featureClass == null)
			{
				_canHaveInteriorRings = false;
			}
			else
			{
				_canHaveInteriorRings = featureClass.ShapeType ==
				                        esriGeometryType.esriGeometryPolygon;

				_baseFieldNames.Add(featureClass.ShapeFieldName);
			}
		}

		public override IEnumerable<string> BaseFieldNames
		{
			get { return _baseFieldNames; }
		}

		protected override object ReadValueCore(IRow row)
		{
			if (! _canHaveInteriorRings)
			{
				return 0;
			}

			var feature = row as IFeature;

			IGeometry shape = feature?.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return 0;
			}

			var polygon = (IPolygon) shape;

			int totalRingCount = ((IGeometryCollection) polygon).GeometryCount;

			if (totalRingCount < 2)
			{
				return 0;
			}

			int? exteriorRingCount = GetExteriorRingCount(polygon);

			if (exteriorRingCount == null)
			{
				return DBNull.Value; // not simple --> unknown --> NULL
			}

			return totalRingCount - exteriorRingCount.Value;
		}

		private static int? GetExteriorRingCount([NotNull] IPolygon polygon)
		{
			try
			{
				return polygon.ExteriorRingCount;
			}
			catch (COMException)
			{
				return null;
			}
		}
	}
}
