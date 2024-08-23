using ArcGIS.Core.Geometry;
using ESRI.ArcGIS.Geometry;
using ProSuite.ArcGIS.Geometry.AO;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcMultipoint : ArcGeometry, IMultipoint
	{
		private readonly Multipoint _proMultipoint;

		public ArcMultipoint(Multipoint proMultipoint) : base(proMultipoint)
		{
			_proMultipoint = proMultipoint;
		}

		#region Implementation of IGeometryCollection

		public int GeometryCount => _proMultipoint.PointCount;

		public IGeometry get_Geometry(int index)
		{
			return new ArcPoint(_proMultipoint.Points[index]);
		}

		#endregion
	}
}
