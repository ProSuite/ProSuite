using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[EdgeMatchTest]
	public class QaEdgeMatchBorderingLinesDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> LineClasses1 { get; }
		public IFeatureClassSchemaDef BorderClass1 { get; }
		public IList<IFeatureClassSchemaDef> LineClasses2 { get; }
		public IFeatureClassSchemaDef BorderClass2 { get; }
		public double SearchDistance { get; }
		private const bool _defaultIsBorderingLineAttributeConstraintSymmetric = false;
		private const bool _defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident = false;
		private const bool _defaultAllowNoFeatureWithinSearchDistance = false;
		private const bool _defaultAllowNonCoincidentEndPointsOnBorder = false;

		private const bool _defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled =
			false;



		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_0))]
		public QaEdgeMatchBorderingLinesDefinition(
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_lineClass1))] [NotNull]
			IFeatureClassSchemaDef lineClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_borderClass1))] [NotNull]
			IFeatureClassSchemaDef
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_lineClass2))] [NotNull]
			IFeatureClassSchemaDef lineClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_borderClass2))] [NotNull]
			IFeatureClassSchemaDef
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_searchDistance))]
			double
				searchDistance)
			: this(new[] { lineClass1 }, borderClass1,
			       new[] { lineClass2 }, borderClass2, searchDistance) { }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_1))]
		public QaEdgeMatchBorderingLinesDefinition(
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_lineClasses1))] [NotNull]
			IList<IFeatureClassSchemaDef>
				lineClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_borderClass1))] [NotNull]
			IFeatureClassSchemaDef
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_lineClasses2))] [NotNull]
			IList<IFeatureClassSchemaDef>
				lineClasses2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_borderClass2))] [NotNull]
			IFeatureClassSchemaDef
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_searchDistance))]
			double
				searchDistance)
			: base(lineClasses1)

		{
			Assert.ArgumentNotNull(lineClasses1, nameof(lineClasses1));
			Assert.ArgumentNotNull(borderClass1, nameof(borderClass1));
			Assert.ArgumentNotNull(lineClasses2, nameof(lineClasses2));
			Assert.ArgumentNotNull(borderClass2, nameof(borderClass2));
			LineClasses1 = lineClasses1;
			BorderClass1 = borderClass1;
			LineClasses2 = lineClasses2;
			BorderClass2 = borderClass2;
			SearchDistance = searchDistance;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_LineClass1BorderMatchCondition))]
		public string LineClass1BorderMatchCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_LineClass2BorderMatchCondition))]
		public string LineClass2BorderMatchCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_BorderingLineMatchCondition))]
		public string BorderingLineMatchCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_BorderingLineAttributeConstraint))]
		public string BorderingLineAttributeConstraint { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_BorderingLineEqualAttributes))]
		public string BorderingLineEqualAttributes { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_BorderingLineEqualAttributeOptions))]
		public IList<string> BorderingLineEqualAttributeOptions { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingLines_ReportIndividualAttributeConstraintViolations))]
		public bool ReportIndividualAttributeConstraintViolations { get; set; }


		[TestParameter(_defaultIsBorderingLineAttributeConstraintSymmetric)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingLines_IsBorderingLineAttributeConstraintSymmetric))]
		public bool IsBorderingLineAttributeConstraintSymmetric { get; set; }

		[TestParameter(_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingLines_AllowDisjointCandidateFeatureIfBordersAreNotCoincident
		))]
		public bool AllowDisjointCandidateFeatureIfBordersAreNotCoincident { get; set; }

		[TestParameter(_defaultAllowNoFeatureWithinSearchDistance)]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_AllowNoFeatureWithinSearchDistance))]
		public bool AllowNoFeatureWithinSearchDistance { get; set; }

		[TestParameter(_defaultAllowNonCoincidentEndPointsOnBorder)]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingLines_AllowNonCoincidentEndPointsOnBorder))]
		public bool AllowNonCoincidentEndPointsOnBorder { get; set; }

		[TestParameter(
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchBorderingLines_AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
		))]
		public bool AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled { get; set; }
	}
}
