using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[ProximityTest]
	public class QaMustBeNearOther : QaRequiredSpatialRelationOther<PendingFeature>
	{
		[NotNull] private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

		private readonly double _maximumDistance;
		private readonly string _shapeFieldName;
		private readonly double _maximumXyTolerance;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoFeatureWithinDistance = "NoFeatureWithinDistance";

			public const string NoFeatureWithinDistance_WithFulfilledConstraint =
				"NoFeatureWithinDistance.WithFulfilledConstraint";

			public Code() : base("MustBeNear") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMustBeNearOther_0))]
		public QaMustBeNearOther(
			[NotNull] [Doc(nameof(DocStrings.QaMustBeNearOther_featureClass))]
			IFeatureClass featureClass,
			[NotNull] [Doc(nameof(DocStrings.QaMustBeNearOther_nearClasses))]
			ICollection<IFeatureClass>
				nearClasses,
			[Doc(nameof(DocStrings.QaMustBeNearOther_maximumDistance))]
			double maximumDistance,
			[Doc(nameof(DocStrings.QaMustBeNearOther_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: base(new[] {featureClass}, nearClasses, relevantRelationCondition)
		{
			_maximumDistance = maximumDistance;
			SearchDistance = maximumDistance;
			_shapeFieldName = featureClass.ShapeFieldName;

			_maximumXyTolerance = DatasetUtils.GetMaximumXyTolerance(nearClasses);
		}

		[CanBeNull]
		[TestParameter]
		[Doc(nameof(DocStrings.QaMustBeNearOther_ErrorDistanceFormat))]
		public string ErrorDistanceFormat { get; set; }

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
				            ? Codes[Code.NoFeatureWithinDistance_WithFulfilledConstraint]
				            : Codes[Code.NoFeatureWithinDistance];
			affectedComponent = _shapeFieldName;

			return string.Format(HasRelevantRelationCondition
				                     ? "No neighboring feature with fulfilled constraint found within {0}"
				                     : "No neighboring feature found within {0}",
			                     Format(_maximumDistance));
		}

		protected override void ConfigureSpatialFilter(ISpatialFilter spatialFilter)
		{
			spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
		}

		protected override IGeometry GetSearchGeometry(IFeature feature, int tableIndex,
		                                               out bool isExpanded)
		{
			IGeometry shape = feature.Shape;
			if (shape.IsEmpty)
			{
				isExpanded = false;
				return null;
			}

			shape.QueryEnvelope(_envelopeTemplate);

			double expansion = Math.Abs(_maximumDistance) < double.Epsilon
				                   ? _maximumXyTolerance
				                   : _maximumDistance;

			_envelopeTemplate.Expand(expansion, expansion, asRatio: false);

			isExpanded = true;
			return _envelopeTemplate;
		}

		protected override bool IsValidRelation(IGeometry shape, IFeature relatedFeature)
		{
			var proximity = (IProximityOperator) shape;

			double distance = proximity.ReturnDistance(relatedFeature.Shape);
			return distance <= _maximumDistance;
		}

		[NotNull]
		private string Format(double x)
		{
			string format = ErrorDistanceFormat;

			return string.Format(string.IsNullOrEmpty(format)
				                     ? "{0}"
				                     : format, x);
		}
	}
}
