extern alias EsriGeodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.ArcGIS.Geometry.AO;
using IGeometryDef = ESRI.ArcGIS.Geodatabase.IGeometryDef;

namespace ProSuite.ArcGIS.Geodatabase.AO
{
	public class ArcGeometryDef : IGeometryDef
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IGeometryDef _aoGeometryDef;

		public ArcGeometryDef(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IGeometryDef aoGeometryDef)
		{
			_aoGeometryDef = aoGeometryDef;
		}

		#region Implementation of IGeometryDef

		public int AvgNumPoints => _aoGeometryDef.AvgNumPoints;

		public esriGeometryType GeometryType => (esriGeometryType)_aoGeometryDef.GeometryType;

		public double get_GridSize(int index)
		{
			return _aoGeometryDef.get_GridSize(index);
		}

		public int GridCount => _aoGeometryDef.GridCount;

		public ISpatialReference SpatialReference => new ArcSpatialReference(_aoGeometryDef.SpatialReference);

		public bool HasZ => _aoGeometryDef.HasZ;

		public bool HasM => _aoGeometryDef.HasM;

		#endregion
	}
}
