using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaPointNotNearDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PointClass { get; }
		public IList<IFeatureClassSchemaDef> ReferenceClasses { get; }
		public double SearchDistance { get; }
		public string PointDistanceExpression { get; }
		public IList<string> ReferenceDistanceExpressions { get; }
		public IList<string> ReferenceRightSideDistances { get; }
		public IList<string> ReferenceFlipExpressions { get; }

		private const double _defaultMinimumErrorLineLength = -1;

		[Doc(nameof(DocStrings.QaPointNotNear_0))]
		public QaPointNotNearDefinition(
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_pointClass))]
			IFeatureClassSchemaDef pointClass,
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceClass))]
			IFeatureClassSchemaDef referenceClass,
			[Doc(nameof(DocStrings.QaPointNotNear_limit))]
			double limit)
			: this(pointClass, new[] { referenceClass }, limit) { }

		[Doc(nameof(DocStrings.QaPointNotNear_1))]
		public QaPointNotNearDefinition(
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_pointClass))]
			IFeatureClassSchemaDef pointClass,
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceClasses))]
			IList<IFeatureClassSchemaDef> referenceClasses,
			[Doc(nameof(DocStrings.QaPointNotNear_limit))]
			double limit)
			: base(new[] { pointClass }.Union(referenceClasses))
		{
			Assert.ArgumentNotNull(pointClass, nameof(pointClass));
			Assert.ArgumentNotNull(referenceClasses, nameof(referenceClasses));

			PointClass = pointClass;
			ReferenceClasses = referenceClasses;
			SearchDistance = limit;
		}

		[Doc(nameof(DocStrings.QaPointNotNear_2))]
		public QaPointNotNearDefinition(
				[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_pointClass))]
				IFeatureClassSchemaDef pointClass,
				[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceClasses))]
				IList<IFeatureClassSchemaDef> referenceClasses,
				[Doc(nameof(DocStrings.QaPointNotNear_searchDistance))]
				double searchDistance,
				[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_pointDistanceExpression))]
				string pointDistanceExpression,
				[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceDistanceExpressions))]
				IList<string> referenceDistanceExpressions)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(pointClass, referenceClasses, searchDistance, pointDistanceExpression,
			       referenceDistanceExpressions,
			       referenceRightSideDistances: null,
			       referenceFlipExpressions: null) { }

		[Doc(nameof(DocStrings.QaPointNotNear_3))]
		public QaPointNotNearDefinition(
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_pointClass))]
			IFeatureClassSchemaDef pointClass,
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceClasses))]
			IList<IFeatureClassSchemaDef> referenceClasses,
			[Doc(nameof(DocStrings.QaPointNotNear_searchDistance))]
			double searchDistance,
			[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_pointDistanceExpression))]
			string pointDistanceExpression,
			[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceDistanceExpressions))]
			IList<string> referenceDistanceExpressions,
			[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceRightSideDistances))]
			IList<string> referenceRightSideDistances,
			[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceFlipExpressions))]
			IList<string> referenceFlipExpressions)
			: base(new[] { pointClass }.Union(referenceClasses))
		{
			Assert.ArgumentNotNull(pointClass, nameof(pointClass));
			Assert.ArgumentNotNull(referenceClasses, nameof(referenceClasses));
			Assert.ArgumentCondition(referenceDistanceExpressions == null ||
			                         referenceDistanceExpressions.Count == 0 ||
			                         referenceDistanceExpressions.Count == 1 ||
			                         referenceDistanceExpressions.Count ==
			                         referenceClasses.Count,
			                         "unexpected number of reference distance expression " +
			                         "(must be 0, 1, or # of references tables)");
			Assert.ArgumentCondition(referenceRightSideDistances == null ||
			                         referenceRightSideDistances.Count == 0 ||
			                         referenceRightSideDistances.Count == 1 ||
			                         referenceRightSideDistances.Count ==
			                         referenceClasses.Count,
			                         "unexpected number of reference right side distances " +
			                         "(must be 0, 1, or equal to the number of reference classes");
			Assert.ArgumentCondition(referenceFlipExpressions == null ||
			                         referenceFlipExpressions.Count == 0 ||
			                         referenceFlipExpressions.Count == 1 ||
			                         referenceFlipExpressions.Count ==
			                         referenceClasses.Count,
			                         "unexpected number of reference flip expressions " +
			                         "(must be 0, 1, or equal to the number of reference classes");

			PointClass = pointClass;
			ReferenceClasses = referenceClasses;
			SearchDistance = searchDistance;
			PointDistanceExpression = pointDistanceExpression;
			ReferenceDistanceExpressions = referenceDistanceExpressions;
			ReferenceRightSideDistances = referenceRightSideDistances;
			ReferenceFlipExpressions = referenceFlipExpressions;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaPointNotNear_AllowCoincidentPoints))]
		public bool AllowCoincidentPoints { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaPointNotNear_GeometryComponents))]
		public IList<GeometryComponent> GeometryComponents { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaPointNotNear_ValidRelationConstraints))]
		public IList<string> ValidRelationConstraints { get; set; }

		[TestParameter(_defaultMinimumErrorLineLength)]
		[Doc(nameof(DocStrings.QaPointNotNear_MinimumErrorLineLength))]
		public double MinimumErrorLineLength { get; set; }
	}
}
