using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaMustIntersectOther : QaRequiredSpatialRelationOther<PendingFeature>
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes =>
			_codes ?? (_codes = new MustIntersectIssueCodes());

		#endregion

		[Doc(nameof(DocStrings.QaMustIntersectOther_0))]
		public QaMustIntersectOther(
			[Doc(nameof(DocStrings.QaMustIntersectOther_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMustIntersectOther_otherFeatureClass))] [NotNull]
			IFeatureClass otherFeatureClass,
			[Doc(nameof(DocStrings.QaMustIntersectOther_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: base(featureClass, otherFeatureClass, relevantRelationCondition) { }

		[Doc(nameof(DocStrings.QaMustIntersectOther_1))]
		public QaMustIntersectOther(
			[Doc(nameof(DocStrings.QaMustIntersectOther_featureClasses))] [NotNull]
			ICollection<IFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectOther_otherFeatureClasses))] [NotNull]
			ICollection<IFeatureClass> otherFeatureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectOther_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: base(featureClasses, otherFeatureClasses, relevantRelationCondition) { }

		protected override CrossTileFeatureState<PendingFeature>
			CreateCrossTileFeatureState()
		{
			return new SimpleCrossTileFeatureState();
		}

		protected override string GetErrorDescription(IFeature feature,
		                                              int tableIndex,
		                                              PendingFeature pendingFeature,
		                                              out IssueCode issueCode,
		                                              out string affectedComponent)
		{
			issueCode = HasRelevantRelationCondition
				            ? Codes[
					            MustIntersectIssueCodes
						            .NoIntersectingFeature_WithFulfilledConstraint]
				            : Codes[MustIntersectIssueCodes.NoIntersectingFeature];

			affectedComponent = ((IFeatureClass) feature.Class).ShapeFieldName;

			return HasRelevantRelationCondition
				       ? "Feature does not intersect another feature that fulfills the constraint"
				       : "Feature does not intersect another feature";
		}

		protected override void ConfigureSpatialFilter(ISpatialFilter spatialFilter)
		{
			spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
		}
	}
}
