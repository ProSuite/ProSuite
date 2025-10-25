using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaLineGroupConstraintsDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> NetworkFeatureClasses { get; }
		public double MinGap { get;}
		public double MinGroupLength { get;}
		public double MinDangleLength { get;}
		public IList<string> GroupBy { get; }

		[Doc(nameof(DocStrings.QaLineGroupConstraints_0))]
		public QaLineGroupConstraintsDefinition(
			[Doc(nameof(DocStrings.QaLineGroupConstraints_networkFeatureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> networkFeatureClasses,
			[Doc(nameof(DocStrings.QaLineGroupConstraints_minGap))]
			double minGap,
			[Doc(nameof(DocStrings.QaLineGroupConstraints_minGroupLength))]
			double minGroupLength,
			[Doc(nameof(DocStrings.QaLineGroupConstraints_minDangleLength))]
			double minDangleLength,
			[Doc(nameof(DocStrings.QaLineGroupConstraints_groupBy))] [NotNull]
			IList<string> groupBy)
			: base(networkFeatureClasses)

		{
			Assert.ArgumentCondition(minGap >= 0, "Invalid minGap value: {0}", minGap);
			Assert.ArgumentCondition(minGroupLength >= 0, "Invalid minGroupLength value: {0}",
			                         minGroupLength);
			Assert.ArgumentCondition(minDangleLength >= 0,
			                         "Invalid minDangleLength value: {0}",
			                         minDangleLength);
			Assert.ArgumentNotNull(groupBy, nameof(groupBy));

			NetworkFeatureClasses = networkFeatureClasses;
			MinGap = minGap;
			MinGroupLength = minGroupLength;
			MinDangleLength = minDangleLength;
			GroupBy = groupBy;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_ValueSeparator))]
		public string ValueSeparator { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_GroupConditions))]
		public IList<string> GroupConditions { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToOtherGroupType))]
		public double MinGapToOtherGroupType { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinDangleLengthContinued))]
		public double MinDangleLengthContinued { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinDangleLengthAtForkContinued))]
		public double MinDangleLengthAtForkContinued { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinDangleLengthAtFork))]
		public double MinDangleLengthAtFork { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToSameGroupTypeCovered))]
		public double MinGapToSameGroupTypeCovered { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToSameGroupTypeAtFork))]
		public double MinGapToSameGroupTypeAtFork { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToSameGroupTypeAtForkCovered))]
		public double MinGapToSameGroupTypeAtForkCovered { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToOtherGroupTypeAtFork))]
		public double MinGapToOtherGroupTypeAtFork { get; set; }

		[TestParameter(0)]
		[Doc(nameof(DocStrings.QaLineGroupConstraints_MinGapToSameGroup))]
		public double MinGapToSameGroup { get; set; }
	}
}
