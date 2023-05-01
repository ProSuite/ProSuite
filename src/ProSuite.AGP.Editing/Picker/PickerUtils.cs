using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	internal static class PickerUtils
	{
		public static Uri GetImagePath(esriGeometryType? geometryType)
		{
			// todo daro introduce image for unkown type
			//if (geometryType == null)
			//{
			//}
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryMultipoint:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/PointGeometry.bmp");
				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryPolyline:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/LineGeometry.bmp");
				case esriGeometryType.esriGeometryPolygon:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/PolygonGeometry.bmp",
						UriKind.Absolute);
				case esriGeometryType.esriGeometryMultiPatch:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/MultipatchGeometry.bmp");
				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported geometry type: {geometryType}");
			}
		}

		public static GeometryType Translate(esriGeometryType geometryType)
		{
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryMultipoint:
					return GeometryType.Point;
				case esriGeometryType.esriGeometryPolyline:
					return GeometryType.Polyline;
				case esriGeometryType.esriGeometryPolygon:
					return GeometryType.Polygon;
				case esriGeometryType.esriGeometryEnvelope:
					return GeometryType.Envelope;
				case esriGeometryType.esriGeometryPath:
				case esriGeometryType.esriGeometryAny:
				case esriGeometryType.esriGeometryMultiPatch:
				case esriGeometryType.esriGeometryRing:
				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryCircularArc:
				case esriGeometryType.esriGeometryBezier3Curve:
				case esriGeometryType.esriGeometryEllipticArc:
				case esriGeometryType.esriGeometryBag:
				case esriGeometryType.esriGeometryTriangleStrip:
				case esriGeometryType.esriGeometryTriangleFan:
				case esriGeometryType.esriGeometryRay:
				case esriGeometryType.esriGeometrySphere:
				case esriGeometryType.esriGeometryTriangles:
				case esriGeometryType.esriGeometryNull:
					return GeometryType.Unknown;
				default:
					throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, null);
			}
		}
	}
}
