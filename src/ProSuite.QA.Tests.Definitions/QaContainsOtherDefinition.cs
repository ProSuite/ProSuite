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
	[TopologyTest]
	public class QaContainsOtherDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> Contains { get; }
		public IList<IFeatureClassSchemaDef> IsWithin { get; }
		public string IsContainingCondition { get; }
		public bool ReportIndividualParts { get; }

		[Doc(nameof(DocStrings.QaContainsOther_0))]
		public QaContainsOtherDefinition(
				[Doc(nameof(DocStrings.QaContainsOther_contains_0))] [NotNull]
				IList<IFeatureClassSchemaDef> contains,
				[Doc(nameof(DocStrings.QaContainsOther_isWithin_0))] [NotNull]
				IList<IFeatureClassSchemaDef> isWithin)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(contains, isWithin, null, false) { }

		[Doc(nameof(DocStrings.QaContainsOther_1))]
		public QaContainsOtherDefinition(
			[Doc(nameof(DocStrings.QaContainsOther_contains_1))] [NotNull]
			IFeatureClassSchemaDef contains,
			[Doc(nameof(DocStrings.QaContainsOther_isWithin_1))] [NotNull]
			IFeatureClassSchemaDef isWithin)
			: this(new[] { contains }, new[] { isWithin }, null, false) { }

		[Doc(nameof(DocStrings.QaContainsOther_2))]
		public QaContainsOtherDefinition(
			[Doc(nameof(DocStrings.QaContainsOther_contains_0))] [NotNull]
			IList<IFeatureClassSchemaDef> contains,
			[Doc(nameof(DocStrings.QaContainsOther_isWithin_0))] [NotNull]
			IList<IFeatureClassSchemaDef> isWithin,
			[Doc(nameof(DocStrings.QaContainsOther_isContainingCondition))] [CanBeNull]
			string isContainingCondition,
			[Doc(nameof(DocStrings.QaContainsOther_reportIndividualParts))]
			bool reportIndividualParts)
			: base(CastToTables(contains, isWithin))
		{
			Assert.ArgumentNotNull(contains, nameof(contains));
			Assert.ArgumentNotNull(isWithin, nameof(isWithin));

			Contains = contains;
			IsWithin = isWithin;
			IsContainingCondition = isContainingCondition;
			ReportIndividualParts = reportIndividualParts;
		}

		[Doc(nameof(DocStrings.QaContainsOther_3))]
		public QaContainsOtherDefinition(
			[Doc(nameof(DocStrings.QaContainsOther_contains_1))] [NotNull]
			IFeatureClassSchemaDef contains,
			[Doc(nameof(DocStrings.QaContainsOther_isWithin_1))] [NotNull]
			IFeatureClassSchemaDef isWithin,
			[Doc(nameof(DocStrings.QaContainsOther_isContainingCondition))] [CanBeNull]
			string
				isContainingCondition,
			[Doc(nameof(DocStrings.QaContainsOther_reportIndividualParts))]
			bool reportIndividualParts)
			: this(new[] { contains },
			       new[] { isWithin },
			       isContainingCondition,
			       reportIndividualParts) { }
	}
}
