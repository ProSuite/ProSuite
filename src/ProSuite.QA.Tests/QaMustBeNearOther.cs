using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
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
			IReadOnlyFeatureClass featureClass,
			[NotNull] [Doc(nameof(DocStrings.QaMustBeNearOther_nearClasses))]
			ICollection<IReadOnlyFeatureClass> nearClasses,
			[Doc(nameof(DocStrings.QaMustBeNearOther_maximumDistance))]
			double maximumDistance,
			[Doc(nameof(DocStrings.QaMustBeNearOther_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: base(new[] {featureClass}, nearClasses, relevantRelationCondition)
		{
			_maximumDistance = maximumDistance;
			SearchDistance = maximumDistance;
			_shapeFieldName = featureClass.ShapeFieldName;

			_maximumXyTolerance =
				nearClasses.Max(
					c => DatasetUtils.TryGetXyTolerance(c.SpatialReference, out double xy)
						     ? xy
						     : 0);
		}

		[InternallyUsedTest]
		public QaMustBeNearOther(
			[NotNull] QaMustBeNearOtherDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClass,
			       definition.NearClasses.Cast<IReadOnlyFeatureClass>().ToList(),
				   definition.MaximumDistance,
				   definition.RelevantRelationCondition)
		{
			ErrorDistanceFormat = definition.ErrorDistanceFormat;
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

		protected override string GetErrorDescription(IReadOnlyFeature feature,
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

		protected override void ConfigureSpatialFilter(IFeatureClassFilter spatialFilter)
		{
			spatialFilter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;
		}

		protected override IGeometry GetSearchGeometry(IReadOnlyFeature feature, int tableIndex,
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

		protected override bool IsValidRelation(IGeometry shape, IReadOnlyFeature relatedFeature)
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
