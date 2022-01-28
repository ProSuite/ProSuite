using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ShapeLengthAliasColumnInfo : ColumnInfo
	{
		private readonly int _fieldIndex;
		private readonly esriGeometryType _shapeType;
		private readonly List<string> _baseFieldNames = new List<string>();

		public ShapeLengthAliasColumnInfo([NotNull] IReadOnlyTable table,
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
				IField lengthField = featureClass.LengthField;

				if (lengthField != null)
				{
					_fieldIndex = featureClass.FindField(lengthField.Name);
					_baseFieldNames.Add(_fieldIndex < 0
						                    ? featureClass.ShapeFieldName
						                    : lengthField.Name);
				}
				else
				{
					_fieldIndex = -1;
					_baseFieldNames.Add(featureClass.ShapeFieldName);
				}

				_shapeType = featureClass.ShapeType;
			}
		}

		public override IEnumerable<string> BaseFieldNames
		{
			get { return _baseFieldNames; }
		}

		protected override object ReadValueCore(IReadOnlyRow row)
		{
			if (_fieldIndex >= 0)
			{
				return row.get_Value(_fieldIndex);
			}

			// there is no "shape length" field (e.g. for shapefiles)

			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return 0;
			}

			IGeometry shape = feature.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return 0;
			}

			switch (_shapeType)
			{
				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryMultipoint:
					return 0;

				case esriGeometryType.esriGeometryPolyline:
				case esriGeometryType.esriGeometryPolygon:
					var polyCurve = shape as IPolycurve;
					return polyCurve?.Length ?? 0;

				case esriGeometryType.esriGeometryEnvelope:
					var envelope = shape as IEnvelope;
					return envelope == null
						       ? 0
						       : GetEnvelopePerimeterLength(envelope);

				case esriGeometryType.esriGeometryMultiPatch:
					var multiPatch = shape as IMultiPatch;
					return multiPatch == null
						       ? 0
						       : GetMultiPatchPerimeterLength(multiPatch);

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

		private static double GetEnvelopePerimeterLength([NotNull] IEnvelope envelope)
		{
			return envelope.Width * 2 + envelope.Height * 2;
		}

		private static double GetMultiPatchPerimeterLength([NotNull] IMultiPatch multiPatch)
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(multiPatch);
			return polygon.IsEmpty
				       ? 0
				       : polygon.Length;
		}
	}
}
