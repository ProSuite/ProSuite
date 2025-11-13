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
	public class QaEdgeMatchCrossingLinesDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> LineClasses1 { get; }
		public IFeatureClassSchemaDef BorderClass1 { get; }
		public IList<IFeatureClassSchemaDef> LineClasses2 { get; }
		public IFeatureClassSchemaDef BorderClass2 { get; }
		public double SearchDistance { get; }

		private const double _defaultCoincidenceTolerance = 0;
		// 0 --> exact match required

		private const bool _defaultIsCrossingLineAttributeConstraintSymmetric = false;
		private const bool _defaultAllowNoFeatureWithinSearchDistance = false;

		private const bool _defaultIgnoreAttributesConstraintsIfThreeOrMoreConnected =
			false;

		private const bool
			_defaultAllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide
				= true;

		private const bool
			_defaultIgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance =
				true;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident = false;

		private const bool
			_defaultAllowEndPointsConnectingToInteriorOfValidNeighborLine =
				false;

		private const bool _defaultIgnoreEndPointsOfBorderingLines = true;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled =
				false;

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_0))]
		public QaEdgeMatchCrossingLinesDefinition(
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_lineClass1))] [NotNull]
			IFeatureClassSchemaDef
				lineClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_borderClass1))] [NotNull]
			IFeatureClassSchemaDef
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_lineClass2))] [NotNull]
			IFeatureClassSchemaDef
				lineClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_borderClass2))] [NotNull]
			IFeatureClassSchemaDef
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_searchDistance))]
			double
				searchDistance)
			: this(new[] { lineClass1 }, borderClass1,
			       new[] { lineClass2 }, borderClass2,
			       searchDistance) { }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_1))]
		public QaEdgeMatchCrossingLinesDefinition(
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_lineClasses1))] [NotNull]
			IList<IFeatureClassSchemaDef>
				lineClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_borderClass1))] [NotNull]
			IFeatureClassSchemaDef
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_lineClasses2))] [NotNull]
			IList<IFeatureClassSchemaDef>
				lineClasses2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_borderClass2))] [NotNull]
			IFeatureClassSchemaDef
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_searchDistance))]
			double
				searchDistance)
			: base(CastToTables(lineClasses1, new[] { borderClass1 },
			                    lineClasses2, new[] { borderClass2 }))
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

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_MinimumErrorConnectionLineLength))]
		[TestParameter(0)]
		public double MinimumErrorConnectionLineLength { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_MaximumEndPointConnectionDistance))]
		[TestParameter(0)]
		public double MaximumEndPointConnectionDistance { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_LineClass1BorderMatchCondition))]
		[TestParameter]
		public string LineClass1BorderMatchCondition { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_LineClass2BorderMatchCondition))]
		[TestParameter]
		public string LineClass2BorderMatchCondition { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CrossingLineMatchCondition))]
		[TestParameter]
		public string CrossingLineMatchCondition { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CrossingLineAttributeConstraint))]
		[TestParameter]
		public string CrossingLineAttributeConstraint { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_IsCrossingLineAttributeConstraintSymmetric))]
		[TestParameter(_defaultIsCrossingLineAttributeConstraintSymmetric)]
		public bool IsCrossingLineAttributeConstraintSymmetric { get; set; }

		// NOTE blank is not supported as field separator (as it may be used as multi-value separator)
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CrossingLineEqualAttributes))]
		[TestParameter]
		public string CrossingLineEqualAttributes { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CrossingLineEqualAttributeOptions))]
		[TestParameter]
		public IList<string> CrossingLineEqualAttributeOptions { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_ReportIndividualAttributeConstraintViolations))]
		public bool ReportIndividualAttributeConstraintViolations { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CoincidenceTolerance))]
		[TestParameter(_defaultCoincidenceTolerance)]
		public double CoincidenceTolerance { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_AllowNoFeatureWithinSearchDistance))]
		[TestParameter(_defaultAllowNoFeatureWithinSearchDistance)]
		public bool AllowNoFeatureWithinSearchDistance { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_IgnoreAttributeConstraintsIfThreeOrMoreConnected
		))]
		[TestParameter(_defaultIgnoreAttributesConstraintsIfThreeOrMoreConnected)]
		public bool IgnoreAttributeConstraintsIfThreeOrMoreConnected { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide
		))]
		[TestParameter(_defaultAllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide
		)]
		public bool AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_AllowDisjointCandidateFeatureIfBordersAreNotCoincident
		))]
		[TestParameter(_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident)
		]
		public bool AllowDisjointCandidateFeatureIfBordersAreNotCoincident { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance
		))]
		[TestParameter(
			_defaultIgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance)
		]
		public bool IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_AllowEndPointsConnectingToInteriorOfValidNeighborLine
		))]
		[TestParameter(_defaultAllowEndPointsConnectingToInteriorOfValidNeighborLine)]
		public bool AllowEndPointsConnectingToInteriorOfValidNeighborLine { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_IgnoreEndPointsOfBorderingLines))]
		[TestParameter(_defaultIgnoreEndPointsOfBorderingLines)]
		public bool IgnoreEndPointsOfBorderingLines { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
		))]
		[TestParameter(
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)]
		public bool AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled { get; set; }
	}
}
