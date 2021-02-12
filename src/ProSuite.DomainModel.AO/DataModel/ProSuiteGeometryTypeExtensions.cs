using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public static class ProSuiteGeometryTypeExtensions
	{
		public static bool IsEqual(this GeometryTypeShape geometryTypeShape,
		                           esriGeometryType esriGeometryType)
		{
			return (int) geometryTypeShape.ShapeType == (int) esriGeometryType;
		}

		public static esriGeometryType ToEsriGeometryType(this GeometryTypeShape geometryTypeShape)
		{
			int code = (int) geometryTypeShape.ShapeType;

			return (esriGeometryType) code;
		}
	}
}