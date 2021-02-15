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

		public MonotonicitySequence(esriMonotinicityEnum monotonicityType,
		                            [CanBeNull] ISpatialReference spatialReference)
		{
			MonotonicityType = monotonicityType;

			_spatialReference = spatialReference;
		}

		public int SegmentCount => _segments.Count;

		public double Length { get; private set; }

		public bool? FeatureIsFlipped { get; set; }

		public esriMonotinicityEnum? FeatureMonotonicityTrend { get; set; }

		public void Add([NotNull] ISegment segment)
		{
			_segments.Add(segment);
			Length += segment.Length;
		}

		[NotNull]
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

		public esriMonotinicityEnum MonotonicityType { get; }

		[NotNull]
		public IList<ISegment> Segments => _segments;

		protected virtual void AdaptMZAware([NotNull] IGeometry geometry) { }
	}
}
