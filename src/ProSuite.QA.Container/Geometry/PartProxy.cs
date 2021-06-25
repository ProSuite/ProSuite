using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.QA.Container.Geometry
{
	internal class PartProxy : IWKSPointCollection
	{
		private readonly bool _hasNonLinearSegs;
		private readonly WKSPointZ[] _points;

		private readonly int _partIndex;

		private readonly Dictionary<int, AoSegmentProxy> _nonLinearSegments;

		#region Constructors

		public PartProxy([NotNull] BoxTree<SegmentProxy> boxTree,
		                 int partIndex,
		                 [NotNull] IPointCollection4 baseGeometry)
		{
			_partIndex = partIndex;
			SpatialReference = ((IGeometry) baseGeometry).SpatialReference;

			_points = new WKSPointZ[baseGeometry.PointCount];
			GeometryUtils.QueryWKSPointZs(baseGeometry, _points);

			var segmentCollection = baseGeometry as ISegmentCollection;

			if (segmentCollection == null)
			{
				return;
			}

			SegmentCount = segmentCollection.SegmentCount;
			IsClosed = ((ICurve) segmentCollection).IsClosed;

			segmentCollection.HasNonLinearSegments(ref _hasNonLinearSegs);

			if (_hasNonLinearSegs)
			{
				_nonLinearSegments = new Dictionary<int, AoSegmentProxy>();

				IEnumSegment enumSeg = segmentCollection.EnumSegments;
				bool recycling = enumSeg.IsRecycling;

				ISegment segment;
				int outPartIndex = 0;
				int outSegmentIndex = 0;

				enumSeg.Next(out segment, ref outPartIndex, ref outSegmentIndex);

				while (segment != null)
				{
					var line = segment as ILine;
					SegmentProxy segmentProxy;
					if (line != null)
					{
						segmentProxy = new WksSegmentProxy(this, _partIndex, outSegmentIndex);
					}
					else
					{
						var aoSegmentProxy = new AoSegmentProxy(recycling
							                                        ? GeometryFactory.Clone(segment)
							                                        : segment,
						                                        _partIndex, outSegmentIndex);

						_nonLinearSegments.Add(outSegmentIndex, aoSegmentProxy);
						segmentProxy = aoSegmentProxy;
					}

					boxTree.Add(segmentProxy.Extent, segmentProxy);

					if (recycling)
					{
						Marshal.ReleaseComObject(segment);
					}

					enumSeg.Next(out segment, ref outPartIndex, ref outSegmentIndex);
				}
			}
			else
			{
				int segmentCount = segmentCollection.SegmentCount;
				for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
				{
					var wksSegmentProxy = new WksSegmentProxy(this, _partIndex, segmentIndex);

					boxTree.Add(wksSegmentProxy.Extent, wksSegmentProxy);
				}
			}
		}

		#endregion

		[NotNull]
		public IList<WKSPointZ> Points => _points;

		public int SegmentCount { get; }

		public bool IsClosed { get; }

		public ISpatialReference SpatialReference { get; }

		[NotNull]
		public IEnumerable<SegmentProxy> GetSegments()
		{
			for (int i = 0; i < SegmentCount; i++)
			{
				SegmentProxy seg = GetSegment(i);
				yield return seg;
			}
		}

		[NotNull]
		public SegmentProxy GetSegment(int segmentIndex)
		{
			AoSegmentProxy nonLinearSegmentProxy;
			if (_nonLinearSegments != null &&
			    _nonLinearSegments.TryGetValue(segmentIndex, out nonLinearSegmentProxy))
			{
				return nonLinearSegmentProxy;
			}

			return new WksSegmentProxy(this, _partIndex, segmentIndex);
		}

		[NotNull]
		public IPolyline GetSubpart(int startSegmentIndex, double startFraction,
		                            int endSegmentIndex, double endFraction)
		{
			IPolyline subpart = _nonLinearSegments != null
				                    ? GetNonLinearSubpart(startSegmentIndex, startFraction,
				                                          endSegmentIndex, endFraction)
				                    : GetLinearSubpart(startSegmentIndex, startFraction,
				                                       endSegmentIndex, endFraction);

			return subpart;
		}

		[NotNull]
		private IPolyline GetNonLinearSubpart(int startSegmentIndex, double startFraction,
		                                      int endSegmentIndex, double endFraction)
		{
			var subpart = new PolylineClass();
			IPointCollection4 points = subpart;
			ISegmentCollection segs = subpart;
			subpart.SpatialReference = SpatialReference;

			bool hasNonLinearParts = false;

			object missing = Type.Missing;
			SegmentProxy segProxy;
			AoSegmentProxy aoSegProxy;

			#region startSegment

			var currentWksPoints = new List<WKSPointZ>();
			if (_nonLinearSegments.TryGetValue(startSegmentIndex, out aoSegProxy))
			{
				hasNonLinearParts = true;
				ISegment seg = aoSegProxy.InnerSegment;
				ICurve part;

				double end = 1;
				if (endSegmentIndex == startSegmentIndex)
				{
					end = endFraction;
				}

				seg.GetSubcurve(startFraction, end, true, out part);

				segs.AddSegment((ISegment) part, ref missing, ref missing);
			}
			else
			{
				segProxy = GetSegment(startSegmentIndex);
				IPnt p = segProxy.GetPointAt(startFraction, as3D: true);
				currentWksPoints.Add(QaGeometryUtils.GetWksPoint(p));
			}

			#endregion

			#region segments

			for (int i = startSegmentIndex + 1; i < endSegmentIndex; i++)
			{
				if (_nonLinearSegments.TryGetValue(i, out aoSegProxy))
				{
					hasNonLinearParts = true;

					if (currentWksPoints.Count > 0)
					{
						currentWksPoints.Add(_points[i]);
						WKSPointZ[] add = currentWksPoints.ToArray();
						GeometryUtils.AddWKSPointZs(points, add);
						currentWksPoints.Clear();
					}

					ISegment seg = GeometryFactory.Clone(aoSegProxy.InnerSegment);
					segs.AddSegment(seg, ref missing, ref missing);
				}
				else
				{
					currentWksPoints.Add(_points[i]);
				}
			}

			#endregion

			#region endsegment

			if (startSegmentIndex == endSegmentIndex)
			{
				if (currentWksPoints.Count > 0)
				{
					segProxy = GetSegment(endSegmentIndex);
					IPnt p = segProxy.GetPointAt(endFraction, as3D: true);
					currentWksPoints.Add(QaGeometryUtils.GetWksPoint(p));
					WKSPointZ[] add = currentWksPoints.ToArray();
					GeometryUtils.AddWKSPointZs(points, add);
				}
			}
			else
			{
				if (_nonLinearSegments.TryGetValue(endSegmentIndex, out aoSegProxy))
				{
					hasNonLinearParts = false;
					if (currentWksPoints.Count > 0)
					{
						currentWksPoints.Add(_points[endSegmentIndex]);
						WKSPointZ[] add = currentWksPoints.ToArray();
						GeometryUtils.AddWKSPointZs(points, add);
						currentWksPoints.Clear();
					}

					ISegment seg = aoSegProxy.InnerSegment;
					ICurve part;
					seg.GetSubcurve(0, endFraction, true, out part);
					segs.AddSegment((ISegment) part, ref missing, ref missing);
				}
				else
				{
					currentWksPoints.Add(_points[endSegmentIndex]);
					segProxy = GetSegment(endSegmentIndex);

					IPnt p = segProxy.GetPointAt(endFraction, as3D: true);
					currentWksPoints.Add(QaGeometryUtils.GetWksPoint(p));

					WKSPointZ[] add = currentWksPoints.ToArray();
					GeometryUtils.AddWKSPointZs(points, add);
				}
			}

			#endregion

			if (hasNonLinearParts)
			{
				var topoOp = (ITopologicalOperator2) subpart;
				topoOp.IsKnownSimple_2 = false;
				topoOp.Simplify();
			}

			return subpart;
		}

		private IPolyline GetLinearSubpart(int startSegmentIndex, double startFraction,
		                                   int endSegmentIndex, double endFraction)
		{
			IPointCollection4 subpart = new PolylineClass();

			int add = 2;
			if (endFraction == 0)
			{
				add = 1;
			}

			int pointCount = endSegmentIndex - startSegmentIndex + add;
			var points = new WKSPointZ[pointCount];

			SegmentProxy seg0 = GetSegment(startSegmentIndex);
			IPnt p = seg0.GetPointAt(startFraction, as3D: true);

			points[0] = QaGeometryUtils.GetWksPoint(p);
			for (int i = startSegmentIndex + 1; i <= endSegmentIndex; i++)
			{
				points[i - startSegmentIndex] = _points[i];
			}

			if (endFraction > 0)
			{
				SegmentProxy seg1 = GetSegment(endSegmentIndex);
				IPnt end = seg1.GetPointAt(endFraction, as3D: true);
				points[pointCount - 1] = QaGeometryUtils.GetWksPoint(end);
			}

			GeometryUtils.SetWKSPointZs(subpart, points);

			((IPolyline) subpart).SpatialReference = SpatialReference;
			return (IPolyline) subpart;
		}
	}
}
