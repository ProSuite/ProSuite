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
	public class QaEdgeMatchCrossingAreasDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> AreaClasses1 { get; }
		public IFeatureClassSchemaDef BorderClass1 { get; }
		public IList<IFeatureClassSchemaDef> AreaClasses2 { get; }
		public IFeatureClassSchemaDef BorderClass2 { get; }
		public double SearchDistance { get; }
		public IList<IFeatureClassSchemaDef> BoundingClasses1 { get; }
		public IList<IFeatureClassSchemaDef> BoundingClasses2 { get; }

		private const bool _defaultIsCrossingAreaAttributeConstraintSymmetric = false;
		private const bool _defaultAllowNoFeatureWithinSearchDistance = false;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident = false;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = false;

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_0))]
		public QaEdgeMatchCrossingAreasDefinition(
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_areaClass1))] [NotNull]
			IFeatureClassSchemaDef areaClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_borderClass1))] [NotNull]
			IFeatureClassSchemaDef
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_areaClass2))] [NotNull]
			IFeatureClassSchemaDef areaClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_borderClass2))] [NotNull]
			IFeatureClassSchemaDef
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_searchDistance))]
			double
				searchDistance,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_boundingClasses1))] [CanBeNull]
			IList<IFeatureClassSchemaDef>
				boundingClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_boundingClasses2))] [CanBeNull]
			IList<IFeatureClassSchemaDef>
				boundingClasses2)
			: this(new[] { areaClass1 }, borderClass1,
			       new[] { areaClass2 }, borderClass2,
			       searchDistance, boundingClasses1, boundingClasses2) { }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_1))]
		public QaEdgeMatchCrossingAreasDefinition(
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_areaClasses1))] [NotNull]
			IList<IFeatureClassSchemaDef>
				areaClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_borderClass1))] [NotNull]
			IFeatureClassSchemaDef
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_areaClasses2))] [NotNull]
			IList<IFeatureClassSchemaDef>
				areaClasses2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_borderClass2))] [NotNull]
			IFeatureClassSchemaDef
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_searchDistance))]
			double
				searchDistance,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_boundingClasses1))] [CanBeNull]
			IList<IFeatureClassSchemaDef>
				boundingClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_boundingClasses2))] [CanBeNull]
			IList<IFeatureClassSchemaDef>
				boundingClasses2)
			: base(CastToTables(areaClasses1, new[] { borderClass1 },
			                    areaClasses2, new[] { borderClass2 },
			                    boundingClasses1 ?? new IFeatureClassSchemaDef[] { },
			                    boundingClasses2 ?? new IFeatureClassSchemaDef[] { }))
		{
			Assert.ArgumentNotNull(areaClasses1, nameof(areaClasses1));
			Assert.ArgumentNotNull(borderClass1, nameof(borderClass1));
			Assert.ArgumentNotNull(areaClasses2, nameof(areaClasses2));
			Assert.ArgumentNotNull(borderClass2, nameof(borderClass2));
			AreaClasses1 = areaClasses1;
			BorderClass1 = borderClass1;
			AreaClasses2 = areaClasses2;
			BorderClass2 = borderClass2;
			SearchDistance = searchDistance;
			BoundingClasses1 = boundingClasses1;
			BoundingClasses2 = boundingClasses2;

			// defaults
			AllowNoFeatureWithinSearchDistance = _defaultAllowNoFeatureWithinSearchDistance;
			AllowDisjointCandidateFeatureIfBordersAreNotCoincident =
				_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident;

			AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled =
				_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AreaClass1BorderMatchCondition))]
		public string AreaClass1BorderMatchCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AreaClass1BoundingFeatureMatchCondition))]
		public string AreaClass1BoundingFeatureMatchCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AreaClass2BoundingFeatureMatchCondition))]
		public string AreaClass2BoundingFeatureMatchCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AreaClass2BorderMatchCondition))]
		public string AreaClass2BorderMatchCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_CrossingAreaMatchCondition))]
		public string CrossingAreaMatchCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_CrossingAreaAttributeConstraint))]
		public string CrossingAreaAttributeConstraint { get; set; }

		[TestParameter(_defaultIsCrossingAreaAttributeConstraintSymmetric)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingAreas_IsCrossingAreaAttributeConstraintSymmetric))]
		public bool IsCrossingAreaAttributeConstraintSymmetric { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_CrossingAreaEqualAttributes))]
		public string CrossingAreaEqualAttributes { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_CrossingAreaEqualAttributeOptions))]
		public IList<string> CrossingAreaEqualAttributeOptions { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingAreas_ReportIndividualAttributeConstraintViolations))]
		public bool ReportIndividualAttributeConstraintViolations { get; set; }

		[TestParameter(_defaultAllowNoFeatureWithinSearchDistance)]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AllowNoFeatureWithinSearchDistance))]
		public bool AllowNoFeatureWithinSearchDistance { get; set; }

		[TestParameter(_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingAreas_AllowDisjointCandidateFeatureIfBordersAreNotCoincident
		))]
		public bool AllowDisjointCandidateFeatureIfBordersAreNotCoincident { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingAreas_AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
		))]
		[TestParameter(
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)]
		public bool AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled { get; set; }
	}
}
