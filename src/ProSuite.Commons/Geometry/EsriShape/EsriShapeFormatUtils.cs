using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry.EsriShape
{
	public static class EsriShapeFormatUtils
	{
		public static EsriShapeType GetShapeType([NotNull] byte[] esriShapeBuffer)
		{
			Assert.ArgumentCondition(esriShapeBuffer.Length > 0,
			                         "The provided shape buffer is empty");

			return (EsriShapeType) esriShapeBuffer[0];
		}

		public static ProSuiteGeometryType GetGeometryType([NotNull] byte[] esriShapeBuffer)
		{
			EsriShapeType shapeType = GetShapeType(esriShapeBuffer);

			return TranslateEsriShapeType(shapeType);
		}

		/// <summary>
		/// Translates the geometry type from the Esri Shape format to the geometry type
		/// used by ArcObjects/ProSuite.
		/// </summary>
		/// <param name="shapeType"></param>
		/// <returns></returns>
		public static ProSuiteGeometryType TranslateEsriShapeType(EsriShapeType shapeType)
		{
			ProSuiteGeometryType geometryType;

			switch (shapeType)
			{
				case EsriShapeType.EsriShapeGeneralPoint:
				case EsriShapeType.EsriShapePoint:
				case EsriShapeType.EsriShapePointZ:
				case EsriShapeType.EsriShapePointM:
				case EsriShapeType.EsriShapePointZM:
					geometryType = ProSuiteGeometryType.Point;
					break;
				case EsriShapeType.EsriShapeGeneralMultipoint:
				case EsriShapeType.EsriShapeMultipoint:
				case EsriShapeType.EsriShapeMultipointZ:
				case EsriShapeType.EsriShapeMultipointM:
				case EsriShapeType.EsriShapeMultipointZM:
					geometryType = ProSuiteGeometryType.Multipoint;
					break;
				case EsriShapeType.EsriShapeGeneralPolyline:
				case EsriShapeType.EsriShapePolyline:
				case EsriShapeType.EsriShapePolylineZ:
				case EsriShapeType.EsriShapePolylineM:
				case EsriShapeType.EsriShapePolylineZM:
					geometryType = ProSuiteGeometryType.Polyline;
					break;
				case EsriShapeType.EsriShapeGeneralPolygon:
				case EsriShapeType.EsriShapePolygon:
				case EsriShapeType.EsriShapePolygonZ:
				case EsriShapeType.EsriShapePolygonM:
				case EsriShapeType.EsriShapePolygonZM:
					geometryType = ProSuiteGeometryType.Polygon;
					break;
				case EsriShapeType.EsriShapeGeneralMultiPatch:
				case EsriShapeType.EsriShapeMultiPatch:
				case EsriShapeType.EsriShapeMultiPatchM:
					geometryType = ProSuiteGeometryType.MultiPatch;
					break;
				case EsriShapeType.EsriShapeNull:
					geometryType = ProSuiteGeometryType.Null;
					break;
				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported geometry type: {shapeType}");
			}

			return geometryType;
		}
	}
}
