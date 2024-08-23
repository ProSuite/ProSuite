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

		public double GetArea()
		{
			return _proPolygon.Area;
		}

		#endregion

		#region Implementation of IGeometryCollection

		public int GeometryCount { get; set; }

		public IGeometry get_Geometry(int index)
		{
			ReadOnlySegmentCollection segmentCollection = ((Multipart) _proPolygon).Parts[index];

			throw new NotImplementedException();
		}

		#endregion
	}
}
