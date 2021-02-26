using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ShapeVertexCountAliasColumnInfo : ColumnInfo
	{
		private readonly esriGeometryType _shapeType;
		private readonly List<string> _baseFieldNames = new List<string>();

		public ShapeVertexCountAliasColumnInfo([NotNull] ITable table,
		                                       [NotNull] string columnName)
			: base(table, columnName, typeof(int))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));

			var featureClass = table as IFeatureClass;
			if (featureClass == null)
			{
				_shapeType = esriGeometryType.esriGeometryNull;
			}
			else
			{
				_baseFieldNames.Add(featureClass.ShapeFieldName);

				_shapeType = featureClass.ShapeType;
			}
		}

		public override IEnumerable<string> BaseFieldNames
		{
			get { return _baseFieldNames; }
		}

		protected override object ReadValueCore(IRow row)
		{
			var feature = row as IFeature;

			IGeometry shape = feature?.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return 0;
			}

			switch (_shapeType)
			{
				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryMultipoint:
				case esriGeometryType.esriGeometryPolyline:
				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryMultiPatch:
					return GeometryUtils.GetPointCount(shape);

				case esriGeometryType.esriGeometryEnvelope:
					return 4;

				case esriGeometryType.esriGeometryNull:
				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryCircularArc:
				case esriGeometryType.esriGeometryEllipticArc:
				case esriGeometryType.esriGeometryBezier3Curve:
				case esriGeometryType.esriGeometryPath:
				case esriGeometryType.esriGeometryRing:
				case esriGeometryType.esriGeometryAny:
				case esriGeometryType.esriGeometryBag:
				case esriGeometryType.esriGeometryTriangleStrip:
				case esriGeometryType.esriGeometryTriangleFan:
				case esriGeometryType.esriGeometryRay:
				case esriGeometryType.esriGeometrySphere:
				case esriGeometryType.esriGeometryTriangles:
					throw new InvalidOperationException("High-level geometry expected");

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
