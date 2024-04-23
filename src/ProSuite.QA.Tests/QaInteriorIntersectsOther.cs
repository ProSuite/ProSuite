using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
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
	public class QaInteriorIntersectsOther : QaSpatialRelationOtherBase
	{
		private readonly string _constraint;
		private const string _intersectionMatrix = "T********";
		private IntersectionMatrixHelper _matrixHelper;
		[CanBeNull] private GeometryConstraint _validIntersectionGeometryConstraint;

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
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClass))] [NotNull]
			IReadOnlyFeatureClass relatedClass)
			: this(featureClass, relatedClass, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_1))]
		public QaInteriorIntersectsOther(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClass))] [NotNull]
			IReadOnlyFeatureClass relatedClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_constraint))] [CanBeNull]
			string constraint)
			: this(new[] { featureClass }, new[] { relatedClass }, constraint) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_2))]
		public QaInteriorIntersectsOther(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> relatedClasses)
			: this(featureClasses, relatedClasses, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_3))]
		public QaInteriorIntersectsOther(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> relatedClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_constraint))] [CanBeNull]
			string constraint)
			: base(featureClasses, relatedClasses, _intersectionMatrix)
		{
			_constraint = StringUtils.IsNotEmpty(constraint)
				              ? constraint
				              : null;
			AddCustomQueryFilterExpression(constraint);
		}

		#endregion

		[InternallyUsedTest]
		public QaInteriorIntersectsOther(QaInteriorIntersectsOtherDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.RelatedClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.Constraint
			)
		{
			ValidIntersectionGeometryConstraint = definition.ValidIntersectionGeometryConstraint;
		}

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

		#region Overrides of QaSpatialRelationOtherBase

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));

			if (row1 == row2)
			{
				return 0;
			}

			var feature1 = (IReadOnlyFeature) row1;
			var feature2 = (IReadOnlyFeature) row2;

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
