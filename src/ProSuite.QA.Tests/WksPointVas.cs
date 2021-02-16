using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Tests
{
	[Obsolete("refactor with Qa.Container.Geometry.IndexedGeometry")]
	internal class WksPointVas : IPoints
	{
		private readonly List<WKSPointVA> _points;
		private readonly ISpatialReference _spatialReference;

		public WksPointVas(List<WKSPointVA> points, ISpatialReference spatialReference)
		{
			_points = points;
			_spatialReference = spatialReference;
		}

		public int PointCount => _points.Count;

		public IPoint GetPoint(int index)
		{
			WKSPointVA p = _points[index];
			IPoint ret = new PointClass();
			ret.PutCoords(p.m_x, p.m_y);
			ret.Z = p.m_z;
			return ret;
		}

		public void QueryCoords(int index, out double x, out double y)
		{
			WKSPointVA p = _points[index];
			x = p.m_x;
			y = p.m_y;
		}

		public void QueryCoords(int index, out double x, out double y, out double z)
		{
			WKSPointVA p = _points[index];
			x = p.m_x;
			y = p.m_y;
			z = p.m_z;
		}

		public ISpatialReference SpatialReference => _spatialReference;
	}
}
