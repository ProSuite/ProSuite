using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaIntersectsOtherDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> IntersectedClasses { get; }
		public IList<IFeatureClassSchemaDef> IntersectingClasses { get; }
		public string ValidRelationConstraint { get; }

		private string _validRelationConstraintSql;

		private const bool _defaultReportIntersectionsAsMultipart = true;

		[Doc(nameof(DocStrings.QaIntersectsOther_0))]
		public QaIntersectsOtherDefinition(
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> intersected,
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> intersecting)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(intersected, intersecting, null) { }

		[Doc(nameof(DocStrings.QaIntersectsOther_1))]
		public QaIntersectsOtherDefinition(
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClass))] [NotNull]
				IFeatureClassSchemaDef intersected,
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClass))] [NotNull]
				IFeatureClassSchemaDef intersecting)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(intersected, intersecting, null) { }

		[Doc(nameof(DocStrings.QaIntersectsOther_2))]
		public QaIntersectsOtherDefinition(
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> intersectedClasses,
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> intersectingClasses,
			[Doc(nameof(DocStrings.QaIntersectsOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(intersectedClasses.Union(intersectingClasses))
		{
			IntersectedClasses = intersectedClasses;
			IntersectingClasses = intersectingClasses;
			ValidRelationConstraint = validRelationConstraint;

			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
		}

		[Doc(nameof(DocStrings.QaIntersectsOther_3))]
		public QaIntersectsOtherDefinition(
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClass))] [NotNull]
			IFeatureClassSchemaDef intersectedClass,
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClass))] [NotNull]
			IFeatureClassSchemaDef intersectingClass,
			[Doc(nameof(DocStrings.QaIntersectsOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { intersectedClass }, new[] { intersectingClass }, validRelationConstraint
			) { }

		[TestParameter(_defaultReportIntersectionsAsMultipart)]
		[Doc(nameof(DocStrings.QaIntersectsOther_ReportIntersectionsAsMultipart))]
		public bool ReportIntersectionsAsMultipart { get; set; } =
			_defaultReportIntersectionsAsMultipart;

		[TestParameter]
		[Doc(nameof(DocStrings.QaIntersectsOther_ValidIntersectionGeometryConstraint))]
		public string ValidIntersectionGeometryConstraint { get; set; }
	}
}
