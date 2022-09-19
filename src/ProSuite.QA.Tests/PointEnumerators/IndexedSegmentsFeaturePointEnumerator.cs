using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestContainer;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using Pnt = ProSuite.Commons.Geom.Pnt;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests.PointEnumerators
{
	internal class IndexedSegmentsFeaturePointEnumerator : PointsEnumerator
	{
		[NotNull] private readonly IIndexedSegmentsFeature _indexedSegmentsFeature;
		[CanBeNull] private readonly Box _searchBox;

		public IndexedSegmentsFeaturePointEnumerator(
			[NotNull] IIndexedSegmentsFeature indexedSegmentsFeature,
			[CanBeNull] IEnvelope envelope)
			: base((IReadOnlyFeature) indexedSegmentsFeature)
		{
			_indexedSegmentsFeature = indexedSegmentsFeature;

			if (envelope != null && ! ((IRelationalOperator) envelope).Contains(Feature.Shape))
			{
				_searchBox = QaGeometryUtils.CreateBox(envelope);
			}
		}

		public override IEnumerable<Pnt> GetPoints()
		{
			return GetPointsCore(_searchBox);
		}

		public override IEnumerable<Pnt> GetPoints(IBox searchBox)
		{
			if (searchBox == null)
			{
				return GetPointsCore(_searchBox);
			}

			if (_searchBox == null)
			{
				return GetPointsCore(searchBox);
			}

			if (! _searchBox.Intersects(searchBox))
			{
				return new Pnt[] { };
			}

			const bool verify = true;
			var common = new Box(
				new Box(_searchBox.Min,
				        Pnt.Create(searchBox.Min), verify)
					.Max,
				new Box(_searchBox.Max,
				        Pnt.Create(searchBox.Max), verify)
					.Min);
			return GetPointsCore(common);
		}

		private IEnumerable<Pnt> GetPointsCore([CanBeNull] IBox searchBox)
		{
			IIndexedSegments indexedSegments = _indexedSegmentsFeature.IndexedSegments;

			const bool as3D = true;

			IEnumerable<SegmentProxy> segments = searchBox == null
				                                     ? indexedSegments.GetSegments()
				                                     : indexedSegments.GetSegments(searchBox);

			foreach (SegmentProxy segmentProxy in segments)
			{
				Pnt start = segmentProxy.GetStart(as3D);
				if (searchBox == null || searchBox.Intersects(start))
				{
					yield return start;
				}

				int partIndex = segmentProxy.PartIndex;

				if (! indexedSegments.IsPartClosed(partIndex) &&
				    indexedSegments.GetPartSegmentCount(partIndex) ==
				    segmentProxy.SegmentIndex + 1)
				{
					Pnt end = segmentProxy.GetEnd(as3D);
					if (searchBox == null || searchBox.Intersects(end))
					{
						yield return end;
					}
				}
			}
		}
	}
}
