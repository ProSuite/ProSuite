using ArcGIS.Core.Geometry;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcPolyline : ArcPolycurve, IPolyline
	{
		private readonly Polyline _proPolyline;

		public ArcPolyline(Polyline proPolyline) : base(proPolyline)
		{
			_proPolyline = proPolyline;
		}
	}
}
