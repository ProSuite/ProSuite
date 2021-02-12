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
	public class QaMustTouchOther : QaRequiredSpatialRelationOther<PendingFeature>
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new MustTouchIssueCodes());

		#endregion

		[Doc(nameof(DocStrings.QaMustTouchOther_0))]
		public QaMustTouchOther(
			[Doc(nameof(DocStrings.QaMustTouchOther_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMustTouchOther_otherFeatureClass))] [NotNull]
			IFeatureClass
				otherFeatureClass,
			[Doc(nameof(DocStrings.QaMustTouchOther_relevantRelationCondition))] [CanBeNull]
			string
				relevantRelationCondition)
			: base(featureClass, otherFeatureClass, relevantRelationCondition) { }

		[Doc(nameof(DocStrings.QaMustTouchOther_1))]
		public QaMustTouchOther(
			[Doc(nameof(DocStrings.QaMustTouchOther_featureClass))] [NotNull]
			ICollection<IFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaMustTouchOther_otherFeatureClass))] [NotNull]
			ICollection<IFeatureClass>
				otherFeatureClasses,
			[Doc(nameof(DocStrings.QaMustTouchOther_relevantRelationCondition))] [CanBeNull]
			string
				relevantRelationCondition)
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
				            ? Codes[MustTouchIssueCodes.NoTouchingFeature_WithFulfilledConstraint]
				            : Codes[MustTouchIssueCodes.NoTouchingFeature];

			affectedComponent = ((IFeatureClass) feature.Class).ShapeFieldName;

			return HasRelevantRelationCondition
				       ? "Feature is not touched by another feature that fulfills the constraint"
				       : "Feature is not touched by another feature";
		}

		protected override void ConfigureSpatialFilter(ISpatialFilter spatialFilter)
		{
			spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
		}
	}
}
