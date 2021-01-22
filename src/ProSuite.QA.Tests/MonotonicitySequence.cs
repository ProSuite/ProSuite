using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class MonotonicitySequence
	{
		[NotNull] private readonly List<ISegment> _segments = new List<ISegment>();

		[CanBeNull] private readonly ISpatialReference _spatialReference;

		[CLSCompliant(false)]
		public MonotonicitySequence(esriMonotinicityEnum monotonicityType,
		                            [CanBeNull] ISpatialReference spatialReference)
		{
			MonotonicityType = monotonicityType;

			_spatialReference = spatialReference;
		}

		public int SegmentCount => _segments.Count;

		public double Length { get; private set; }

		public bool? FeatureIsFlipped { get; set; }

		[CLSCompliant(false)]
		public esriMonotinicityEnum? FeatureMonotonicityTrend { get; set; }

		[CLSCompliant(false)]
		public void Add([NotNull] ISegment segment)
		{
			_segments.Add(segment);
			Length += segment.Length;
		}

		[NotNull]
		[CLSCompliant(false)]
		public IPolyline CreatePolyline()
		{
			IPolyline result = new PolylineClass
			                   {
				                   SpatialReference = _spatialReference
			                   };

			AdaptMZAware(result);
			var segments = (ISegmentCollection) result;

			object o = Type.Missing;

			foreach (ISegment segment in _segments)
			{
				segments.AddSegment(GeometryFactory.Clone(segment), ref o, ref o);
			}

			return result;
		}

		[CLSCompliant(false)]
		public esriMonotinicityEnum MonotonicityType { get; }

		[NotNull]
		[CLSCompliant(false)]
		public IList<ISegment> Segments => _segments;

		[CLSCompliant(false)]
		protected virtual void AdaptMZAware([NotNull] IGeometry geometry) { }
	}
}
