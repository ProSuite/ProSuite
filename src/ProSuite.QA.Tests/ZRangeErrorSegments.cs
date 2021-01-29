using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class ZRangeErrorSegments
	{
		[NotNull] private readonly List<ISegment> _segments = new List<ISegment>();

		[CLSCompliant(false)]
		public ZRangeErrorSegments(ZRangeRelation zRangeRelation,
		                           [CanBeNull] ISpatialReference spatialReference,
		                           bool startsOnFirstSegment = false)
		{
			ZRangeRelation = zRangeRelation;
			StartsOnFirstSegment = startsOnFirstSegment;
			SpatialReference = spatialReference;

			MinZ = double.MaxValue;
			MaxZ = double.MinValue;
		}

		public void AddSegments([NotNull] ZRangeErrorSegments errorSegments)
		{
			_segments.AddRange(errorSegments.Segments);

			MinZ = Math.Min(MinZ, errorSegments.MinZ);
			MaxZ = Math.Max(MaxZ, errorSegments.MaxZ);
		}

		[CLSCompliant(false)]
		public void AddSegment([NotNull] ISegment segment, double fromZ, double toZ)
		{
			_segments.Add(segment);

			MinZ = Math.Min(MinZ, Math.Min(fromZ, toZ));
			MaxZ = Math.Max(MaxZ, Math.Max(fromZ, toZ));
		}

		[NotNull]
		[CLSCompliant(false)]
		public IEnumerable<ISegment> Segments => _segments;

		public ZRangeRelation ZRangeRelation { get; }

		public bool StartsOnFirstSegment { get; }

		public bool EndsOnLastSegment { get; set; }

		public int SegmentCount => _segments.Count;

		public double MinZ { get; private set; }

		public double MaxZ { get; private set; }

		[CanBeNull]
		[CLSCompliant(false)]
		public ISpatialReference SpatialReference { get; }

		[CanBeNull]
		[CLSCompliant(false)]
		public IPoint CreateStartPoint()
		{
			return _segments.Count == 0
				       ? null
				       : _segments[0].FromPoint;
		}

		[NotNull]
		[CLSCompliant(false)]
		public IPolyline CreatePolyline()
		{
			IPolyline result = new PolylineClass
			                   {
				                   SpatialReference = SpatialReference
			                   };

			GeometryUtils.MakeZAware(result);
			var segments = (ISegmentCollection) result;

			object o = Type.Missing;

			foreach (ISegment segment in _segments)
			{
				segments.AddSegment(GeometryFactory.Clone(segment), ref o, ref o);
			}

			return result;
		}
	}
}
