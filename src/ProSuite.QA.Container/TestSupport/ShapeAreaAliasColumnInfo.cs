using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ShapeAreaAliasColumnInfo : ColumnInfo
	{
		private readonly int _fieldIndex;
		private readonly esriGeometryType _shapeType;
		private readonly List<string> _baseFieldNames = new List<string>();

		public ShapeAreaAliasColumnInfo([NotNull] IReadOnlyTable table,
		                                [NotNull] string columnName)
			: base(table, columnName, typeof(double))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));

			var featureClass = table as IReadOnlyFeatureClass;

			if (featureClass == null)
			{
				_fieldIndex = -1;
				_shapeType = esriGeometryType.esriGeometryNull;
			}
			else
			{
				IField areaField = featureClass.AreaField;

				if (areaField != null)
				{
					_fieldIndex = featureClass.FindField(areaField.Name);
					_baseFieldNames.Add(_fieldIndex < 0
						                    ? featureClass.ShapeFieldName
						                    : areaField.Name);
				}
				else
				{
					_fieldIndex = -1;
					_baseFieldNames.Add(featureClass.ShapeFieldName);
				}

				_shapeType = featureClass.ShapeType;
			}
		}

		public override IEnumerable<string> BaseFieldNames => _baseFieldNames;

		protected override object ReadValueCore(IReadOnlyRow row)
		{
			if (_fieldIndex >= 0)
			{
				return row.get_Value(_fieldIndex);
			}

			// there is no "shape area" field (e.g. for shapefiles)

			var feature = row as IReadOnlyFeature;

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
					return 0;

				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryEnvelope:
				case esriGeometryType.esriGeometryMultiPatch:
					var area = shape as IArea;
					return area?.Area ?? 0;

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
