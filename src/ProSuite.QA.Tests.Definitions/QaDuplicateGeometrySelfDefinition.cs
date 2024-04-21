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
	public class QaDuplicateGeometrySelfDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public string ValidDuplicateConstraint { get; }
		public bool ReportSingleErrorPerDuplicateSet { get; }

		[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_0))]
		public QaDuplicateGeometrySelfDefinition(
			[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass)
			: this(featureClass, string.Empty) { }

		[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_1))]
		public QaDuplicateGeometrySelfDefinition(
				[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_featureClass))] [NotNull]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_validDuplicateConstraint))]
				[CanBeNull]
				string
					validDuplicateConstraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, validDuplicateConstraint, false) { }

		[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_2))]
		public QaDuplicateGeometrySelfDefinition(
			[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_validDuplicateConstraint))] [CanBeNull]
			string
				validDuplicateConstraint,
			[Doc(nameof(DocStrings.QaDuplicateGeometrySelf_reportSingleErrorPerDuplicateSet))]
			bool
				reportSingleErrorPerDuplicateSet)
			: base(featureClass)
		//: base(featureClass, esriSpatialRelEnum.esriSpatialRelIntersects)

		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			FeatureClass = featureClass;
			ValidDuplicateConstraint = validDuplicateConstraint;
			ReportSingleErrorPerDuplicateSet = reportSingleErrorPerDuplicateSet;

		}
	}
}
