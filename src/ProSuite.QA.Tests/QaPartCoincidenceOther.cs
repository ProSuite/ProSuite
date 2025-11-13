using System.Collections.Generic;
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
	public class QaPartCoincidenceOther : QaNearCoincidenceBase
	{
		private readonly IReadOnlyFeatureClass _reference;
		private IFeatureClassFilter _filter;
		private QueryFilterHelper _helper;

		private IgnoreRowNeighborCondition _ignoreNeighborCondition;
		private bool _isIgnoreNeighborConditionInitialized;
		private string _ignoreNeighborConstraint;

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_0))]
		public QaPartCoincidenceOther(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
				IReadOnlyFeatureClass reference,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_0))]
		public QaPartCoincidenceOther(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(featureClass, reference, near, minLength, minLength, is3D, tileSize, 0)
		{
			_reference = reference;
		}

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_0))]
		public QaPartCoincidenceOther(
				[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
				IReadOnlyFeatureClass reference,
				[Doc(nameof(DocStrings.QaPartCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, false, 1000.0) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_0))]
		public QaPartCoincidenceOther(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize)
			: this(featureClass, reference, near, minLength, false, tileSize) { }

		[Doc(nameof(DocStrings.QaPartCoincidenceOther_4))]
		public QaPartCoincidenceOther(
			[Doc(nameof(DocStrings.QaPartCoincidence_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaPartCoincidence_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaPartCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaPartCoincidence_connectedMinLength))]
			double connectedMinLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_disjointMinLength))]
			double disjointMinLength,
			[Doc(nameof(DocStrings.QaPartCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaPartCoincidence_tileSize))]
			double tileSize,
			[Doc(nameof(DocStrings.QaPartCoincidence_coincidenceTolerance))]
			double coincidenceTolerance)
			: base(
				new[] {featureClass, reference}, near,
				new ConstantFeatureDistanceProvider(near / 2),
				new ConstantPairDistanceProvider(connectedMinLength),
				new ConstantPairDistanceProvider(disjointMinLength),
				is3D, coincidenceTolerance)
		{
			_reference = reference;
		}

		protected override bool IsDirected => true;

		[InternallyUsedTest]
		public QaPartCoincidenceOther([NotNull] QaPartCoincidenceOtherDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       (IReadOnlyFeatureClass) definition.Reference, definition.Near,
			       definition.ConnectedMinLength, definition.DisjointMinLength,
			       definition.Is3D, definition.TileSize, definition.CoincidenceTolerance)
		{
			IgnoreNeighborCondition = definition.IgnoreNeighborCondition;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaPartCoincidenceOther_IgnoreNeighborCondition))]
		public string IgnoreNeighborCondition
		{
			get => _ignoreNeighborConstraint;
			set
			{
				_ignoreNeighborConstraint = value;
				AddCustomQueryFilterExpression(value);
			}
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (tableIndex > 0)
			{
				return NoError;
			}

			if (_filter == null)
			{
				InitFilter();
			}

			IFeatureClassFilter filter = Assert.NotNull(_filter, "_filter");

			var processed0 = new SegmentNeighbors(new SegmentPartComparer());

			IGeometry geom0 = ((IReadOnlyFeature) row).Shape;
			IEnvelope box0 = geom0.Envelope;
			box0.Expand(SearchDistance, SearchDistance, false);

			filter.FilterGeometry = box0;

			var errorCount = 0;

			double maxNear = SearchDistance;
			const int neighborTableIndex = 1;

			IFeatureRowsDistance rowsDistance =
				NearDistanceProvider.GetRowsDistance(row, tableIndex);
			foreach (IReadOnlyRow neighborRow in
			         Search(_reference, filter, _helper))
			{
				var rowNeighbor = (IReadOnlyFeature) neighborRow;

				if (IgnoreNeighbor(row, neighborRow))
				{
					continue;
				}

				SegmentNeighbors processed1;
				var neighborKey = new RowKey(rowNeighbor, neighborTableIndex);
				if (! ProcessedList.TryGetValue(neighborKey, out processed1))
				{
					processed1 = new SegmentNeighbors(new SegmentPartComparer());
					ProcessedList.Add(neighborKey, processed1);
				}

				NeighborhoodFinder finder =
					GetNeighborhoodFinder(rowsDistance, (IReadOnlyFeature) row, tableIndex,
					                      rowNeighbor, neighborTableIndex);
				errorCount += FindNeighborhood(finder, tableIndex, processed0,
				                               neighborTableIndex, processed1,
				                               maxNear);
			}

			return errorCount;
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
					IgnoreNeighborCondition, GetSqlCaseSensitivity(),
					IsDirected);
			}

			_isIgnoreNeighborConditionInitialized = true;
		}

		private void InitFilter()
		{
			IList<IFeatureClassFilter> filter;
			IList<QueryFilterHelper> helper;

			CopyFilters(out filter, out helper);
			_filter = filter[1];
			_helper = helper[1];

			_filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
		}
	}
}
