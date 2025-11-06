using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaInteriorIntersectsSelfDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; private set; }
		public string Constraint { get; }
		public bool ReportIntersectionsAsMultipart { get; set; }

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_0))]
		public QaInteriorIntersectsSelfDefinition(
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass)
			: this(featureClass, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_1))]
		public QaInteriorIntersectsSelfDefinition(
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_constraint))]
			string constraint)
			: this(new[] { featureClass }, constraint) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_2))]
		public QaInteriorIntersectsSelfDefinition(
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				featureClasses)
			: this(featureClasses, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_3))]
		public QaInteriorIntersectsSelfDefinition(
				[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClasses))] [NotNull]
				IList<IFeatureClassSchemaDef>
					featureClasses,
				[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_constraint))]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, constraint, false) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_4))]
		public QaInteriorIntersectsSelfDefinition(
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				featureClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_constraint))]
			string constraint,
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_reportIntersectionsAsMultipart))]
			bool
				reportIntersectionsAsMultipart)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			Constraint = constraint;
			ReportIntersectionsAsMultipart = reportIntersectionsAsMultipart;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_ValidIntersectionGeometryConstraint))]
		public string ValidIntersectionGeometryConstraint { get; set; }
	}
}
