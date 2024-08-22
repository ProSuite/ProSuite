using System;
using ArcGIS.Core.Geometry;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.ArcGIS.Geometry.AO
{
	public class ArcPolygon : ArcGeometry, IPolygon
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

		#endregion
	}
}
