using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaFullCoincidence : QaFullCoincidenceBase
	{
		// TODO should be dependent at least on sref
		private const double _defaultTileSize = 1000.0;

		private readonly IEnvelope _queryBox = new EnvelopeClass();
		private readonly IList<IReadOnlyFeatureClass> _referenceList;

		private IList<QueryFilterHelper> _helperList;
		private IList<IFeatureClassFilter> _spatialFilters;

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

		[Doc(nameof(DocStrings.QaFullCoincidence_0))]
		public QaFullCoincidence(
				[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaFullCoincidence_reference))]
				IReadOnlyFeatureClass reference,
				[Doc(nameof(DocStrings.QaFullCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaFullCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, is3D, _defaultTileSize) { }

		[Doc(nameof(DocStrings.QaFullCoincidence_0))]
		public QaFullCoincidence(
			[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaFullCoincidence_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaFullCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaFullCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaFullCoincidence_tileSize))]
			double tileSize)
			: base(
				new[] { featureClass, reference }, near,
				new ConstantFeatureDistanceProvider(near / 2), is3D)
		{
			Assert.ArgumentNotNull(reference, nameof(reference));

			_referenceList = new[] { reference };
		}

		[Doc(nameof(DocStrings.QaFullCoincidence_2))]
		public QaFullCoincidence(
				[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaFullCoincidence_references))]
				IList<IReadOnlyFeatureClass> references,
				[Doc(nameof(DocStrings.QaFullCoincidence_near))]
				double near,
				[Doc(nameof(DocStrings.QaFullCoincidence_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, references, near, is3D, _defaultTileSize) { }

		[Doc(nameof(DocStrings.QaFullCoincidence_2))]
		public QaFullCoincidence(
			[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaFullCoincidence_references))]
			IList<IReadOnlyFeatureClass> references,
			[Doc(nameof(DocStrings.QaFullCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaFullCoincidence_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaFullCoincidence_tileSize))]
			double tileSize)
			: base(
				Union(new[] { featureClass }, references), near,
				new ConstantFeatureDistanceProvider(near / 2), is3D)
		{
			Assert.ArgumentNotNull(references, nameof(references));

			_referenceList = references;
		}

		[Doc(nameof(DocStrings.QaFullCoincidence_2))]
		public QaFullCoincidence(
				[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaFullCoincidence_references))]
				IList<IReadOnlyFeatureClass> references,
				[Doc(nameof(DocStrings.QaFullCoincidence_near))]
				double near)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, references, near, false, _defaultTileSize) { }

		[Doc(nameof(DocStrings.QaFullCoincidence_2))]
		public QaFullCoincidence(
			[Doc(nameof(DocStrings.QaFullCoincidence_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaFullCoincidence_references))]
			IList<IReadOnlyFeatureClass> references,
			[Doc(nameof(DocStrings.QaFullCoincidence_near))]
			double near,
			[Doc(nameof(DocStrings.QaFullCoincidence_tileSize))]
			double tileSize)
			: this(featureClass, references, near, false, tileSize) { }

		[InternallyUsedTest]
		public QaFullCoincidence(QaFullCoincidenceDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       definition.References.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.Near,
			       definition.TileSize
			)
		{
			IgnoreNeighborConditions = definition.IgnoreNeighborConditions;
		}

		protected override bool IsDirected => true;

		[TestParameter]
		[Doc(nameof(DocStrings.QaFullCoincidence_IgnoreNeighborConditions))]
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

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (tableIndex != 0)
			{
				return NoError;
			}

			if (_spatialFilters == null)
			{
				InitFilter();
			}

			IList<IFeatureClassFilter> filters = Assert.NotNull(_spatialFilters);

			// iterating over all needed tables
			int neighborTableIndex = -1;

			IGeometry geom0 = ((IReadOnlyFeature) row).Shape;
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

			foreach (IReadOnlyFeatureClass neighborFeatureClass in _referenceList)
			{
				neighborTableIndex++;

				IFeatureClassFilter spatialFilter = filters[neighborTableIndex];
				spatialFilter.FilterGeometry = _queryBox;

				var neighborTable = (IReadOnlyTable) neighborFeatureClass;

				foreach (IReadOnlyRow neighborRow in
				         Search(neighborTable, spatialFilter, _helperList[neighborTableIndex]))
				{
					if (IgnoreNeighbor(row, neighborRow, neighborTableIndex))
					{
						continue;
					}

					var neighborFeature = (IReadOnlyFeature) neighborRow;
					var processed1 = new SegmentNeighbors(new SegmentPartComparer());

					var finder = new FullNeighborhoodFinder(
						rowsDistance, (IReadOnlyFeature) row, tableIndex, neighborFeature,
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
			IReadOnlyFeature feature,
			int tableIndex,
			IReadOnlyFeature neighbor,
			int neighborTableIndex)
		{
			return new FullNeighborhoodFinder(rowsDistance, feature, tableIndex, neighbor,
			                                  neighborTableIndex);
		}

		private bool IgnoreNeighbor([NotNull] IReadOnlyRow row, [NotNull] IReadOnlyRow neighbor,
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
			IList<IFeatureClassFilter> filters;
			IList<QueryFilterHelper> helpers;
			CopyFilters(out filters, out helpers);

			// only filters for reference layers are needed
			int filterCount = filters.Count;
			_spatialFilters = new IFeatureClassFilter[filterCount - 1];
			_helperList = new QueryFilterHelper[filterCount - 1];

			for (var filterIndex = 1; filterIndex < filterCount; filterIndex++)
			{
				_spatialFilters[filterIndex - 1] = filters[filterIndex];
				_helperList[filterIndex - 1] = helpers[filterIndex];
			}

			foreach (var filter in _spatialFilters)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
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
					var feature = (IReadOnlyFeature) pair.Key.Row;

					if (IsFeatureToCheck(feature, args.State, currentBox))
					{
						errorCount += Check(feature, pair.Value, args.AllBox);
					}
				}
			}

			errorCount += base.CompleteTileCore(args);

			return errorCount;
		}

		private bool IsFeatureToCheck([NotNull] IReadOnlyFeature feature,
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

		private void GetEnvelopeMax([NotNull] IReadOnlyFeature feature,
		                            out double xMax,
		                            out double yMax)
		{
			feature.Shape.QueryEnvelope(_queryBox);

			_queryBox.QueryCoords(out double _, out double _, out xMax, out yMax);
		}

		private int Check([NotNull] IReadOnlyFeature feature,
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

		private int ReportErrors([NotNull] IReadOnlyFeature feature,
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

				string shapeFieldName = ((IReadOnlyFeatureClass) feature.Table).ShapeFieldName;

				errorCount +=
					ReportError(
						LocalizableStrings.QaFullCoincidence_PartNotNearReference,
						InvolvedRowUtils.GetInvolvedRows(feature),
						errorGeometry, Codes[Code.PartNotNearReference], shapeFieldName);
			}

			return errorCount;
		}
	}
}
