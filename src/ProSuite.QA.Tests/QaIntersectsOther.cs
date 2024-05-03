using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaIntersectsOther : QaSpatialRelationOtherBase
	{
		[CanBeNull] private readonly string _validRelationConstraintSql;
		[CanBeNull] private IValidRelationConstraint _validRelationConstraint;
		[CanBeNull] private GeometryConstraint _validIntersectionGeometryConstraint;
		private const bool _defaultReportIntersectionsAsMultipart = true;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new IntersectsIssueCodes());

		#endregion

		[Doc(nameof(DocStrings.QaIntersectsOther_0))]
		public QaIntersectsOther(
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClasses))] [NotNull]
				IList<IReadOnlyFeatureClass> intersected,
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClasses))] [NotNull]
				IList<IReadOnlyFeatureClass> intersecting)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(intersected, intersecting, null) { }

		[Doc(nameof(DocStrings.QaIntersectsOther_1))]
		public QaIntersectsOther(
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClass))] [NotNull]
				IReadOnlyFeatureClass intersected,
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClass))] [NotNull]
				IReadOnlyFeatureClass intersecting)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(intersected, intersecting, null) { }

		[Doc(nameof(DocStrings.QaIntersectsOther_2))]
		public QaIntersectsOther(
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> intersectedClasses,
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> intersectingClasses,
			[Doc(nameof(DocStrings.QaIntersectsOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(
				intersectedClasses, intersectingClasses,
				esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
			AddCustomQueryFilterExpression(validRelationConstraint);
		}

		[Doc(nameof(DocStrings.QaIntersectsOther_3))]
		public QaIntersectsOther(
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClass))] [NotNull]
			IReadOnlyFeatureClass intersectedClass,
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClass))] [NotNull]
			IReadOnlyFeatureClass intersectingClass,
			[Doc(nameof(DocStrings.QaIntersectsOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { intersectedClass }, new[] { intersectingClass }, validRelationConstraint
			) { }

		[InternallyUsedTest]
		public QaIntersectsOther(QaIntersectsOtherDefinition definition)
			: this(definition.IntersectedClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.IntersectingClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.ValidRelationConstraint)
		{
			ReportIntersectionsAsMultipart = definition.ReportIntersectionsAsMultipart;
			ValidIntersectionGeometryConstraint = definition.ValidIntersectionGeometryConstraint;
		}

		#region Overrides of QaSpatialRelationOtherBase

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			if (_validRelationConstraint == null)
			{
				const bool constraintIsDirected = true;
				_validRelationConstraint = new ValidRelationConstraint(
					_validRelationConstraintSql, constraintIsDirected,
					GetSqlCaseSensitivity());
			}

			return QaSpatialRelationUtils.ReportIntersections(
				row1, tableIndex1,
				row2, tableIndex2,
				this, GetIssueCode(),
				_validRelationConstraint,
				! ReportIntersectionsAsMultipart,
				_validIntersectionGeometryConstraint);
		}

		#endregion

		[TestParameter(_defaultReportIntersectionsAsMultipart)]
		[Doc(nameof(DocStrings.QaIntersectsOther_ReportIntersectionsAsMultipart))]
		public bool ReportIntersectionsAsMultipart { get; set; } =
			_defaultReportIntersectionsAsMultipart;

		[TestParameter]
		[Doc(nameof(DocStrings.QaIntersectsOther_ValidIntersectionGeometryConstraint))]
		public string ValidIntersectionGeometryConstraint
		{
			get { return _validIntersectionGeometryConstraint?.Constraint; }
			set
			{
				_validIntersectionGeometryConstraint =
					StringUtils.IsNullOrEmptyOrBlank(value)
						? null
						: new GeometryConstraint(value);
			}
		}

		[CanBeNull]
		private IssueCode GetIssueCode()
		{
			return _validRelationConstraintSql == null ||
			       _validIntersectionGeometryConstraint != null
				       ? Codes[IntersectsIssueCodes.GeometriesIntersect]
				       : Codes[
					       IntersectsIssueCodes
						       .GeometriesIntersect_ConstraintNotFulfilled];
		}
	}
}
