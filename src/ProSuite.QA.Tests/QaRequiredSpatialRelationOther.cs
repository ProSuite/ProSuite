using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests
{
	public abstract class QaRequiredSpatialRelationOther<T> : ContainerTest
		where T : PendingFeature
	{
		// only access via property:
		[CanBeNull] private CrossTileFeatureState<T> _crossTileFeatureState;

		private readonly string _relevantRelationConditionSql;
		private readonly int _firstOtherClassIndex;
		private readonly int _totalClassesCount;

		private RelevantRelationCondition _relevantRelationCondition;
		private QueryFilterHelper[] _helper;
		private ISpatialFilter[] _queryFilter;

		protected QaRequiredSpatialRelationOther(
			[NotNull] IReadOnlyFeatureClass featureClass,
			[NotNull] IReadOnlyFeatureClass otherFeatureClass,
			[CanBeNull] string relevantRelationCondition)
			: this(new[] {featureClass}, new[] {otherFeatureClass},
			       relevantRelationCondition)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(otherFeatureClass, nameof(otherFeatureClass));
		}

		protected QaRequiredSpatialRelationOther(
			[NotNull] ICollection<IReadOnlyFeatureClass> featureClasses,
			[NotNull] ICollection<IReadOnlyFeatureClass> otherFeatureClasses,
			[CanBeNull] string relevantRelationCondition)
			: base(Union(featureClasses.ToList(), otherFeatureClasses.ToList()))
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));
			Assert.ArgumentNotNull(otherFeatureClasses, nameof(otherFeatureClasses));

			_relevantRelationConditionSql =
				StringUtils.IsNotEmpty(relevantRelationCondition)
					? relevantRelationCondition
					: null;

			_firstOtherClassIndex = featureClasses.Count;
			_totalClassesCount = featureClasses.Count + otherFeatureClasses.Count;

			HasRelevantRelationCondition =
				StringUtils.IsNotEmpty(relevantRelationCondition);
		}

		public bool HasRelevantRelationCondition { get; }

		public sealed override bool IsQueriedTable(int tableIndex)
		{
			// only other classes are queried
			return tableIndex >= _firstOtherClassIndex;
		}

		protected sealed override int ExecuteCore(IReadOnlyRow row, int tableIndex)
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

			if (tableIndex >= _firstOtherClassIndex)
			{
				// it's a row from the other feature classes, ignore
				return NoError;
			}

			var feature = (IReadOnlyFeature) row;

			if (CrossTileFeatureState.IsFeatureKnownOK(tableIndex, feature.OID))
			{
				return NoError;
			}

			IGeometry shape = feature.Shape;

			IGeometry searchGeometry = GetSearchGeometry(feature, tableIndex, out bool _);

			var anyRelated = false;

			if (searchGeometry != null && ! searchGeometry.IsEmpty)
			{
				for (int relatedTableIndex = _firstOtherClassIndex;
				     relatedTableIndex < _totalClassesCount;
				     relatedTableIndex++)
				{
					foreach (IReadOnlyFeature relatedFeature in
					         GetRelatedFeatures(searchGeometry, relatedTableIndex))
					{
						if (relatedFeature == feature)
						{
							continue; // same feature instance
						}

						if (relatedFeature.OID == feature.OID &&
						    relatedFeature.Table == feature.Table)
						{
							continue; // same feature
						}

						if (! _relevantRelationCondition.IsFulfilled(row, tableIndex,
							    relatedFeature,
							    relatedTableIndex))
						{
							continue; // the pair does not fulfill the condition
						}

						// this check can be expensive, so do this after the condition check
						if (! IsValidRelation(shape, relatedFeature))
						{
							continue;
						}

						// a relevant related feature is found -> correct
						anyRelated = true;
						CrossTileFeatureState.FlagFeatureAsOK(tableIndex, feature);
					}
				}
			}

			if (! anyRelated)
			{
				// could be an error; but maybe there's a related feature in a later tile
				FlagFeatureAsSuspicious(tableIndex, feature);
			}

			return NoError;
		}

		protected sealed override int CompleteTileCore(TileInfo tileInfo)
		{
			IEnvelope tileEnvelope = tileInfo.CurrentEnvelope;

			IEnvelope testRunEnvelope = tileInfo.AllBox;

			return tileEnvelope == null
				       ? NoError
				       : CrossTileFeatureState.ReportErrors(ReportErrors,
				                                            tileEnvelope,
				                                            testRunEnvelope);
		}

		[NotNull]
		protected abstract CrossTileFeatureState<T> CreateCrossTileFeatureState();

		[NotNull]
		protected abstract string GetErrorDescription(
			[NotNull] IReadOnlyFeature feature,
			int tableIndex,
			[NotNull] T pendingFeature,
			[CanBeNull] out IssueCode issueCode,
			[CanBeNull] out string affectedComponent);

		protected abstract void ConfigureSpatialFilter(
			[NotNull] ISpatialFilter spatialFilter);

		[CanBeNull]
		protected virtual IGeometry GetSearchGeometry([NotNull] IReadOnlyFeature feature,
		                                              int tableIndex,
		                                              out bool isExpanded)
		{
			isExpanded = false;
			return feature.Shape;
		}

		protected virtual bool IsValidRelation([NotNull] IGeometry shape,
		                                       [NotNull] IReadOnlyFeature relatedFeature)
		{
			return true;
		}

		protected virtual void UpdateSuspiciousFeature([NotNull] T pendingFeature,
		                                               int tableIndex,
		                                               [NotNull] IReadOnlyFeature feature)
		{
			// do nothing
		}

		[NotNull]
		private CrossTileFeatureState<T> CrossTileFeatureState =>
			_crossTileFeatureState ??
			(_crossTileFeatureState = CreateCrossTileFeatureState());

		private void FlagFeatureAsSuspicious(int tableIndex, [NotNull] IReadOnlyFeature feature)
		{
			T pendingFeature;
			CrossTileFeatureState.FlagFeatureAsSuspicious(tableIndex, feature,
			                                              out pendingFeature);

			UpdateSuspiciousFeature(pendingFeature, tableIndex, feature);
		}

		[NotNull]
		private IEnumerable<IReadOnlyFeature> GetRelatedFeatures([NotNull] IGeometry shape,
		                                                         int relatedTableIndex)
		{
			IReadOnlyTable table = InvolvedTables[relatedTableIndex];

			ISpatialFilter spatialFilter = _queryFilter[relatedTableIndex];
			spatialFilter.Geometry = shape;

			QueryFilterHelper filterHelper = _helper[relatedTableIndex];

			return Search(table, spatialFilter, filterHelper)
				.Cast<IReadOnlyFeature>();
		}

		private int ReportErrors(int tableIndex,
		                         [NotNull] ICollection<T> errorFeatures)
		{
			if (errorFeatures.Count == 0)
			{
				return NoError;
			}

			var featureClass = (IReadOnlyFeatureClass) InvolvedTables[tableIndex];

			List<long> oids = errorFeatures.Select(feature => feature.OID).ToList();
			Dictionary<long, T> errorFeaturesByOid =
				errorFeatures.ToDictionary(errorFeature => errorFeature.OID);

			const bool recycling = true;
			return GdbQueryUtils
			       .GetRows(featureClass, oids, recycling)
			       .Sum(feature => ReportError((IReadOnlyFeature) feature, tableIndex,
			                                   errorFeaturesByOid[feature.OID]));
		}

		private int ReportError([NotNull] IReadOnlyFeature feature,
		                        int tableIndex,
		                        [NotNull] T pendingFeature)
		{
			IssueCode issueCode;
			string affectedComponent;
			string description = GetErrorDescription(feature, tableIndex,
			                                         pendingFeature,
			                                         out issueCode,
			                                         out affectedComponent);

			return ReportError(
				description, GetInvolvedRows(feature),
				feature.ShapeCopy, issueCode, affectedComponent);
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
			for (var i = 0; i < _totalClassesCount; i++)
			{
				_queryFilter[i] = filters[i];
				ConfigureSpatialFilter(_queryFilter[i]);

				_helper[i] = filterHelpers[i];
			}
		}

		#region Nested types

		private class RelevantRelationCondition : RowPairCondition
		{
			private const bool _isDirected = true;
			private const bool _undefinedConstraintIsFulfilled = true;
			private const string _row1Alias = "G1";
			private const string _row2Alias = "G2";

			public RelevantRelationCondition([CanBeNull] string constraint,
			                                 bool caseSensitive)
				: base(constraint, _isDirected, _undefinedConstraintIsFulfilled,
				       _row1Alias, _row2Alias, caseSensitive) { }
		}

		#endregion
	}
}
