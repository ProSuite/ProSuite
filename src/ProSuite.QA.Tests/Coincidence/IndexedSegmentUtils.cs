using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Coincidence
{
	public static class IndexedSegmentUtils
	{
		[NotNull]
		public static IIndexedSegments GetIndexedGeometry([NotNull] IReadOnlyFeature feature,
		                                                  bool releaseOnDispose)
		{
			var polycurveProxy = feature as IIndexedPolycurveFeature;
			if (polycurveProxy != null)
			{
				return polycurveProxy.IndexedSegments;
			}

			IGeometry geometry = releaseOnDispose
				                     ? feature.ShapeCopy
				                     : feature.Shape;

			return new SegmentSearcher((ISegmentCollection) geometry, releaseOnDispose);
		}

		internal static double GetLength([NotNull] IIndexedSegments baseGeometry,
		                                 int partIndex,
		                                 int startSegmentIndex, double startFraction,
		                                 int endSegmentIndex, double endFraction)
		{
			double length = 0;

			for (int i = startSegmentIndex; i <= endSegmentIndex; i++)
			{
				SegmentProxy seg = baseGeometry.GetSegment(partIndex, i);

				double segLength = seg.Length;
				double addLength = segLength;

				if (i == startSegmentIndex)
				{
					addLength -= segLength * startFraction;
				}

				if (i == endSegmentIndex)
				{
					addLength -= segLength * (1 - endFraction);
				}

				length += addLength;
			}

			return length;
		}

		public static WKSEnvelope GetEnvelope([NotNull] IIndexedSegments baseGeometry,
		                                      int part, int startSegmentIndex,
		                                      double startFraction,
		                                      int endSegmentIndex, double endFraction)
		{
			SegmentProxy segment = baseGeometry.GetSegment(part, startSegmentIndex);
			double startEnd = 1;
			if (startSegmentIndex == endSegmentIndex)
			{
				startEnd = endFraction;
			}

			WKSEnvelope box = segment.GetSubCurveBox(startFraction, startEnd);

			for (int i = startSegmentIndex + 1; i < endSegmentIndex - 1; i++)
			{
				segment = baseGeometry.GetSegment(part, i);
				box = GetUnion(box, segment.GetSubCurveBox(0, 1));
			}

			if (endSegmentIndex > startSegmentIndex && endFraction > 0)
			{
				segment = baseGeometry.GetSegment(part, endSegmentIndex);
				box = GetUnion(box, segment.GetSubCurveBox(0, endFraction));
			}

			return box;
		}

		private static WKSEnvelope GetUnion(WKSEnvelope x, WKSEnvelope y)
		{
			y.XMin = Math.Min(x.XMin, y.XMin);
			y.XMax = Math.Max(x.XMax, y.XMax);
			y.YMin = Math.Min(x.YMin, y.YMin);
			y.YMax = Math.Max(x.YMax, y.YMax);

			return y;
		}
	}
}
