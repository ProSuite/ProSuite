using System;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Container.PolygonGrower
{
	[CLSCompliant(false)]
	public interface IHasPolyline
	{
		IPolyline Polyline { get; }
	}
}
