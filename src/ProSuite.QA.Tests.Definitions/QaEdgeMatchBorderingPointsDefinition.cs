using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests
{

	[UsedImplicitly]
	[EdgeMatchTest]
	public class QaEdgeMatchBorderingPointsDefinition : AlgorithmDefinition
	{

		private const double _defaultCoincidenceTolerance = 0; // 0 --> exact match required

		private const bool _defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident =
			false;

		private const bool _defaultAllowNoFeatureWithinSearchDistance = false;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = false;

		private const bool _defaultIsBorderingPointAttributeConstraintSymmetric = false;

		//private static bool _defaultIsBorderingPointAttributeConstraintSymmetric;
		//public bool IsBorderingPointAttributeConstraintSymmetric { get; set; } = _defaultIsBorderingPointAttributeConstraintSymmetric;

		//private readonly bool _isBorderingPointAttributeConstraintSymmetric =
		//	_defaultIsBorderingPointAttributeConstraintSymmetric;
		public IList<IFeatureClassSchemaDef> PointClasses1 { get; }
		public IFeatureClassSchemaDef BorderClass1 { get; }
		public IList<IFeatureClassSchemaDef> PointClasses2 { get; }
		public IFeatureClassSchemaDef BorderClass2 { get; }
		public double SearchDistance { get; }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_0))]
		public QaEdgeMatchBorderingPointsDefinition(
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_pointClass1))] [NotNull]
			IFeatureClassSchemaDef pointClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_borderClass1))] [NotNull]
			IFeatureClassSchemaDef
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_pointClass2))] [NotNull]
			IFeatureClassSchemaDef pointClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_borderClass2))] [NotNull]
			IFeatureClassSchemaDef
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_searchDistance))]
			double searchDistance)
			: this(new[] { pointClass1 }, borderClass1,
			       new[] { pointClass2 }, borderClass2,
			       searchDistance) { }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_1))]
		public QaEdgeMatchBorderingPointsDefinition(
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_pointClasses1))] [NotNull]
			IList<IFeatureClassSchemaDef>
				pointClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_borderClass1))] [NotNull]
			IFeatureClassSchemaDef
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_pointClasses2))] [NotNull]
			IList<IFeatureClassSchemaDef>
				pointClasses2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_borderClass2))] [NotNull]
			IFeatureClassSchemaDef
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_searchDistance))]
			double searchDistance)
			: base(CastToTables(pointClasses1, new[] { borderClass1 },
			                    pointClasses2, new[] { borderClass2 }))
		{
			Assert.ArgumentNotNull(pointClasses1, nameof(pointClasses1));
			Assert.ArgumentNotNull(borderClass1, nameof(borderClass1));
			Assert.ArgumentNotNull(pointClasses2, nameof(pointClasses2));
			Assert.ArgumentNotNull(borderClass2, nameof(borderClass2));
			PointClasses1 = pointClasses1;
			BorderClass1 = borderClass1;
			PointClasses2 = pointClasses2;
			BorderClass2 = borderClass2;
			SearchDistance = searchDistance;

		}

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_PointClass1BorderMatchCondition))]
		[TestParameter]
		public string PointClass1BorderMatchCondition { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_PointClass2BorderMatchCondition))]
		[TestParameter]
		public string PointClass2BorderMatchCondition { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_BorderingPointMatchCondition))]
		[TestParameter]
		public string BorderingPointMatchCondition { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_BorderingPointAttributeConstraint))]
		[TestParameter]
		public string BorderingPointAttributeConstraint { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingPoints_IsBorderingPointAttributeConstraintSymmetric))]
		[TestParameter(_defaultIsBorderingPointAttributeConstraintSymmetric)]
		public bool IsBorderingPointAttributeConstraintSymmetric { get; set; }

		// NOTE blank is not supported as field separator (as it may be used as multi-value separator)
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_BorderPointEqualAttributes))]
		[TestParameter]
		public string BorderingPointEqualAttributes { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_BorderingPointEqualAttributeOptions))]
		[TestParameter]
		public IList<string> BorderingPointEqualAttributeOptions { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingPoints_ReportIndividualAttributeConstraintViolations))]
		public bool ReportIndividualAttributeConstraintViolations { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_CoincidenceTolerance))]
		[TestParameter(_defaultCoincidenceTolerance)]
		public double CoincidenceTolerance { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingPoints_AllowDisjointCandidateFeatureIfBordersAreNotCoincident
		))]
		[TestParameter(_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident)]
		public bool AllowDisjointCandidateFeatureIfBordersAreNotCoincident { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_AllowNoFeatureWithinSearchDistance))]
		[TestParameter(_defaultAllowNoFeatureWithinSearchDistance)]
		public bool AllowNoFeatureWithinSearchDistance { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingPoints_AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
		))]
		[TestParameter(
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)]
		public bool AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled { get; set; }
	}
}
