using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[TopologyTest]
	public class QaInteriorIntersectsOther : QaSpatialRelationOtherBase
	{
		private readonly string _constraint;
		private const string _intersectionMatrix = "T********";
		private IntersectionMatrixHelper _matrixHelper;
		[CanBeNull] private GeometryConstraint _validIntersectionGeometryConstraint;
		[CanBeNull] private ContainsPostProcessor _ignoreAreaProcessor;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes
			=> _codes ?? (_codes = new InteriorIntersectsIssueCodes());

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_0))]
		public QaInteriorIntersectsOther(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClass))] [NotNull]
			IFeatureClass relatedClass)
			: this(featureClass, relatedClass, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_1))]
		public QaInteriorIntersectsOther(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClass))] [NotNull]
			IFeatureClass relatedClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_constraint))] [CanBeNull]
			string constraint)
			: this(new[] {featureClass}, new[] {relatedClass}, constraint) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_2))]
		public QaInteriorIntersectsOther(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClasses))] [NotNull]
			IList<IFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClasses))] [NotNull]
			IList<IFeatureClass> relatedClasses)
			: this(featureClasses, relatedClasses, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_3))]
		public QaInteriorIntersectsOther(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClasses))] [NotNull]
			IList<IFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClasses))] [NotNull]
			IList<IFeatureClass> relatedClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_constraint))] [CanBeNull]
			string constraint)
			: base(featureClasses, relatedClasses, _intersectionMatrix)
		{
			_constraint = StringUtils.IsNotEmpty(constraint)
				              ? constraint
				              : null;
		}

		#endregion

		[TestParameter]
		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_ValidIntersectionGeometryConstraint))]
		public string ValidIntersectionGeometryConstraint
		{
			get { return _validIntersectionGeometryConstraint?.Constraint; }
			set
			{
				_validIntersectionGeometryConstraint = StringUtils.IsNullOrEmptyOrBlank(value)
					                                       ? null
					                                       : new GeometryConstraint(value);
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_IgnoreArea))]
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

		#region Overrides of QaSpatialRelationOtherBase

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));

			if (row1 == row2)
			{
				return 0;
			}

			var feature1 = (IFeature) row1;
			var feature2 = (IFeature) row2;

			// if the test is made from a To row to a From row, then the roles 
			// of the features must be inverted
			bool swapRoles = ! IsInFromTableList(tableIndex1);

			if (_matrixHelper == null)
			{
				const bool constraintIsDirected = true;
				_matrixHelper = new IntersectionMatrixHelper(
					Assert.NotNull(IntersectionMatrix, "matrix"),
					_constraint, constraintIsDirected,
					constraintIsCaseSensitive: GetSqlCaseSensitivity(),
					intersectionGeometryConstraint: _validIntersectionGeometryConstraint);
			}

			const bool reportIndividualErrors = true;
			return swapRoles
				       ? _matrixHelper.ReportErrors(feature2, tableIndex2,
				                                    feature1, tableIndex1,
				                                    this, GetIssueCode(),
				                                    reportIndividualErrors)
				       : _matrixHelper.ReportErrors(feature1, tableIndex1,
				                                    feature2, tableIndex2,
				                                    this, GetIssueCode(),
				                                    reportIndividualErrors);
		}

		#endregion

		[CanBeNull]
		private IssueCode GetIssueCode()
		{
			return _constraint != null || _validIntersectionGeometryConstraint != null
				       ? Codes[
					       InteriorIntersectsIssueCodes.InteriorsIntersect_ConstraintNotFulfilled]
				       : Codes[InteriorIntersectsIssueCodes.InteriorsIntersect];
		}
	}
}
