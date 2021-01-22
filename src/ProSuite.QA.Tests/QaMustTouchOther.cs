using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
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

		[Doc("QaMustTouchOther_0")]
		public QaMustTouchOther(
			[Doc("QaMustTouchOther_featureClass")] [NotNull]
			IFeatureClass featureClass,
			[Doc("QaMustTouchOther_otherFeatureClass")] [NotNull]
			IFeatureClass
				otherFeatureClass,
			[Doc("QaMustTouchOther_relevantRelationCondition")] [CanBeNull]
			string
				relevantRelationCondition)
			: base(featureClass, otherFeatureClass, relevantRelationCondition) { }

		[Doc("QaMustTouchOther_1")]
		public QaMustTouchOther(
			[Doc("QaMustTouchOther_featureClass")] [NotNull]
			ICollection<IFeatureClass>
				featureClasses,
			[Doc("QaMustTouchOther_otherFeatureClass")] [NotNull]
			ICollection<IFeatureClass>
				otherFeatureClasses,
			[Doc("QaMustTouchOther_relevantRelationCondition")] [CanBeNull]
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
