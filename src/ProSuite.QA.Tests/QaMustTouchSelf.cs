using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaMustTouchSelf : ContainerTest
	{
		private readonly SimpleCrossTileFeatureState _crossTileFeatureState =
			new SimpleCrossTileFeatureState();

		private readonly string _relevantRelationConditionSql;
		private QueryFilterHelper[] _helper;
		private ISpatialFilter[] _queryFilter;
		private readonly int _totalClassesCount;

		private RelevantRelationCondition _relevantRelationCondition;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new MustTouchIssueCodes());

		#endregion

		#region constructors

		[Doc(nameof(DocStrings.QaMustTouchSelf_0))]
		public QaMustTouchSelf(
			[Doc(nameof(DocStrings.QaMustTouchSelf_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMustTouchSelf_relevantRelationCondition))] [CanBeNull]
			string
				relevantRelationCondition)
			: this(new[] {featureClass}, relevantRelationCondition)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
		}

		[Doc(nameof(DocStrings.QaMustTouchSelf_1))]
		public QaMustTouchSelf(
			[Doc(nameof(DocStrings.QaMustTouchSelf_featureClasses))] [NotNull]
			ICollection<IFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaMustTouchSelf_relevantRelationCondition))] [CanBeNull]
			string
				relevantRelationCondition)
			: base(featureClasses.Cast<ITable>())
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));

			_relevantRelationConditionSql = StringUtils.IsNotEmpty(relevantRelationCondition)
				                                ? relevantRelationCondition
				                                : null;
			_totalClassesCount = featureClasses.Count;
		}

		#endregion

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			if (_queryFilter == null)
			{
				InitFilter();
				Assert.NotNull(_queryFilter, "_queryFilter");
			}

			if (_relevantRelationCondition == null)
			{
				_relevantRelationCondition =
					new RelevantRelationCondition(_relevantRelationConditionSql,
					                              GetSqlCaseSensitivity());
			}

			var feature = (IFeature) row;

			if (_crossTileFeatureState.IsFeatureKnownOK(tableIndex, feature.OID))
			{
				return NoError;
			}

			IGeometry shape = feature.Shape;

			int startTableIndex = IgnoreUndirected
				                      ? tableIndex
				                      : 0;

			bool anyTouching = false;
			for (int relatedTableIndex = startTableIndex;
			     relatedTableIndex < _totalClassesCount;
			     relatedTableIndex++)
			{
				foreach (IFeature relatedFeature in GetRelatedFeatures(shape, relatedTableIndex))
				{
					if (! _relevantRelationCondition.IsFulfilled(row, tableIndex,
					                                             relatedFeature, relatedTableIndex))
					{
						continue;
					}

					// a relevant touching feature is found -> correct
					anyTouching = true;
					_crossTileFeatureState.FlagFeatureAsOK(tableIndex, feature);

					// the related feature is now also known to be correct
					_crossTileFeatureState.FlagFeatureAsOK(relatedTableIndex, relatedFeature);
				}
			}

			if (! anyTouching)
			{
				// could be an error; but maybe there's a touching feature in a later tile
				PendingFeature pendingFeature;
				_crossTileFeatureState.FlagFeatureAsSuspicious(tableIndex, feature,
				                                               out pendingFeature);
			}

			return NoError;
		}

		protected override int CompleteTileCore(TileInfo tileInfo)
		{
			IEnvelope tileEnvelope = tileInfo.CurrentEnvelope;

			return tileEnvelope == null
				       ? NoError
				       : _crossTileFeatureState.ReportErrors(ReportErrors, tileEnvelope,
				                                             tileInfo.AllBox);
		}

		[NotNull]
		private IEnumerable<IFeature> GetRelatedFeatures([NotNull] IGeometry shape,
		                                                 int relatedTableIndex)
		{
			ITable table = InvolvedTables[relatedTableIndex];

			ISpatialFilter spatialFilter = _queryFilter[relatedTableIndex];
			spatialFilter.Geometry = shape;

			QueryFilterHelper filterHelper = _helper[relatedTableIndex];

			return Search(table, spatialFilter, filterHelper).Cast<IFeature>();
		}

		private int ReportErrors(int tableIndex,
		                         [NotNull] ICollection<PendingFeature> errorFeatures)
		{
			if (errorFeatures.Count == 0)
			{
				return NoError;
			}

			var featureClass = (IFeatureClass) InvolvedTables[tableIndex];
			List<int> oids = errorFeatures.Select(feature => feature.OID).ToList();

			// TODO extract base class (QaRequiredSpatialRelationSelf)
			// TODO use issue code NoTouchingFeature_WithFulfilledConstraint
			const bool recycling = true;
			return GdbQueryUtils.GetFeatures(featureClass, oids, recycling)
			                    .Sum(feature => ReportError(
				                         "Feature is not touched by another feature",
				                         feature.ShapeCopy,
				                         Codes[MustTouchIssueCodes.NoTouchingFeature],
				                         TestUtils.GetShapeFieldName(feature),
				                         GetInvolvedRows(feature)));
		}

		/// <summary>
		/// create a filter that gets the lines crossing the current row,
		/// with the same attribute constraints as the table
		/// </summary>
		private void InitFilter()
		{
			IList<ISpatialFilter> filters;
			IList<QueryFilterHelper> filterHelpers;

			_queryFilter = new ISpatialFilter[_totalClassesCount];
			_helper = new QueryFilterHelper[_totalClassesCount];

			// Create copy of this filter and use it for quering features
			CopyFilters(out filters, out filterHelpers);
			for (int i = 0; i < _totalClassesCount; i++)
			{
				_queryFilter[i] = filters[i];
				_queryFilter[i].SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;

				_helper[i] = filterHelpers[i];
			}
		}

		#region Nested types

		private class RelevantRelationCondition : RowPairCondition
		{
			private const bool _isDirected = false;
			private const bool _undefinedConstraintIsFulfilled = true;
			private const string _row1Alias = "G1";
			private const string _row2Alias = "G2";

			public RelevantRelationCondition([CanBeNull] string constraint, bool caseSensitive)
				: base(constraint, _isDirected, _undefinedConstraintIsFulfilled,
				       _row1Alias, _row2Alias, caseSensitive) { }
		}

		#endregion
	}
}
