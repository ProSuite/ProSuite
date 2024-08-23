using ArcGIS.Core.Geometry;
using ESRI.ArcGIS.Geometry;
using ProSuite.ArcGIS.Geometry.AO;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcPolyline : ArcGeometry, IPolyline
	{
		private readonly Polyline _proPolyline;

		public ArcPolyline(Polyline proPolyline) : base(proPolyline)
		{
			_proPolyline = proPolyline;
		}

		#region Implementation of IPolyline

		public double Length => _proPolyline.Length;

		public IPoint FromPoint => new ArcPoint(_proPolyline.Points[0]);

		//public void QueryFromPoint(IPoint from)
		//{
		// throw new NotImplementedException();
		//}

		public IPoint ToPoint => new ArcPoint(_proPolyline.Points[_proPolyline.PointCount - 1]);

		//public void QueryToPoint(IPoint to)
		//{
		// throw new NotImplementedException();
		//}

		public bool IsClosed => FromPoint.Equals(ToPoint);

		#endregion
	}
}
