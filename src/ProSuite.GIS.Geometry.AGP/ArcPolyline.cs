using ProSuite.GIS.Geometry.API;
using Polyline = ArcGIS.Core.Geometry.Polyline;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcPolyline : ArcPolycurve, IPolyline
	{
		private readonly Polyline _proPolyline;

		public ArcPolyline(Polyline proPolyline) : base(proPolyline)
		{
			_proPolyline = proPolyline;
		}

		#region Overrides of ArcGeometry

		public override IGeometry Clone()
		{
			return new ArcPolyline((Polyline) _proPolyline.Clone());
		}

		#endregion
	}
}
