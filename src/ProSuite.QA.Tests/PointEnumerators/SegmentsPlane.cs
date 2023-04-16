using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container.Geometry;
using SegmentUtils_ = ProSuite.QA.Container.Geometry.SegmentUtils_;

namespace ProSuite.QA.Tests.PointEnumerators
{
	public class SegmentsPlane
	{
		[CanBeNull] private Plane3D _plane;
		[CanBeNull] private IGeometry _geometry;

		public SegmentsPlane([NotNull] IEnumerable<SegmentProxy> segments,
		                     esriGeometryType shapeType)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));
			Assert.ArgumentCondition(shapeType == esriGeometryType.esriGeometryPolygon ||
			                         shapeType == esriGeometryType.esriGeometryMultiPatch,
			                         "Multipatch or polygon expected",
			                         nameof(shapeType));

			Segments = segments;
			ShapeType = shapeType;
		}

		public esriGeometryType ShapeType { get; }

		[NotNull]
		public IEnumerable<SegmentProxy> Segments { get; }

		public IEnumerable<Pnt3D> GetPoints()
		{
			return Segments.Select(s => (Pnt3D) s.GetStart(true));
		}

		[NotNull]
		public Plane3D Plane =>
			_plane ?? (_plane = ProxyUtils.CreatePlane3D(Segments));

		[NotNull]
		public IGeometry Geometry =>
			_geometry ?? (_geometry = GetGeometry(Segments, ShapeType));

		public double GetXyDistance([NotNull] IGeometry other)
		{
			return ((IProximityOperator) Geometry).ReturnDistance(other);
		}

		[NotNull]
		private static IGeometry GetGeometry([NotNull] IEnumerable<SegmentProxy> segments,
		                                     esriGeometryType geometryType)
		{
			return geometryType == esriGeometryType.esriGeometryMultiPatch
				       ? (IGeometry) SegmentUtils_.CreateMultiPatch(segments)
				       : SegmentUtils_.CreatePolygon(segments);
		}
	}
}
