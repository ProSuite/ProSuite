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
	public class QaPartCoincidenceOther : QaNearCoincidenceBase
	{
		private readonly IFeatureClass _reference;
		private ISpatialFilter _filter;
		private QueryFilterHelper _helper;

		private IgnoreRowNeighborCondition _ignoreNeighborCondition;
		private bool _isIgnoreNeighborConditionInitialized;

		[Doc("QaPartCoincidenceOther_0")]
		public QaPartCoincidenceOther(
				[Doc("QaPartCoincidence_featureClass")]
				IFeatureClass featureClass,
				[Doc("QaPartCoincidence_reference")] IFeatureClass reference,
				[Doc("QaPartCoincidence_near")] double near,
				[Doc("QaPartCoincidence_minLength")] double minLength,
				[Doc("QaPartCoincidence_is3D")] bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D, 1000.0) { }

		[Doc("QaPartCoincidenceOther_0")]
		public QaPartCoincidenceOther(
			[Doc("QaPartCoincidence_featureClass")]
			IFeatureClass featureClass,
			[Doc("QaPartCoincidence_reference")] IFeatureClass reference,
			[Doc("QaPartCoincidence_near")] double near,
			[Doc("QaPartCoincidence_minLength")] double minLength,
			[Doc("QaPartCoincidence_is3D")] bool is3D,
			[Doc("QaPartCoincidence_tileSize")] double tileSize)
			: this(featureClass, reference, near, minLength, minLength, is3D, tileSize, 0)
		{
			_reference = reference;
		}

		[Doc("QaPartCoincidenceOther_0")]
		public QaPartCoincidenceOther(
				[Doc("QaPartCoincidence_featureClass")]
				IFeatureClass featureClass,
				[Doc("QaPartCoincidence_reference")] IFeatureClass reference,
				[Doc("QaPartCoincidence_near")] double near,
				[Doc("QaPartCoincidence_minLength")] double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, false, 1000.0) { }

		[Doc("QaPartCoincidenceOther_0")]
		public QaPartCoincidenceOther(
			[Doc("QaPartCoincidence_featureClass")]
			IFeatureClass featureClass,
			[Doc("QaPartCoincidence_reference")] IFeatureClass reference,
			[Doc("QaPartCoincidence_near")] double near,
			[Doc("QaPartCoincidence_minLength")] double minLength,
			[Doc("QaPartCoincidence_tileSize")] double tileSize)
			: this(featureClass, reference, near, minLength, false, tileSize) { }

		[Doc("QaPartCoincidenceOther_4")]
		public QaPartCoincidenceOther(
			[Doc("QaPartCoincidence_featureClass")]
			IFeatureClass featureClass,
			[Doc("QaPartCoincidence_reference")] IFeatureClass reference,
			[Doc("QaPartCoincidence_near")] double near,
			[Doc("QaPartCoincidence_connectedMinLength")]
			double connectedMinLength,
			[Doc("QaPartCoincidence_disjointMinLength")]
			double disjointMinLength,
			[Doc("QaPartCoincidence_is3D")] bool is3D,
			[Doc("QaPartCoincidence_tileSize")] double tileSize,
			[Doc("QaPartCoincidence_coincidenceTolerance")]
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

		[TestParameter]
		[Doc("QaPartCoincidenceOther_IgnoreNeighborCondition")]
		public string IgnoreNeighborCondition { get; set; }

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			if (tableIndex > 0)
			{
				return NoError;
			}

			if (_filter == null)
			{
				InitFilter();
			}

			ISpatialFilter filter = Assert.NotNull(_filter, "_filter");

			var processed0 = new SegmentNeighbors(new SegmentPartComparer());

			IGeometry geom0 = ((IFeature) row).Shape;
			IEnvelope box0 = geom0.Envelope;
			box0.Expand(SearchDistance, SearchDistance, false);

			filter.Geometry = box0;

			var errorCount = 0;

			double maxNear = SearchDistance;
			const int neighborTableIndex = 1;

			IFeatureRowsDistance rowsDistance =
				NearDistanceProvider.GetRowsDistance(row, tableIndex);
			foreach (IRow neighborRow in
				Search((ITable) _reference, filter, _helper, geom0))
			{
				var rowNeighbor = (IFeature) neighborRow;

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
					GetNeighborhoodFinder(rowsDistance, (IFeature) row, tableIndex,
					                      rowNeighbor, neighborTableIndex);
				errorCount += FindNeighborhood(finder, tableIndex, processed0,
				                               neighborTableIndex, processed1,
				                               maxNear);
			}

			return errorCount;
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
					IgnoreNeighborCondition, GetSqlCaseSensitivity(),
					IsDirected);
			}

			_isIgnoreNeighborConditionInitialized = true;
		}

		private void InitFilter()
		{
			IList<ISpatialFilter> filter;
			IList<QueryFilterHelper> helper;

			CopyFilters(out filter, out helper);
			_filter = filter[1];
			_helper = helper[1];

			_filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
		}
	}
}