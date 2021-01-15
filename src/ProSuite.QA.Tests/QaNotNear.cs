using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.Documentation;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[ProximityTest]
	public class QaNotNear : QaNearCoincidenceBase
	{
		private readonly IFeatureClass _reference;

		[NotNull] private readonly bool? _self;
		// nullable/notnull to ensure initialization in constructors

		private ISpatialFilter _filter;
		private QueryFilterHelper _helper;

		[Doc("QaNotNear_0")]
		public QaNotNear(
				[Doc("QaNotNear_featureClass")] IFeatureClass featureClass,
				[Doc("QaNotNear_near")] double near,
				[Doc("QaNotNear_minLength")] double minLength,
				[Doc("QaNotNear_is3D")] bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, is3D, tileSize: 1000.0) { }

		[Doc("QaNotNear_0")]
		public QaNotNear(
			[Doc("QaNotNear_featureClass")] IFeatureClass featureClass,
			[Doc("QaNotNear_near")] double near,
			[Doc("QaNotNear_minLength")] double minLength,
			[Doc("QaNotNear_is3D")] bool is3D,
			[Doc("QaNotNear_tileSize")] double tileSize)
			: base(new[] {featureClass}, near,
			       new ConstantFeatureDistanceProvider(near / 2),
			       new ConstantPairDistanceProvider(minLength),
			       new ConstantPairDistanceProvider(minLength), is3D,
			       coincidenceTolerance: 0)
		{
			_reference = featureClass;
			_self = true;
		}

		[Doc("QaNotNear_2")]
		public QaNotNear(
				[Doc("QaNotNear_featureClass")] IFeatureClass featureClass,
				[Doc("QaNotNear_reference")] IFeatureClass reference,
				[Doc("QaNotNear_near")] double near,
				[Doc("QaNotNear_minLength")] double minLength,
				[Doc("QaNotNear_is3D")] bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D: is3D,
			       tileSize: 1000.0) { }

		[Doc("QaNotNear_2")]
		public QaNotNear(
			[Doc("QaNotNear_featureClass")] IFeatureClass featureClass,
			[Doc("QaNotNear_reference")] IFeatureClass reference,
			[Doc("QaNotNear_near")] double near,
			[Doc("QaNotNear_minLength")] double minLength,
			[Doc("QaNotNear_is3D")] bool is3D,
			[Doc("QaNotNear_tileSize")] double tileSize)
			: base(new[] {featureClass, reference},
			       near, new ConstantFeatureDistanceProvider(near / 2),
			       new ConstantPairDistanceProvider(minLength),
			       new ConstantPairDistanceProvider(minLength), is3D, 0)
		{
			_reference = reference;
			_self = false;
		}

		[Doc("QaNotNear_0")]
		public QaNotNear(
				[Doc("QaNotNear_featureClass")] IFeatureClass featureClass,
				[Doc("QaNotNear_near")] double near,
				[Doc("QaNotNear_minLength")] double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, is3D: false, tileSize: 1000.0) { }

		[Doc("QaNotNear_0")]
		public QaNotNear(
			[Doc("QaNotNear_featureClass")] IFeatureClass featureClass,
			[Doc("QaNotNear_near")] double near,
			[Doc("QaNotNear_minLength")] double minLength,
			[Doc("QaNotNear_tileSize")] double tileSize)
			: this(featureClass, near, minLength, is3D: false, tileSize: tileSize) { }

		[Doc("QaNotNear_2")]
		public QaNotNear(
				[Doc("QaNotNear_featureClass")] IFeatureClass featureClass,
				[Doc("QaNotNear_reference")] IFeatureClass reference,
				[Doc("QaNotNear_near")] double near,
				[Doc("QaNotNear_minLength")] double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D: false, tileSize: 1000.0) { }

		[Doc("QaNotNear_2")]
		public QaNotNear(
			[Doc("QaNotNear_featureClass")] IFeatureClass featureClass,
			[Doc("QaNotNear_reference")] IFeatureClass reference,
			[Doc("QaNotNear_near")] double near,
			[Doc("QaNotNear_minLength")] double minLength,
			[Doc("QaNotNear_tileSize")] double tileSize)
			: this(featureClass, reference, near, minLength, is3D: false) { }

		protected override bool IsDirected => ! _self.Value;

		[TestParameter]
		[Doc("QaNotNear_IgnoreNeighborCondition")]
		public string IgnoreNeighborCondition { get; set; }

		private IgnoreRowNeighborCondition _ignoreNeighborCondition;
		private bool _isIgnoreNeighborConditionInitialized;

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			if (tableIndex > 0)
			{
				return NoError;
			}

			var feature = row as IFeature;
			if (feature == null)
			{
				return NoError;
			}

			if (_filter == null)
			{
				InitFilter();
				Assert.NotNull(_filter, "_filter");
			}

			var processed0 = new SegmentNeighbors(new SegmentPartComparer());

			IGeometry geom0 = feature.Shape;
			IEnvelope box0 = geom0.Envelope;

			const bool asRatio = false;
			box0.Expand(SearchDistance, SearchDistance, asRatio);

			ISpatialFilter filter = Assert.NotNull(_filter);
			filter.Geometry = box0;

			var errorCount = 0;

			double maxNear = SearchDistance;
			const int referenceTableIndex = 0;
			IFeatureRowsDistance rowsDistance =
				NearDistanceProvider.GetRowsDistance(feature, tableIndex);

			foreach (IRow neighborRow in Search((ITable) _reference, filter, _helper, geom0))
			{
				var neighborFeature = (IFeature) neighborRow;

				if (neighborFeature == feature)
				{
					continue;
				}

				// TODO apply comparison condition to filter out irrelevant pairs
				if (IgnoreNeighbor(row, neighborRow))
				{
					continue;
				}

				SegmentNeighbors processed1;
				var neighborKey = new RowKey(neighborFeature, referenceTableIndex);
				if (! ProcessedList.TryGetValue(neighborKey, out processed1))
				{
					processed1 = new SegmentNeighbors(new SegmentPartComparer());

					ProcessedList.Add(neighborKey, processed1);
				}

				NeighborhoodFinder finder = new NotNearNeighborhoodFinder(
					rowsDistance, feature, tableIndex, neighborFeature, referenceTableIndex);

				errorCount += FindNeighborhood(finder, tableIndex, processed0,
				                               referenceTableIndex, processed1,
				                               maxNear);
			}

			return errorCount;
		}

		protected override NeighborhoodFinder GetNeighborhoodFinder(
			IFeatureRowsDistance rowsDistance, IFeature feature, int tableIndex,
			IFeature neighbor, int neighborTableIndex)
		{
			return new NotNearNeighborhoodFinder(
				rowsDistance, feature, tableIndex, neighbor, neighborTableIndex);
		}

		private bool IgnoreNeighbor(IRow row, IRow neighbor)
		{
			EnsureIgnoreNeighborInitialized();
			if (_ignoreNeighborCondition == null)
			{
				return false;
			}

			return _ignoreNeighborCondition.IsFulfilled(row, 0, neighbor, 1);
		}

		private void EnsureIgnoreNeighborInitialized()
		{
			if (_isIgnoreNeighborConditionInitialized)
			{
				return;
			}

			if (! string.IsNullOrEmpty(IgnoreNeighborCondition))
			{
				_ignoreNeighborCondition = new IgnoreRowNeighborCondition(
					IgnoreNeighborCondition, GetSqlCaseSensitivity(), IsDirected);
			}

			_isIgnoreNeighborConditionInitialized = true;
		}

		protected override int Check(
			IFeature feat0, int tableIndex,
			SortedDictionary<SegmentPart, SegmentParts> processed0,
			IFeature feat1, int neighborTableIndex,
			SortedDictionary<SegmentPart, SegmentParts> processed1,
			double near)
		{
			foreach (SegmentParts polySegments in processed0.Values)
			{
				foreach (SegmentPart segmentPart in polySegments)
				{
					segmentPart.Complete = false;
				}
			}

			return base.Check(feat0, tableIndex, processed0,
			                  feat1, neighborTableIndex, processed1,
			                  near);
		}

		private void InitFilter()
		{
			IList<ISpatialFilter> filter;
			IList<QueryFilterHelper> helper;

			CopyFilters(out filter, out helper);

			int tableIndex = _self.Value
				                 ? 0
				                 : 1;

			_filter = filter[tableIndex];
			_helper = helper[tableIndex];

			_filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
		}

		protected sealed class NotNearNeighborhoodFinder : NeighborhoodFinder
		{
			public NotNearNeighborhoodFinder(
				[NotNull] IFeatureRowsDistance rowsDistance,
				[NotNull] IFeature feature, int tableIndex,
				[CanBeNull] IFeature neighbor, int neighborTableIndex)
				: base(rowsDistance, feature, tableIndex, neighbor, neighborTableIndex) { }

			protected override bool VerifyContinue(SegmentProxy seg0, SegmentProxy seg1,
			                                       SegmentNeighbors processed1,
			                                       SegmentParts partsOfSeg0, bool coincident)
			{
				return true;
			}
		}
	}
}
