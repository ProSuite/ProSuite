using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;
using System;
using System.Collections.Generic;
using System.Xml;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcMultipatch : ArcGeometry, IMultiPatch
	{
		private readonly Multipatch _proMultipatch;

		public ArcMultipatch(Multipatch proMultipatch) : base(proMultipatch)
		{
			_proMultipatch = proMultipatch;
		}

		#region Implementation of IGeometryCollection

		public int GeometryCount => _proMultipatch.PartCount;

		public IGeometry get_Geometry(int index)
		{
			ReadOnlyPointCollection pointCollection = _proMultipatch.Points;

			int startIdx = _proMultipatch.GetPatchStartPointIndex(index);
			int pointCount = _proMultipatch.GetPatchPointCount(index);

			// Build segments from consecutive points
			var segments = new List<Segment>();
			for (int i = startIdx; i < startIdx + pointCount - 1; i++)
			{
				MapPoint fromPoint = pointCollection[i];
				MapPoint toPoint = pointCollection[i + 1];

				// Create a line segment between consecutive points
				LineSegment segment = LineBuilderEx.CreateLineSegment(fromPoint, toPoint);
				segments.Add(segment);
			}

			Polyline polyline = PolylineBuilderEx.CreatePolyline(segments, _proMultipatch.SpatialReference);

			return new ArcPath(polyline.Parts[0], true, SpatialReference);
		}

		#endregion

		#region Implementation of IMultiPatch

		public IGeometry XYFootprint { get; set; }

		public void InvalXYFootprint()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Overrides of ArcGeometry

		public override IGeometry Clone()
		{
			return new ArcMultipatch((Multipatch) _proMultipatch.Clone());
		}

		#endregion
	}
}
