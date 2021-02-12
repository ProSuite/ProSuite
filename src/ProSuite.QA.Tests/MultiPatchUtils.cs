using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	public static class MultiPatchUtils
	{
		[NotNull]
		public static IPolygon GetPolygon([NotNull] IEnumerable<WKSPointVA> points)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			IPointCollection5 mps = new MultipointClass();

			foreach (WKSPointVA point in points)
			{
				WKSPointVA p = point;
				mps.InsertWKSPointVA(0, 1, ref p);
			}

			IPointCollection poly = new PolygonClass();
			poly.AddPointCollection(mps);
			return (IPolygon) poly;
		}

		[NotNull]
		public static List<WKSPointVA> GetPoints([NotNull] IPointCollection points)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			IPointCollection5 mps = new MultipointClass();
			mps.AddPointCollection(points);

			int pointCount = points.PointCount;

			int iPoint = 0;
			var result = new List<WKSPointVA>(pointCount);

			for (int i = 0; i < pointCount; i++)
			{
				result.Add(GetWKSPointVA(mps, ref iPoint));
			}

			return result;
		}

		[NotNull]
		public static List<List<WKSPointVA>> GetRings([NotNull] IMultiPatch multiPatch)
		{
			Assert.ArgumentNotNull(multiPatch, nameof(multiPatch));

			IPointCollection5 mps = new MultipointClass();
			mps.AddPointCollection((IPointCollection) multiPatch);

			var parts = (IGeometryCollection) multiPatch;
			int partCount = parts.GeometryCount;
			var rings = new List<List<WKSPointVA>>(partCount);

			int pointIndex = 0;
			for (int partIndex = 0; partIndex < partCount; partIndex++)
			{
				IGeometry part = parts.Geometry[partIndex];

				var partPoints = (IPointCollection) part;

				if (part is IRing)
				{
					int ringCount = partPoints.PointCount;
					var ringPoints = new List<WKSPointVA>(ringCount);
					for (int iRingPoint = 0; iRingPoint < ringCount; iRingPoint++)
					{
						ringPoints.Add(GetWKSPointVA(mps, ref pointIndex));
					}

					rings.Add(ringPoints);
				}
				else if (part is ITriangleFan)
				{
					int fanCount = partPoints.PointCount;
					WKSPointVA center = GetWKSPointVA(mps, ref pointIndex);
					WKSPointVA first = GetWKSPointVA(mps, ref pointIndex);

					for (int i = 2; i < fanCount; i++)
					{
						WKSPointVA second = GetWKSPointVA(mps, ref pointIndex);
						var trainglePoints =
							new List<WKSPointVA> {center, first, second, center};
						rings.Add(trainglePoints);

						first = second;
					}
				}
				else if (part is ITriangleStrip)
				{
					int stripCount = partPoints.PointCount;
					WKSPointVA first = GetWKSPointVA(mps, ref pointIndex);
					WKSPointVA second = GetWKSPointVA(mps, ref pointIndex);

					for (int i = 2; i < stripCount; i++)
					{
						WKSPointVA third = GetWKSPointVA(mps, ref pointIndex);
						var tri = new List<WKSPointVA> {first, second, third, first};
						rings.Add(tri);

						first = second;
						second = third;
					}
				}
				else if (part is ITriangles)
				{
					int trianglePointsCount = partPoints.PointCount;
					Assert.AreEqual(trianglePointsCount % 3, 0,
					                string.Format("{0} points in ITriangles",
					                              trianglePointsCount));
					int triangleCount = trianglePointsCount / 3;

					for (int i = 0; i < triangleCount; i++)
					{
						WKSPointVA first = GetWKSPointVA(mps, ref pointIndex);
						WKSPointVA second = GetWKSPointVA(mps, ref pointIndex);
						WKSPointVA third = GetWKSPointVA(mps, ref pointIndex);

						var trianglePoints =
							new List<WKSPointVA> {first, second, third, first};

						rings.Add(trianglePoints);
					}
				}
				else
				{
					throw new InvalidOperationException(string.Format("{0} not handled",
					                                                  part.GeometryType));
				}

				Marshal.ReleaseComObject(part);
			}

			return rings;
		}

		[NotNull]
		public static IPolygon GetFace([NotNull] IMultiPatch multiPatch,
		                               [NotNull] IRing beginningRing)
		{
			Assert.ArgumentNotNull(multiPatch, nameof(multiPatch));
			Assert.ArgumentNotNull(beginningRing, nameof(beginningRing));

			IPolygon face = new PolygonClass
			                {
				                SpatialReference = beginningRing.SpatialReference
			                };

			((IZAware) face).ZAware = true;

			object missing = Type.Missing;
			((IGeometryCollection) face).AddGeometry(GeometryFactory.Clone(beginningRing),
			                                         ref missing, ref missing);

			int followingRingCount = multiPatch.FollowingRingCount[beginningRing];

			if (followingRingCount > 0)
			{
				var followingRings = new IRing[followingRingCount];

				GeometryUtils.GeometryBridge.QueryFollowingRings(
					multiPatch, beginningRing,
					ref followingRings);
				foreach (IRing followingRing in followingRings)
				{
					((IGeometryCollection) face).AddGeometry(
						GeometryFactory.Clone(followingRing),
						ref missing, ref missing);
				}
			}

			return face;
		}

		[NotNull]
		public static List<IPolygon> GetFaces(
			[NotNull] IIndexedMultiPatch indexedMultiPatch,
			int patchIndex)
		{
			IMultiPatch multiPatch = indexedMultiPatch.BaseGeometry;
			var patches = (IGeometryCollection) multiPatch;
			var ring = patches.Geometry[patchIndex] as IRing;

			if (ring != null)
			{
				bool beginning = false;
				multiPatch.GetRingType(ring, ref beginning);

				// TODO handle undefined/invalid ring types?
				return beginning
					       ? new List<IPolygon> {GetFace(multiPatch, ring)}
					       : new List<IPolygon>();
			}

			List<int> partIndexes = indexedMultiPatch.GetPartIndexes(patchIndex);

			var result = new List<IPolygon>(partIndexes.Count);

			foreach (int partIndex in partIndexes)
			{
				WKSPointZ[] wksPoints = GetWksPointZs(indexedMultiPatch, partIndex);

				IPolygon face = GeometryFactory.CreatePolygon(wksPoints,
				                                              multiPatch
					                                              .SpatialReference);

				result.Add(face);
			}

			return result;
		}

		[NotNull]
		public static List<int> GetInnerRingPartIndexes(
			[NotNull] IIndexedMultiPatch indexedMultiPatch,
			int outerRingPartIndex)
		{
			IMultiPatch multiPatch = indexedMultiPatch.BaseGeometry;

			int outerRingIndex = indexedMultiPatch.GetPatchIndex(outerRingPartIndex);

			var geometryCollection = (IGeometryCollection) multiPatch;
			var outerRing = (IRing) geometryCollection.Geometry[outerRingIndex];

			int followingRingCount = multiPatch.FollowingRingCount[outerRing];

			// following rings must follow the outer ring directly
			var result = new List<int>(followingRingCount);

			for (int index = 0; index < followingRingCount; index++)
			{
				result.Add(index + outerRingPartIndex + 1);
			}

			return result;
		}

		private static WKSPointVA GetWKSPointVA([NotNull] IPointCollection5 allPoints,
		                                        ref int pointIndex)
		{
			WKSPointVA wks;
			allPoints.QueryWKSPointVA(pointIndex, 1, out wks);

			pointIndex++;

			return wks;
		}

		[NotNull]
		private static WKSPointZ[] GetWksPointZs(
			[NotNull] IIndexedSegments indexedSegments,
			int partIndex)
		{
			int partSegmentCount = indexedSegments.GetPartSegmentCount(partIndex);

			var segments = new List<SegmentProxy>(partSegmentCount);

			for (int segmentIndex = 0; segmentIndex < partSegmentCount; segmentIndex++)
			{
				segments.Add(indexedSegments.GetSegment(partIndex, segmentIndex));
			}

			var wksPoints = new List<WKSPointZ>(partSegmentCount + 1);

			foreach (Pnt point in QaGeometryUtils.GetPoints(segments))
			{
				wksPoints.Add(QaGeometryUtils.GetWksPoint(point));
			}

			return wksPoints.ToArray();
		}
	}
}
