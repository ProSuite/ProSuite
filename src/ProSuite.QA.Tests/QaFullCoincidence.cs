using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[ProximityTest]
	public class QaFullCoincidence : QaFullCoincidenceBase
	{
		// TODO should be dependent at least on sref
		private const double _defaultTileSize = 1000.0;

		private readonly IEnvelope _queryBox = new EnvelopeClass();
		private readonly IList<IFeatureClass> _referenceList;

		private IList<QueryFilterHelper> _helperList;
		private IList<ISpatialFilter> _spatialFilters;

		private IList<string> _ignoreNeighborConditionsSql;
		private List<IgnoreRowNeighborCondition> _ignoreNeighborConditions;
		private bool _isIgnoreNeighborConditionsInitialized;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string PartNotNearReference = "PartNotNearReference";

			public Code() : base("FullCoincidence") { }
		}

		#endregion

		[Doc("QaFullCoincidence_0")]
		public QaFullCoincidence(
				[Doc("QaFullCoincidence_featureClass")]
				IFeatureClass featureClass,
				[Doc("QaFullCoincidence_reference")] IFeatureClass reference,
				[Doc("QaFullCoincidence_near")] double near,
				[Doc("QaFullCoincidence_is3D")] bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, is3D, _defaultTileSize) { }

		[Doc("QaFullCoincidence_0")]
		public QaFullCoincidence(
			[Doc("QaFullCoincidence_featureClass")]
			IFeatureClass featureClass,
			[Doc("QaFullCoincidence_reference")] IFeatureClass reference,
			[Doc("QaFullCoincidence_near")] double near,
			[Doc("QaFullCoincidence_is3D")] bool is3D,
			[Doc("QaFullCoincidence_tileSize")] double tileSize)
			: base(
				new[] {featureClass, reference}, near,
				new ConstantFeatureDistanceProvider(near / 2), is3D)
		{
			Assert.ArgumentNotNull(reference, nameof(reference));

			_referenceList = new[] {reference};
		}

		[Doc("QaFullCoincidence_2")]
		public QaFullCoincidence(
				[Doc("QaFullCoincidence_featureClass")]
				IFeatureClass featureClass,
				[Doc("QaFullCoincidence_references")] IList<IFeatureClass> references,
				[Doc("QaFullCoincidence_near")] double near,
				[Doc("QaFullCoincidence_is3D")] bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, references, near, is3D, _defaultTileSize) { }

		[Doc("QaFullCoincidence_2")]
		public QaFullCoincidence(
			[Doc("QaFullCoincidence_featureClass")]
			IFeatureClass featureClass,
			[Doc("QaFullCoincidence_references")] IList<IFeatureClass> references,
			[Doc("QaFullCoincidence_near")] double near,
			[Doc("QaFullCoincidence_is3D")] bool is3D,
			[Doc("QaFullCoincidence_tileSize")] double tileSize)
			: base(
				Union(new[] {featureClass}, references), near,
				new ConstantFeatureDistanceProvider(near / 2), is3D)
		{
			Assert.ArgumentNotNull(references, nameof(references));

			_referenceList = references;
		}

		[Doc("QaFullCoincidence_2")]
		public QaFullCoincidence(
				[Doc("QaFullCoincidence_featureClass")]
				IFeatureClass featureClass,
				[Doc("QaFullCoincidence_references")] IList<IFeatureClass> references,
				[Doc("QaFullCoincidence_near")] double near)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, references, near, false, _defaultTileSize) { }

		[Doc("QaFullCoincidence_2")]
		public QaFullCoincidence(
			[Doc("QaFullCoincidence_featureClass")]
			IFeatureClass featureClass,
			[Doc("QaFullCoincidence_references")] IList<IFeatureClass> references,
			[Doc("QaFullCoincidence_near")] double near,
			[Doc("QaFullCoincidence_tileSize")] double tileSize)
			: this(featureClass, references, near, false, tileSize) { }

		protected override bool IsDirected => true;

		[TestParameter]
		[Doc("QaFullCoincidence_IgnoreNeighborConditions")]
		public IList<string> IgnoreNeighborConditions
		{
			get { return _ignoreNeighborConditionsSql; }
			set
			{
				Assert.ArgumentCondition(value == null ||
				                         value.Count == 0 ||
				                         value.Count == 1 ||
				                         value.Count == _referenceList.Count,
				                         "unexpected number of IgnoredNeighborConditionsSql conditions " +
				                         "(must be 0, 1, or # of references tables)");

				_ignoreNeighborConditionsSql = value;
				_ignoreNeighborConditions = null;
			}
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			if (tableIndex != 0)
			{
				return NoError;
			}

			if (_spatialFilters == null)
			{
				InitFilter();
			}

			IList<ISpatialFilter> filters = Assert.NotNull(_spatialFilters);

			// iterating over all needed tables
			int neighborTableIndex = -1;

			IGeometry geom0 = ((IFeature) row).Shape;
			geom0.QueryEnvelope(_queryBox);

			SegmentNeighbors processed0;
			var rowKey = new RowKey(row, tableIndex);
			if (! ProcessedList.TryGetValue(rowKey, out processed0))
			{
				// add to process List
				processed0 = new SegmentNeighbors(new SegmentPartComparer());
				ProcessedList.Add(rowKey, processed0);
			}

			_queryBox.Expand(SearchDistance, SearchDistance, false);
			double maxNear = SearchDistance;

			IFeatureRowsDistance rowsDistance =
				NearDistanceProvider.GetRowsDistance(row, tableIndex);

			foreach (IFeatureClass neighborFeatureClass in _referenceList)
			{
				neighborTableIndex++;

				ISpatialFilter spatialFilter = filters[neighborTableIndex];
				spatialFilter.Geometry = _queryBox;

				var neighborTable = (ITable) neighborFeatureClass;

				foreach (IRow neighborRow in
					Search(neighborTable, spatialFilter, _helperList[neighborTableIndex], geom0))
				{
					if (IgnoreNeighbor(row, neighborRow, neighborTableIndex))
					{
						continue;
					}

					var neighborFeature = (IFeature) neighborRow;
					var processed1 = new SegmentNeighbors(new SegmentPartComparer());

					var finder = new FullNeighborhoodFinder(
						rowsDistance, (IFeature) row, tableIndex, neighborFeature,
						neighborTableIndex);
					FindNeighborhood(finder, tableIndex, processed0,
					                 neighborTableIndex, processed1,
					                 maxNear);
				}
			}

			// Remark: here only the neighborhood properties are found
			// if these properties are correct is checked in OnProgressedChanged
			return NoError;
		}

		protected override NeighborhoodFinder GetNeighborhoodFinder(
			IFeatureRowsDistance rowsDistance,
			IFeature feature,
			int tableIndex,
			IFeature neighbor,
			int neighborTableIndex)
		{
			return new FullNeighborhoodFinder(rowsDistance, feature, tableIndex, neighbor,
			                                  neighborTableIndex);
		}

		private bool IgnoreNeighbor([NotNull] IRow row, [NotNull] IRow neighbor,
		                            int neighborTableIndex)
		{
			EnsureIgnoreNeighborInitialized();
			if (_ignoreNeighborConditions == null || _ignoreNeighborConditions.Count == 0)
			{
				return false;
			}

			IgnoreRowNeighborCondition condition =
				_ignoreNeighborConditions.Count == 1
					? _ignoreNeighborConditions[0]
					: _ignoreNeighborConditions[neighborTableIndex];

			return condition.IsFulfilled(row, 0, neighbor, neighborTableIndex + 1);
		}

		private void EnsureIgnoreNeighborInitialized()
		{
			if (_isIgnoreNeighborConditionsInitialized)
			{
				return;
			}

			if (_ignoreNeighborConditionsSql != null)
			{
				_ignoreNeighborConditions =
					new List<IgnoreRowNeighborCondition>(_ignoreNeighborConditionsSql.Count);

				bool caseSensitivity = GetSqlCaseSensitivity();
				foreach (string condition in _ignoreNeighborConditionsSql)
				{
					_ignoreNeighborConditions.Add(new IgnoreRowNeighborCondition(condition,
					                                                             caseSensitivity,
					                                                             IsDirected));
				}
			}

			_isIgnoreNeighborConditionsInitialized = true;
		}

		private void InitFilter()
		{
			IList<ISpatialFilter> filters;
			IList<QueryFilterHelper> helpers;
			CopyFilters(out filters, out helpers);

			// only filters for reference layers are needed
			int filterCount = filters.Count;
			_spatialFilters = new ISpatialFilter[filterCount - 1];
			_helperList = new QueryFilterHelper[filterCount - 1];

			for (var filterIndex = 1; filterIndex < filterCount; filterIndex++)
			{
				_spatialFilters[filterIndex - 1] = filters[filterIndex];
				_helperList[filterIndex - 1] = helpers[filterIndex];
			}

			foreach (ISpatialFilter filter in _spatialFilters)
			{
				filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			var errorCount = 0;

			if (args.CurrentEnvelope != null)
			{
				WKSEnvelope currentBox;
				args.CurrentEnvelope.QueryWKSCoords(out currentBox);

				foreach (KeyValuePair<RowKey, SegmentNeighbors> pair in ProcessedList)
				{
					var feature = (IFeature) pair.Key.Row;

					if (IsFeatureToCheck(feature, args.State, currentBox))
					{
						errorCount += Check(feature, pair.Value, args.AllBox);
					}
				}
			}

			errorCount += base.CompleteTileCore(args);

			return errorCount;
		}

		private bool IsFeatureToCheck([NotNull] IFeature feature,
		                              TileState state,
		                              WKSEnvelope currentBox)
		{
			if (state == TileState.Final)
			{
				return true;
			}

			double featureXMax;
			double featureYMax;
			GetEnvelopeMax(feature, out featureXMax, out featureYMax);

			return featureXMax < currentBox.XMax && featureYMax < currentBox.YMax;
		}

		private void GetEnvelopeMax([NotNull] IFeature feature,
		                            out double xMax,
		                            out double yMax)
		{
			feature.Shape.QueryEnvelope(_queryBox);

			double xMin;
			double yMin;
			_queryBox.QueryCoords(out xMin, out yMin, out xMax, out yMax);
		}

		private int Check([NotNull] IFeature feature,
		                  [NotNull] SortedDictionary<SegmentPart, SegmentParts> nearList,
		                  [CanBeNull] IEnvelope processEnvelope)
		{
			// using (IIndexedGeometry geom = GetIndexedGeometry((IFeature)row, false)) // TODO revise, causes IClone.IsEqual to return incorrect results afterwards (at least for Z-only differences)

			const bool releaseOnDispose = true;
			using (IIndexedSegments indexedGeometry =
				IndexedSegmentUtils.GetIndexedGeometry(feature, releaseOnDispose))
			{
				IList<Subcurve> missingSegments = GetMissingSegments(feature,
				                                                     indexedGeometry,
				                                                     nearList);

				return missingSegments != null
					       ? ReportErrors(feature, missingSegments, processEnvelope)
					       : 0;
			}
		}

		private int ReportErrors([NotNull] IFeature feature,
		                         [NotNull] IEnumerable<Subcurve> errorSegments,
		                         [CanBeNull] IEnvelope processEnvelope)
		{
			var envelopeRelOp = (IRelationalOperator) processEnvelope;
			var errorCount = 0;

			foreach (Subcurve errorSegment in errorSegments)
			{
				IPolyline errorGeometry = errorSegment.GetGeometry();

				if (envelopeRelOp != null && envelopeRelOp.Disjoint(errorGeometry))
				{
					continue;
				}

				if (envelopeRelOp != null && ! envelopeRelOp.Contains(errorGeometry))
				{
					((ITopologicalOperator) errorGeometry).Clip(processEnvelope);
				}

				string shapeFieldName = ((IFeatureClass) feature.Class).ShapeFieldName;

				errorCount +=
					ReportError(LocalizableStrings.QaFullCoincidence_PartNotNearReference,
					            errorGeometry,
					            Codes[Code.PartNotNearReference],
					            shapeFieldName, feature);
			}

			return errorCount;
		}
	}
}