using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaIntersectsSelfDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public string ValidRelationConstraint { get; }

		private const bool _defaultReportIntersectionsAsMultipart = true;

		[Doc(nameof(DocStrings.QaIntersectsSelf_0))]
		public QaIntersectsSelfDefinition(
				[Doc(nameof(DocStrings.QaIntersectsSelf_featureClass))]
				IFeatureClassSchemaDef featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc(nameof(DocStrings.QaIntersectsSelf_1))]
		public QaIntersectsSelfDefinition(
				[Doc(nameof(DocStrings.QaIntersectsSelf_featureClasses))]
				IList<IFeatureClassSchemaDef> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaIntersectsSelf_2))]
		public QaIntersectsSelfDefinition(
			[Doc(nameof(DocStrings.QaIntersectsSelf_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaIntersectsSelf_validRelationConstraint))]
			string validRelationConstraint)
			: base(featureClasses)
		{
			Assert.ArgumentCondition(featureClasses.Count > 0, "empty featureClasses");

			FeatureClasses = featureClasses;
			ValidRelationConstraint = validRelationConstraint;
		}

		[Doc(nameof(DocStrings.QaIntersectsSelf_3))]
		public QaIntersectsSelfDefinition(
			[Doc(nameof(DocStrings.QaIntersectsSelf_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaIntersectsSelf_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { featureClass }, validRelationConstraint) { }

		[TestParameter(_defaultReportIntersectionsAsMultipart)]
		[Doc(nameof(DocStrings.QaIntersectsSelf_ReportIntersectionsAsMultipart))]
		public bool ReportIntersectionsAsMultipart { get; set; } =
			_defaultReportIntersectionsAsMultipart;

		[TestParameter]
		[Doc(nameof(DocStrings.QaIntersectsSelf_ValidIntersectionGeometryConstraint))]
		public string ValidIntersectionGeometryConstraint { get; set; }

		[Doc(nameof(DocStrings.QaIntersectsSelf_GeometryComponents))]
		[TestParameter]
		public IList<GeometryComponent> GeometryComponents { get; set; }
	}
}
