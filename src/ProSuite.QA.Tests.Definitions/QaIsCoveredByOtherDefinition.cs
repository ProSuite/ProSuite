using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
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
	[TopologyTest]
	public class QaIsCoveredByOtherDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> Covering { get; }
		public IList<GeometryComponent> CoveringGeometryComponents { get; }
		public IList<IFeatureClassSchemaDef> Covered { get; }
		public IList<GeometryComponent> CoveredGeometryComponents { get; }
		public IList<string> IsCoveringConditions { get; }
		public double AllowedUncoveredPercentage { get; }
		public IList<IFeatureClassSchemaDef> AreaOfInterestClasses { get; }

		[CanBeNull] private string _validUncoveredGeometryConstraint;

		private readonly IList<double> _coveringClassTolerances =
			new ReadOnlyList<double>(new List<double>());

		#region Constructors

		[Doc(nameof(DocStrings.QaIsCoveredByOther_0))]
		public QaIsCoveredByOtherDefinition(
				[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
				IList<IFeatureClassSchemaDef> covering,
				[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
				IList<IFeatureClassSchemaDef> covered)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(covering, covered, null) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_1))]
		public QaIsCoveredByOtherDefinition(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_1))] [NotNull]
			IFeatureClassSchemaDef covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_1))] [NotNull]
			IFeatureClassSchemaDef covered)
			: this(new[] { covering }, new[] { covered }, null) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_2))]
		public QaIsCoveredByOtherDefinition(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClassSchemaDef> covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClassSchemaDef> covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringCondition))]
			string isCoveringCondition)
			: this(covering, new[] { GeometryComponent.EntireGeometry },
			       covered, new[] { GeometryComponent.EntireGeometry },
			       isCoveringCondition) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_3))]
		public QaIsCoveredByOtherDefinition(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_1))] [NotNull]
			IFeatureClassSchemaDef covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_1))] [NotNull]
			IFeatureClassSchemaDef covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringCondition))]
			string isCoveringCondition)
			: this(new[] { covering }, new[] { covered }, isCoveringCondition) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_4))]
		public QaIsCoveredByOtherDefinition(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClassSchemaDef> covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveringGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveringGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClassSchemaDef>
				covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveredGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveredGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringCondition))] [CanBeNull]
			string isCoveringCondition)
			: this(covering, coveringGeometryComponents,
			       covered, coveredGeometryComponents,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       isCoveringCondition, 0d) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_5))]
		public QaIsCoveredByOtherDefinition(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClassSchemaDef> covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveringGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveringGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClassSchemaDef> covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveredGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveredGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringCondition))] [CanBeNull]
			string isCoveringCondition,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_allowedUncoveredPercentage))]
			double allowedUncoveredPercentage)
			: this(covering, coveringGeometryComponents,
			       covered, coveredGeometryComponents,
			       string.IsNullOrEmpty(isCoveringCondition)
				       ? null
				       : new[] { isCoveringCondition },
			       allowedUncoveredPercentage) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_6))]
		public QaIsCoveredByOtherDefinition(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClassSchemaDef> covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveringGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveringGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClassSchemaDef> covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveredGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveredGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringConditions))] [CanBeNull]
			IList<string> isCoveringConditions,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_allowedUncoveredPercentage))]
			double allowedUncoveredPercentage)
			: this(covering, coveringGeometryComponents,
			       covered, coveredGeometryComponents,
			       isCoveringConditions,
			       allowedUncoveredPercentage,
			       new List<IFeatureClassSchemaDef>()) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_7))]
		public QaIsCoveredByOtherDefinition(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClassSchemaDef> covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveringGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveringGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClassSchemaDef> covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveredGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveredGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringConditions))] [CanBeNull]
			IList<string> isCoveringConditions,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_allowedUncoveredPercentage))]
			double allowedUncoveredPercentage,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_areaOfInterestClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> areaOfInterestClasses)
			: base(CastToTables(covering, covered, areaOfInterestClasses))
		{
			Assert.ArgumentNotNull(covering, nameof(covering));
			Assert.ArgumentNotNull(covered, nameof(covered));
			Assert.ArgumentNotNull(coveringGeometryComponents,
			                       nameof(coveringGeometryComponents));
			Assert.ArgumentNotNull(coveredGeometryComponents,
			                       nameof(coveredGeometryComponents));
			Assert.ArgumentCondition(allowedUncoveredPercentage >= 0,
			                         "allowed uncovered percentage must be >= 0");
			Assert.ArgumentCondition(allowedUncoveredPercentage < 100,
			                         "allowed uncovered percentage must be < 100");
			Assert.ArgumentCondition(isCoveringConditions == null ||
			                         isCoveringConditions.Count == 0 ||
			                         isCoveringConditions.Count == 1 ||
			                         isCoveringConditions.Count ==
			                         covering.Count * covered.Count,
			                         "unexpected number of isCovering conditions " +
			                         "(must be 0, 1, or # of covering classes * # of covered classes");

			Covering = covering;
			CoveringGeometryComponents = coveringGeometryComponents;
			Covered = covered;
			CoveredGeometryComponents = coveredGeometryComponents;
			IsCoveringConditions = isCoveringConditions;
			AllowedUncoveredPercentage = allowedUncoveredPercentage;
			AreaOfInterestClasses = areaOfInterestClasses;
		}

		#endregion

		[Doc(nameof(DocStrings.QaIsCoveredByOther_CoveringClassTolerances))]
		[TestParameter]
		public IList<double> CoveringClassTolerances
		{
			get { return _coveringClassTolerances; }
			set
			{
				Assert.ArgumentCondition(value == null ||
				                         value.Count == 0 || value.Count == 1 ||
				                         value.Count == Covering.Count,
				                         "unexpected number of covering class tolerance values " +
				                         "(must be 0, 1, or equal to the number of covering classes");
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaIsCoveredByOther_ValidUncoveredGeometryConstraint))]
		public string ValidUncoveredGeometryConstraint
		{
			get => _validUncoveredGeometryConstraint;
			set => _validUncoveredGeometryConstraint = value;
		}
	}
}
