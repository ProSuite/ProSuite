using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

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
			IList<IFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix)
			: this(featureClasses, intersectionMatrix, string.Empty) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_1))]
		public QaIntersectionMatrixSelf(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClasses))] [NotNull]
			IList<IFeatureClass>
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
		}

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_2))]
		public QaIntersectionMatrixSelf(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix)
			: this(featureClass, intersectionMatrix, string.Empty) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_3))]
		public QaIntersectionMatrixSelf(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_constraint))]
			string constraint)
			: this(new[] {featureClass}, intersectionMatrix, constraint) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_4))]
		public QaIntersectionMatrixSelf(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClasses))] [NotNull]
			IList<IFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_constraint))]
			string constraint,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_validIntersectionDimensions))] [CanBeNull]
			string
				validIntersectionDimensions)
			: base(featureClasses, intersectionMatrix)
		{
			_constraint = StringUtils.IsNotEmpty(constraint)
				              ? constraint
				              : null;
			_validIntersectionDimensions = validIntersectionDimensions;
		}

		#endregion

		#region Overrides of QaSpatialRelationSelfBase

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
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
			return _matrixHelper.ReportErrors((IFeature) row1, tableIndex1,
			                                  (IFeature) row2, tableIndex2,
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

		protected override int FindErrorsNoRelated(IRow row)
		{
			IGeometry errorGeometry = ((IFeature) row).ShapeCopy;

			const string description = "No intersection";

			return ReportError(description, errorGeometry,
			                   Codes[IntersectionMatrixIssueCodes.NoIntersection],
			                   TestUtils.GetShapeFieldName(row), row);
		}
	}
}