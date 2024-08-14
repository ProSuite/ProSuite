using System;
using ArcGIS.Core.Data.DDL;
using ESRI.ArcGIS.Geometry;
using ProSuite.ArcGIS.Geometry.AO;
using IGeometryDef = ESRI.ArcGIS.Geodatabase.IGeometryDef;

namespace ProSuite.ArcGIS.Geodatabase.AO
{
	public class ArcGeometryDef : IGeometryDef
	{
		private readonly ShapeDescription _shapeDescription;

		public ArcGeometryDef(ShapeDescription shapeDescription)
		{
			_shapeDescription = shapeDescription;
		}

		#region Implementation of IGeometryDef

		public int AvgNumPoints => throw new NotImplementedException();

		public esriGeometryType GeometryType => (esriGeometryType)_shapeDescription.GeometryType;

		public double get_GridSize(int index)
		{
			throw new NotImplementedException();
		}

		public int GridCount => throw new NotImplementedException();

		public ISpatialReference SpatialReference => new ArcSpatialReference(_shapeDescription.SpatialReference);

		public bool HasZ => _shapeDescription.HasZ;

		public bool HasM => _shapeDescription.HasM;

		#endregion
	}
}
