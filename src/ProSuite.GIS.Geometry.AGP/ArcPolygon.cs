using System;
using ArcGIS.Core.Geometry;
using ESRI.ArcGIS.Geometry;
using ProSuite.GIS.Geometry.AGP;

namespace ProSuite.ArcGIS.Geometry.AO
{
	public class ArcPolygon : ArcPolycurve, IPolygon
	{
		private readonly Polygon _proPolygon;

		public ArcPolygon(Polygon polygon) : base(polygon)
		{
			_proPolygon = polygon;
		}

		#region Implementation of IPolygon

		public int ExteriorRingCount => _proPolygon.ExteriorRingCount;

		public void SimplifyPreserveFromTo()
		{
			throw new NotImplementedException();
		}

		public double GetArea()
		{
			return _proPolygon.Area;
		}

		#endregion
	}
}