using System;
using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
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

		#region Overrides of ArcGeometry

		public override IGeometry Clone()
		{
			return new ArcPolygon((Polygon) _proPolygon.Clone());
		}

		#endregion
	}
}
