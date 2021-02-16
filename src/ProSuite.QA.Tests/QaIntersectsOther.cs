using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaIntersectsOther : QaSpatialRelationOtherBase
	{
		[CanBeNull] private readonly string _validRelationConstraintSql;
		[CanBeNull] private IValidRelationConstraint _validRelationConstraint;
		[CanBeNull] private GeometryConstraint _validIntersectionGeometryConstraint;
		[CanBeNull] private ContainsPostProcessor _ignoreAreaProcessor;
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
				IList<IFeatureClass> intersected,
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClasses))] [NotNull]
				IList<IFeatureClass> intersecting)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(intersected, intersecting, null) { }

		[Doc(nameof(DocStrings.QaIntersectsOther_1))]
		public QaIntersectsOther(
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClass))] [NotNull]
				IFeatureClass intersected,
				[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClass))] [NotNull]
				IFeatureClass intersecting)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(intersected, intersecting, null) { }

		[Doc(nameof(DocStrings.QaIntersectsOther_2))]
		public QaIntersectsOther(
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClasses))] [NotNull]
			IList<IFeatureClass> intersectedClasses,
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClasses))] [NotNull]
			IList<IFeatureClass> intersectingClasses,
			[Doc(nameof(DocStrings.QaIntersectsOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(
				intersectedClasses, intersectingClasses,
				esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
		}

		[Doc(nameof(DocStrings.QaIntersectsOther_3))]
		public QaIntersectsOther(
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectedClass))] [NotNull]
			IFeatureClass intersectedClass,
			[Doc(nameof(DocStrings.QaIntersectsOther_intersectingClass))] [NotNull]
			IFeatureClass intersectingClass,
			[Doc(nameof(DocStrings.QaIntersectsOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] {intersectedClass}, new[] {intersectingClass}, validRelationConstraint
			) { }

		#region Overrides of QaSpatialRelationOtherBase

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
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

		[TestParameter]
		[Doc(nameof(DocStrings.QaIntersectsOther_IgnoreArea))]
		public IFeatureClass IgnoreArea
		{
			get { return _ignoreAreaProcessor?.FeatureClass; }
			set
			{
				_ignoreAreaProcessor?.Dispose();
				_ignoreAreaProcessor =
					value != null ? new ContainsPostProcessor(this, value) : null;
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
