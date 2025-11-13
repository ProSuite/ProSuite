using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaIntersectionMatrixSelf : QaSpatialRelationSelfBase
	{
		private readonly string _constraint;
		private readonly string _validIntersectionDimensions;
		private IntersectionMatrixHelper _matrixHelper;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes =>
			_codes ?? (_codes = new IntersectionMatrixIssueCodes());

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_0))]
		public QaIntersectionMatrixSelf(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix)
			: this(featureClasses, intersectionMatrix, string.Empty) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_1))]
		public QaIntersectionMatrixSelf(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_constraint))]
			string constraint)
			: base(featureClasses, intersectionMatrix)
		{
			_constraint = StringUtils.IsNotEmpty(constraint)
				              ? constraint
				              : null;
			AddCustomQueryFilterExpression(constraint);
		}

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_2))]
		public QaIntersectionMatrixSelf(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix)
			: this(featureClass, intersectionMatrix, string.Empty) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_3))]
		public QaIntersectionMatrixSelf(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_constraint))]
			string constraint)
			: this(new[] {featureClass}, intersectionMatrix, constraint) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_4))]
		public QaIntersectionMatrixSelf(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_constraint))]
			string constraint,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_validIntersectionDimensions))]
			[CanBeNull]
			string
				validIntersectionDimensions)
			: base(featureClasses, intersectionMatrix)
		{
			_constraint = StringUtils.IsNotEmpty(constraint)
				              ? constraint
				              : null;
			AddCustomQueryFilterExpression(constraint);
			_validIntersectionDimensions = validIntersectionDimensions;
		}

		[InternallyUsedTest]
		public QaIntersectionMatrixSelf(QaIntersectionMatrixSelfDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.IntersectionMatrix,
			       definition.Constraint, definition.ValidIntersectionDimensions)
		{ }

		#endregion

		#region Overrides of QaSpatialRelationSelfBase

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));

			if (row1 == row2)
			{
				return NoError;
			}

			if (_matrixHelper == null)
			{
				const bool constraintIsDirected = false;
				_matrixHelper = new IntersectionMatrixHelper(
					Assert.NotNull(IntersectionMatrix, "matrix"),
					_constraint, constraintIsDirected,
					QaSpatialRelationUtils.ParseDimensions(_validIntersectionDimensions),
					GetSqlCaseSensitivity());
			}

			const bool reportIndividualErrors = true;
			return _matrixHelper.ReportErrors((IReadOnlyFeature) row1, tableIndex1,
			                                  (IReadOnlyFeature) row2, tableIndex2,
			                                  this, GetIssueCode(),
			                                  reportIndividualErrors);
		}

		#endregion

		[CanBeNull]
		private IssueCode GetIssueCode()
		{
			return _constraint != null
				       ? Codes[
					       IntersectionMatrixIssueCodes
						       .GeometriesIntersectWithMatrix_ConstraintNotFulfilled]
				       : Codes[IntersectionMatrixIssueCodes.GeometriesIntersectWithMatrix];
		}

		protected override int FindErrorsNoRelated(IReadOnlyRow row)
		{
			IGeometry errorGeometry = ((IReadOnlyFeature) row).ShapeCopy;

			const string description = "No intersection";

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
				Codes[IntersectionMatrixIssueCodes.NoIntersection],
				TestUtils.GetShapeFieldName(row));
		}
	}
}
