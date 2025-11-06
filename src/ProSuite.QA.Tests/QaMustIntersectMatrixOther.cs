using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaMustIntersectMatrixOther :
		QaRequiredSpatialRelationOther<PendingFeature>
	{
		private readonly ICollection<esriGeometryDimension> _requiredDimensions;
		private readonly ICollection<esriGeometryDimension> _unallowedDimensions;
		private readonly string _relationString;

		private readonly List<IntersectionMatrix> _intersectionMatrices;
		private const string _relationInteriorIntersects = "RELATE (G1, G2, 'T********')";

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoFeatureWithRequiredRelation = "NoFeatureWithRequiredRelation";

			public const string NoFeatureWithRequiredRelation_WithFulfilledConstraint =
				"NoFeatureWithRequiredRelation.WithFulfilledConstraint";

			public Code() : base("MustIntersect9IM") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_0))]
		public QaMustIntersectMatrixOther(
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_otherFeatureClass))] [NotNull]
			IReadOnlyFeatureClass otherFeatureClass,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_relevantRelationCondition))]
			[CanBeNull]
			string
				relevantRelationCondition)
			: this(featureClass, otherFeatureClass,
			       intersectionMatrix, relevantRelationCondition,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       null, null) { }

		[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_1))]
		public QaMustIntersectMatrixOther(
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_otherFeatureClass))] [NotNull]
			IReadOnlyFeatureClass otherFeatureClass,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_relevantRelationCondition))]
			[CanBeNull]
			string relevantRelationCondition,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_requiredIntersectionDimensions))]
			string requiredIntersectionDimensions,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_unallowedIntersectionDimensions))]
			string unallowedIntersectionDimensions)
			: this(new[] { featureClass }, new[] { otherFeatureClass },
			       intersectionMatrix, relevantRelationCondition,
			       requiredIntersectionDimensions, unallowedIntersectionDimensions)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(otherFeatureClass, nameof(otherFeatureClass));
		}

		[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_2))]
		public QaMustIntersectMatrixOther(
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_featureClasses))] [NotNull]
			ICollection<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_otherFeatureClasses))] [NotNull]
			ICollection<IReadOnlyFeatureClass> otherFeatureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_relevantRelationCondition))]
			[CanBeNull]
			string
				relevantRelationCondition)
			: this(
				featureClasses, otherFeatureClasses,
				intersectionMatrix, relevantRelationCondition,
				// ReSharper disable once IntroduceOptionalParameters.Global
				null, null) { }

		[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_3))]
		public QaMustIntersectMatrixOther(
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_featureClasses))] [NotNull]
			ICollection<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_otherFeatureClasses))] [NotNull]
			ICollection<IReadOnlyFeatureClass> otherFeatureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_relevantRelationCondition))]
			[CanBeNull]
			string relevantRelationCondition,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_requiredIntersectionDimensions))]
			[CanBeNull]
			string requiredIntersectionDimensions,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_unallowedIntersectionDimensions))]
			[CanBeNull]
			string unallowedIntersectionDimensions)
			: base(featureClasses, otherFeatureClasses, relevantRelationCondition)
		{
			Assert.ArgumentNotNullOrEmpty(intersectionMatrix, nameof(intersectionMatrix));

			_intersectionMatrices = new List<IntersectionMatrix>();
			foreach (string matrixString in TestUtils.GetTokens(intersectionMatrix))
			{
				_intersectionMatrices.Add(new IntersectionMatrix(matrixString));
			}

			_requiredDimensions =
				QaSpatialRelationUtils.ParseDimensions(requiredIntersectionDimensions);
			_unallowedDimensions =
				QaSpatialRelationUtils.ParseDimensions(unallowedIntersectionDimensions);

			_relationString = GetRelationString(_intersectionMatrices,
			                                    _requiredDimensions,
			                                    _unallowedDimensions);
		}

		[InternallyUsedTest]
		public QaMustIntersectMatrixOther(
			[NotNull] QaMustIntersectMatrixOtherDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.OtherFeatureClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.IntersectionMatrix,
			       definition.RelevantRelationCondition,
			       definition.RequiredIntersectionDimensions,
			       definition.UnallowedIntersectionDimensions) { }

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
				            ? Codes[Code.NoFeatureWithRequiredRelation_WithFulfilledConstraint]
				            : Codes[Code.NoFeatureWithRequiredRelation];
			affectedComponent = ((IReadOnlyFeatureClass) feature.Table).ShapeFieldName;

			if (_intersectionMatrices.Count == 1)
			{
				return string.Format(
					HasRelevantRelationCondition
						? "Feature does not have the required intersection relation ({0}) with any other feature that fulfills the constraint"
						: "Feature does not have the required intersection relation ({0}) with any other feature",
					_relationString);
			}

			return string.Format(
				HasRelevantRelationCondition
					? "Feature does not have any of the required intersection relations ({0}) with any other feature that fulfills the constraint"
					: "Feature does not have any of the required intersection relations ({0}) with any other feature",
				_relationString);
		}

		protected override bool IsValidRelation(IGeometry shape, IReadOnlyFeature relatedFeature)
		{
			if (_intersectionMatrices.Count == 1)
			{
				return HasExpectedIntersectionDimensions(shape, relatedFeature,
				                                         _intersectionMatrices[0]);
			}

			IGeometry relatedShape = relatedFeature.Shape;
			var shapeRelOp = (IRelationalOperator) shape;

			foreach (IntersectionMatrix matrix in _intersectionMatrices)
			{
				string relationDescription = GetSpatialRelDescription(matrix);

				if (EvaluateRelation(shapeRelOp, relatedShape, relationDescription) &&
				    HasExpectedIntersectionDimensions(shape, relatedFeature, matrix))
				{
					return true;
				}
			}

			return false;
		}

		private static bool EvaluateRelation([NotNull] IRelationalOperator shapeRelOp,
		                                     [NotNull] IGeometry relatedShape,
		                                     [NotNull] string relationDescription)
		{
			if (IsInteriorIntersectionRelation(relationDescription))
			{
				// more robust: (not disjoint) and (not touches)	
				// "disjoint" geometries should not make it here.

				// TODO consolidate optimization of touches with GeometryEngine
				return ! shapeRelOp.Touches(relatedShape);
			}

			return shapeRelOp.Relation(relatedShape, relationDescription);
		}

		private static bool IsInteriorIntersectionRelation(
			[NotNull] string relationDescription)
		{
			return string.Equals(relationDescription.Trim(),
			                     _relationInteriorIntersects,
			                     StringComparison.OrdinalIgnoreCase);
		}

		private bool HasExpectedIntersectionDimensions([NotNull] IGeometry shape,
		                                               [NotNull] IReadOnlyFeature relatedFeature,
		                                               [NotNull] IntersectionMatrix matrix)
		{
			if (_requiredDimensions == null && _unallowedDimensions == null)
			{
				return true;
			}

			List<esriGeometryDimension> dimensions =
				matrix.GetIntersections(shape, relatedFeature.Shape)
				      .Select(intersection => intersection.Dimension)
				      .Distinct()
				      .ToList();

			// if any of the intersection dimensions is unallowed, then the relation is not valid
			if (_unallowedDimensions != null &&
			    dimensions.Any(dimension => _unallowedDimensions.Contains(dimension)))
			{
				return false;
			}

			// if any of the intersection parts has one of the required dimensions, then the relation is valid
			return _requiredDimensions != null &&
			       dimensions.Any(dimension => _requiredDimensions.Contains(dimension));
		}

		protected override void ConfigureSpatialFilter(IFeatureClassFilter spatialFilter)
		{
			if (_intersectionMatrices.Count == 1)
			{
				spatialFilter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelRelation;
				spatialFilter.SpatialRelDescription =
					GetSpatialRelDescription(_intersectionMatrices[0]);
			}
			else
			{
				spatialFilter.SpatialRelDescription = string.Empty;
				spatialFilter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;
			}
		}

		[NotNull]
		private static string GetSpatialRelDescription([NotNull] IntersectionMatrix matrix)
		{
			return string.Format("RELATE (G1, G2, '{0}')", matrix.MatrixString);
		}

		[NotNull]
		private static string GetRelationString(
			[NotNull] IEnumerable<IntersectionMatrix> intersectionMatrices,
			[CanBeNull] IEnumerable<esriGeometryDimension> requiredDimensions,
			[CanBeNull] IEnumerable<esriGeometryDimension> unallowedDimensions)
		{
			string matricesString = StringUtils.Concatenate(intersectionMatrices,
			                                                m => m.MatrixString, ",");

			var sb = new StringBuilder();
			sb.Append(matricesString);

			if (requiredDimensions != null)
			{
				sb.AppendFormat("; required dimensions: {0}",
				                StringUtils.Concatenate(requiredDimensions,
				                                        QaSpatialRelationUtils.GetDimensionText,
				                                        ","));
			}

			if (unallowedDimensions != null)
			{
				sb.AppendFormat("; unallowed dimensions: {0}",
				                StringUtils.Concatenate(unallowedDimensions,
				                                        QaSpatialRelationUtils.GetDimensionText,
				                                        ","));
			}

			return sb.ToString();
		}
	}
}
