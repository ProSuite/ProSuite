using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.PointEnumerators
{
	public abstract class SegmentsPlaneProvider
	{
		private readonly IIndexedSegments _indexedSegments;

		public static SegmentsPlaneProvider Create([NotNull] IReadOnlyFeature feature,
		                                           bool includeAssociatedParts)
		{
			return Create(feature, feature.Shape.GeometryType, includeAssociatedParts);
		}

		public static SegmentsPlaneProvider Create(
			[NotNull] IReadOnlyFeature feature,
			esriGeometryType shapeType,
			bool includeAssociatedParts)
		{
			if (shapeType == esriGeometryType.esriGeometryMultiPatch)
			{
				IIndexedMultiPatch indexedMultiPatch = GetIndexedMultiPatch(feature);

				return includeAssociatedParts
					       ? (SegmentsPlaneProvider)
					       new SurfaceSegmentsPlaneProvider(indexedMultiPatch)
					       : new PartSegmentsPlaneProvider(indexedMultiPatch, shapeType);
			}

			IIndexedSegments indexedSegments = GetIndexedSegments(feature);

			return includeAssociatedParts
				       ? (SegmentsPlaneProvider)
				       new FullSegmentsPlaneProvider(indexedSegments, shapeType)
				       : new PartSegmentsPlaneProvider(indexedSegments, shapeType);
		}

		[NotNull]
		private static IIndexedSegments GetIndexedSegments([NotNull] IReadOnlyFeature feature)
		{
			var indexedSegmentsFeature = feature as IIndexedPolycurveFeature;

			return indexedSegmentsFeature != null &&
			       indexedSegmentsFeature.AreIndexedSegmentsLoaded
				       ? indexedSegmentsFeature.IndexedSegments
				       : new IndexedPolycurve((IPointCollection4) feature.Shape);
		}

		[NotNull]
		private static IIndexedMultiPatch GetIndexedMultiPatch([NotNull] IReadOnlyFeature feature)
		{
			var indexedMultiPatchFeature = feature as IIndexedMultiPatchFeature;

			return indexedMultiPatchFeature != null
				       ? indexedMultiPatchFeature.IndexedMultiPatch
				       : ProxyUtils.CreateIndexedMultiPatch(
					       (IMultiPatch) feature.Shape);
		}

		private SegmentsPlaneProvider([NotNull] IIndexedSegments indexedSegments)
		{
			_indexedSegments = indexedSegments;
		}

		[NotNull]
		public IIndexedSegments IndexedSegments => _indexedSegments;

		[CanBeNull]
		public abstract SegmentsPlane ReadPlane();

		private class PartSegmentsPlaneProvider : SegmentsPlaneProvider
		{
			private readonly esriGeometryType _shapeType;
			private readonly IEnumerator<SegmentProxy> _segmentsEnum;
			private bool _enumValid;

			public PartSegmentsPlaneProvider([NotNull] IIndexedSegments indexedSegments,
			                                 esriGeometryType shapeType)
				: base(indexedSegments)
			{
				_shapeType = shapeType;
				_segmentsEnum = _indexedSegments.GetSegments().GetEnumerator();
				_enumValid = _segmentsEnum.MoveNext();
			}

			public override SegmentsPlane ReadPlane()
			{
				if (! _enumValid)
				{
					return null;
				}

				int currentPart = Assert.NotNull(_segmentsEnum.Current).PartIndex;
				int segmentCount = _indexedSegments.GetPartSegmentCount(currentPart);

				var partSegments = new List<SegmentProxy>(segmentCount)
				                   { _segmentsEnum.Current };

				while ((_enumValid = _segmentsEnum.MoveNext()) &&
				       Assert.NotNull(_segmentsEnum.Current).PartIndex == currentPart)
				{
					partSegments.Add(_segmentsEnum.Current);
				}

				return new SegmentsPlane(partSegments, _shapeType);
			}
		}

		private class FullSegmentsPlaneProvider : SegmentsPlaneProvider
		{
			private readonly esriGeometryType _shapeType;
			private bool _enumValid;

			public FullSegmentsPlaneProvider([NotNull] IIndexedSegments indexedSegments,
			                                 esriGeometryType shapeType)
				: base(indexedSegments)
			{
				_shapeType = shapeType;
				_enumValid = true;
			}

			public override SegmentsPlane ReadPlane()
			{
				if (! _enumValid)
				{
					return null;
				}

				IEnumerable<SegmentProxy> segments = _indexedSegments.GetSegments();

				_enumValid = false;
				return new SegmentsPlane(segments, _shapeType);
			}
		}

		private class SurfaceSegmentsPlaneProvider : SegmentsPlaneProvider
		{
			private readonly IIndexedMultiPatch _indexedMultiPatch;
			private readonly IEnumerator<SegmentProxy> _segmentsEnum;
			private bool _enumValid;

			public SurfaceSegmentsPlaneProvider(
				[NotNull] IIndexedMultiPatch indexedMultiPatch)
				: base(indexedMultiPatch)
			{
				_indexedMultiPatch = indexedMultiPatch;
				_segmentsEnum = _indexedMultiPatch.GetSegments().GetEnumerator();
				_enumValid = _segmentsEnum.MoveNext();
			}

			public override SegmentsPlane ReadPlane()
			{
				if (! _enumValid)
				{
					return null;
				}

				int currentPart = Assert.NotNull(_segmentsEnum.Current).PartIndex;
				int patchIndex = _indexedMultiPatch.GetPatchIndex(currentPart);

				int endPart = currentPart;

				var geometryCollection =
					(IGeometryCollection) _indexedMultiPatch.BaseGeometry;
				var maybeOuterRing = geometryCollection.Geometry[patchIndex] as IRing;

				if (maybeOuterRing != null)
				{
					var multiPatch = (IMultiPatch) geometryCollection;
					bool isBeginning = false;
					multiPatch.GetRingType(maybeOuterRing, ref isBeginning);
					if (isBeginning)
					{
						int followingRingCount =
							multiPatch.FollowingRingCount[maybeOuterRing];
						endPart += followingRingCount;
					}
				}

				int segmentCount = _indexedMultiPatch.GetPartSegmentCount(currentPart);
				var surfaceSegments = new List<SegmentProxy>(segmentCount)
				                      { _segmentsEnum.Current };

				while ((_enumValid = _segmentsEnum.MoveNext()) &&
				       Assert.NotNull(_segmentsEnum.Current).PartIndex <= endPart)
				{
					surfaceSegments.Add(_segmentsEnum.Current);
				}

				return new SegmentsPlane(surfaceSegments,
				                         esriGeometryType.esriGeometryMultiPatch);
			}
		}
	}
}
