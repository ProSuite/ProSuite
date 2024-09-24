using System;
using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcPolycurve : ArcGeometry, IPolycurve
	{
		private readonly Multipart _proPolycurve;

		public ArcPolycurve(Multipart proPolyline) : base(proPolyline)
		{
			_proPolycurve = proPolyline;
		}

		#region Implementation of IPolyline

		public double Length => _proPolycurve.Length;

		public IPoint FromPoint => new ArcPoint(_proPolycurve.Points[0]);

		//public void QueryFromPoint(IPoint from)
		//{
		// throw new NotImplementedException();
		//}

		public IPoint ToPoint => new ArcPoint(_proPolycurve.Points[_proPolycurve.PointCount - 1]);

		//public void QueryToPoint(IPoint to)
		//{
		// throw new NotImplementedException();
		//}

		public bool IsClosed => FromPoint.Equals(ToPoint);

		#endregion

		#region Implementation of IGeometryCollection

		public int GeometryCount => _proPolycurve.Parts.Count;

		public IGeometry get_Geometry(int index)
		{
			ReadOnlySegmentCollection segmentCollection = _proPolycurve.Parts[index];

			throw new NotImplementedException();
		}

		#endregion
	}
}
