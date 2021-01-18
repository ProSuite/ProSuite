using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Tests.Coincidence
{
	internal class SegmentSearcher : IIndexedSegments
	{
		private static readonly ThreadLocal<IEnvelope> _qEnv =
			new ThreadLocal<IEnvelope>(() => new EnvelopeClass());

		private readonly ISegmentCollection _segments;
		private bool _releaseOnDispose;

		public SegmentSearcher([NotNull] ISegmentCollection segments, bool releaseOnDispose)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));

			_segments = segments;
			_releaseOnDispose = releaseOnDispose;
		}

		public void Dispose()
		{
			if (_releaseOnDispose)
			{
				ComUtils.ReleaseComObject(_segments);
			}

			_releaseOnDispose = false;
		}

		public bool AllowIndexing
		{
			get { return ((ISpatialIndex) _segments).AllowIndexing; }
			set { ((ISpatialIndex) _segments).AllowIndexing = value; }
		}

		public IEnvelope Envelope => ((IGeometry) _segments).Envelope;

		public IEnumerable<SegmentProxy> GetSegments()
		{
			IEnumSegment enumSegs = _segments.EnumSegments;

			var enumerable = new SegmentEnumerable(enumSegs);
			return enumerable;
		}

		public SegmentProxy GetSegment(int partIndex, int segmentIndex)
		{
			IEnumSegment enumSegs = _segments.EnumSegments;
			enumSegs.SetAt(partIndex, segmentIndex);

			ISegment segment;
			enumSegs.Next(out segment, ref partIndex, ref segmentIndex);

			ISegment proxiedSegment;
			if (enumSegs.IsRecycling)
			{
				proxiedSegment = GeometryFactory.Clone(segment);

				// release the segment, otherwise "pure virtual function call" occurs 
				// when there are certain circular arcs (IsLine == true ?)
				Marshal.ReleaseComObject(segment);
			}
			else
			{
				proxiedSegment = segment;
			}

			return new AoSegmentProxy(proxiedSegment, partIndex, segmentIndex);
		}

		public int GetPartsCount()
		{
			return ((IGeometryCollection) _segments).GeometryCount;
		}

		public IEnumerable<SegmentProxy> GetSegments(IBox box)
		{
			IPnt min = box.Min;
			IPnt max = box.Max;
			_qEnv.Value.PutCoords(min.X, min.Y, max.X, max.Y);
			IEnumSegment enumSegs = _segments.IndexedEnumSegments[_qEnv.Value];

			var enumerable = new SegmentEnumerable(enumSegs);
			return enumerable;
		}

		public bool IsPartClosed(int part)
		{
			IGeometry sourcePart = ((IGeometryCollection) _segments).Geometry[part];

			bool partIsClosed = ((IPath) sourcePart).IsClosed;
			return partIsClosed;
		}

		public int GetPartSegmentCount(int part)
		{
			IGeometry sourcePart = ((IGeometryCollection) _segments).Geometry[part];

			int segCount = ((ISegmentCollection) sourcePart).SegmentCount;
			return segCount;
		}

		public IPolyline GetSubpart(int part, int startSegmentIndex, double startFraction,
		                            int endSegmentIndex, double endFraction)
		{
			double l0 = IndexedSegmentUtils.GetLength(this, part, 0, 0, startSegmentIndex,
			                                          startFraction);
			double l1 = IndexedSegmentUtils.GetLength(this, part, startSegmentIndex,
			                                          startFraction,
			                                          endSegmentIndex, endFraction);

			IGeometry sourcePart = ((IGeometryCollection) _segments).Geometry[part];
			var sourceCurve = (ICurve) sourcePart;

			ICurve subcurve;
			sourceCurve.GetSubcurve(l0, l0 + l1, false, out subcurve);

			ISegmentCollection line = QaGeometryUtils.CreatePolyline(subcurve);
			((IZAware) line).ZAware = ((IZAware) subcurve).ZAware;
			line.AddSegmentCollection((ISegmentCollection) subcurve);

			return (IPolyline) line;
		}

		bool IIndexedSegments.TryGetSegmentNeighborhoods(
			IIndexedSegments neighborSegments, IBox commonBox, double searchDistance,
			out IEnumerable<SegmentProxyNeighborhood> neighborhoods)
		{
			neighborhoods = null;
			return false;
		}

		private class SegmentEnumerable : IEnumerable<SegmentProxy>
		{
			private readonly IEnumSegment _enumSegment;

			public SegmentEnumerable([NotNull] IEnumSegment enumSegment)
			{
				_enumSegment = enumSegment;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			IEnumerator<SegmentProxy> IEnumerable<SegmentProxy>.GetEnumerator()
			{
				return GetEnumerator();
			}

			[NotNull]
			private SegmentEnumerator GetEnumerator()
			{
				return new SegmentEnumerator(_enumSegment);
			}
		}

		private class SegmentEnumerator : IEnumerator<SegmentProxy>
		{
			private readonly bool _isRecycling;
			private readonly IEnumSegment _enumSegment;
			private SegmentProxy _currentSeg;

			public SegmentEnumerator([NotNull] IEnumSegment enumSegment)
			{
				Assert.ArgumentNotNull(enumSegment, nameof(enumSegment));

				_enumSegment = enumSegment;
				_isRecycling = enumSegment.IsRecycling;
			}

			public void Dispose() { }

			public bool MoveNext()
			{
				ISegment segment;
				int partIndex = 0;
				int segIndex = 0;
				_enumSegment.Next(out segment, ref partIndex, ref segIndex);

				if (segment == null)
				{
					_currentSeg = null;
					return false;
				}

				ISegment proxiedSegment;
				if (_isRecycling)
				{
					proxiedSegment = GeometryFactory.Clone(segment);

					// release the segment, otherwise "pure virtual function call" occurs 
					// when there are certain circular arcs (IsLine == true ?)
					Marshal.ReleaseComObject(segment);
				}
				else
				{
					proxiedSegment = segment;
				}

				_currentSeg = new AoSegmentProxy(proxiedSegment, partIndex, segIndex);
				return true;
			}

			public void Reset()
			{
				_enumSegment.Reset();
			}

			object IEnumerator.Current => Current;

			public SegmentProxy Current => _currentSeg;
		}
	}
}
