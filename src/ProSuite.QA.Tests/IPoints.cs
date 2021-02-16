using System;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Tests
{
	[Obsolete("refactor with Qa.Container.Geometry.IndexedGeometry")]
	internal interface IPoints
	{
		int PointCount { get; }
		ISpatialReference SpatialReference { get; }

		void QueryCoords(int index, out double x, out double y);

		void QueryCoords(int index, out double x, out double y, out double z);

		IPoint GetPoint(int index);
	}
}
