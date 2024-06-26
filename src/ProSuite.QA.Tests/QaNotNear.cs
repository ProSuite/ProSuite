using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaNotNear : QaNearCoincidenceBase
	{
		private readonly IReadOnlyFeatureClass _reference;

		[NotNull] private readonly bool? _self;
		// nullable/notnull to ensure initialization in constructors

		private IFeatureClassFilter _filter;
		private QueryFilterHelper _helper;

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaNotNear(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaNotNear_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, is3D, tileSize: 1000.0) { }

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: base(new[] {featureClass}, near,
			       new ConstantFeatureDistanceProvider(near / 2),
			       new ConstantPairDistanceProvider(minLength),
			       new ConstantPairDistanceProvider(minLength), is3D,
			       coincidenceTolerance: 0)
		{
			_reference = featureClass;
			_self = true;
		}

		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaNotNear(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaNotNear_reference))]
				IReadOnlyFeatureClass reference,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaNotNear_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D: is3D,
			       tileSize: 1000.0) { }

		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: base(new[] {featureClass, reference},
			       near, new ConstantFeatureDistanceProvider(near / 2),
			       new ConstantPairDistanceProvider(minLength),
			       new ConstantPairDistanceProvider(minLength), is3D, 0)
		{
			_reference = reference;
			_self = false;
		}

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaNotNear(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, is3D: false, tileSize: 1000.0) { }

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(featureClass, near, minLength, is3D: false, tileSize: tileSize) { }

		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaNotNear(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaNotNear_reference))]
				IReadOnlyFeatureClass reference,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D: false, tileSize: 1000.0) { }

		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(featureClass, reference, near, minLength, is3D: false) { }

		/// <summary>
		/// Constructor using Definition. Must always be the last constructor!
		/// </summary>
		/// <param name="notNearDef"></param>
		[InternallyUsedTest]
		public QaNotNear([NotNull] QaNotNearDefinition notNearDef)
			: base(notNearDef.InvolvedTables.Cast<IReadOnlyFeatureClass>(),
			       notNearDef.Near, new ConstantFeatureDistanceProvider(notNearDef.Near / 2),
			       new ConstantPairDistanceProvider(notNearDef.MinLength),
			       new ConstantPairDistanceProvider(notNearDef.MinLength), notNearDef.Is3D, 0)
		{
			bool hasReference = notNearDef.Reference != null;

			if (hasReference)
			{
				_reference = (IReadOnlyFeatureClass) notNearDef.Reference;
				_self = false;
			}
			else
			{
				_reference = (IReadOnlyFeatureClass) notNearDef.FeatureClass;
				_self = true;
			}
		}

		protected override bool IsDirected => ! _self.Value;

		[TestParameter]
		[Doc(nameof(DocStrings.QaNotNear_IgnoreNeighborCondition))]
		public string IgnoreNeighborCondition
		{
			get => _ignoreNeighborConstraint;
			set
			{
				_ignoreNeighborConstraint = value; 
				AddCustomQueryFilterExpression(value);
			}
		}

		private IgnoreRowNeighborCondition _ignoreNeighborCondition;
		private bool _isIgnoreNeighborConditionInitialized;
		private string _ignoreNeighborConstraint;

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (tableIndex > 0)
			{
				return NoError;
			}

			var feature = row as IReadOnlyFeature;
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

			IFeatureClassFilter filter = Assert.NotNull(_filter);
			filter.FilterGeometry = box0;

			var errorCount = 0;

			double maxNear = SearchDistance;
			const int referenceTableIndex = 0;
			IFeatureRowsDistance rowsDistance =
				NearDistanceProvider.GetRowsDistance(feature, tableIndex);

			foreach (IReadOnlyRow neighborRow in Search(_reference, filter, _helper))
			{
				var neighborFeature = (IReadOnlyFeature) neighborRow;

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
			IFeatureRowsDistance rowsDistance, IReadOnlyFeature feature, int tableIndex,
			IReadOnlyFeature neighbor, int neighborTableIndex)
		{
			return new NotNearNeighborhoodFinder(
				rowsDistance, feature, tableIndex, neighbor, neighborTableIndex);
		}

		private bool IgnoreNeighbor(IReadOnlyRow row, IReadOnlyRow neighbor)
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
			IReadOnlyFeature feat0, int tableIndex,
			SortedDictionary<SegmentPart, SegmentParts> processed0,
			IReadOnlyFeature feat1, int neighborTableIndex,
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
			IList<IFeatureClassFilter> filter;
			IList<QueryFilterHelper> helper;

			CopyFilters(out filter, out helper);

			int tableIndex = _self.Value
				                 ? 0
				                 : 1;

			_filter = filter[tableIndex];
			_helper = helper[tableIndex];

			_filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
		}

		protected sealed class NotNearNeighborhoodFinder : NeighborhoodFinder
		{
			public NotNearNeighborhoodFinder(
				[NotNull] IFeatureRowsDistance rowsDistance,
				[NotNull] IReadOnlyFeature feature, int tableIndex,
				[CanBeNull] IReadOnlyFeature neighbor, int neighborTableIndex)
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
