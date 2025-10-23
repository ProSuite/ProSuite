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
	public class QaIntersectionMatrixOther : QaSpatialRelationOtherBase
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

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_0))]
		public QaIntersectionMatrixOther(
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				relatedClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix)
			: this(featureClasses, relatedClasses, intersectionMatrix, string.Empty, null) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_1))]
		public QaIntersectionMatrixOther(
				[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClasses))] [NotNull]
				IList<IReadOnlyFeatureClass>
					featureClasses,
				[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClasses))] [NotNull]
				IList<IReadOnlyFeatureClass>
					relatedClasses,
				[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
				string
					intersectionMatrix,
				[Doc(nameof(DocStrings.QaIntersectionMatrixOther_constraint))] [CanBeNull]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, relatedClasses, intersectionMatrix, constraint, null) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_2))]
		public QaIntersectionMatrixOther(
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClass))] [NotNull]
			IReadOnlyFeatureClass
				featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClass))] [NotNull]
			IReadOnlyFeatureClass
				relatedClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix)
			: this(featureClass, relatedClass, intersectionMatrix, string.Empty) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_3))]
		public QaIntersectionMatrixOther(
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClass))] [NotNull]
			IReadOnlyFeatureClass
				featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClass))] [NotNull]
			IReadOnlyFeatureClass
				relatedClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_constraint))] [CanBeNull]
			string constraint)
			: this(new[] {featureClass}, new[] {relatedClass}, intersectionMatrix, constraint) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_4))]
		public QaIntersectionMatrixOther(
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				relatedClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_constraint))] [CanBeNull]
			string constraint,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_validIntersectionDimensions))]
			[CanBeNull]
			string
				validIntersectionDimensions)
			: base(featureClasses, relatedClasses, intersectionMatrix)
		{
			_constraint = StringUtils.IsNotEmpty(constraint)
				              ? constraint
				              : null;
			AddCustomQueryFilterExpression(constraint);
			_validIntersectionDimensions = validIntersectionDimensions;
		}

		[InternallyUsedTest]
		public QaIntersectionMatrixOther(QaIntersectionMatrixOtherDefinition definition)

			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>()
							 .ToList(),
				   definition.RelatedClasses.Cast<IReadOnlyFeatureClass>()
							 .ToList(),
				   definition.IntersectionMatrix,
				   definition.Constraint, definition.ValidIntersectionDimensions)
		{ }

		#endregion

		#region Overrides of QaSpatialRelationOtherBase

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));

			if (row1 == row2)
			{
				return NoError;
			}

			var feature1 = (IReadOnlyFeature) row1;
			var feature2 = (IReadOnlyFeature) row2;

			// if the test is made from a To row to a From row, then the roles of the features must 
			// be inverted
			bool swapRoles = ! IsInFromTableList(tableIndex1);

			if (_matrixHelper == null)
			{
				const bool constraintIsDirected = true;
				_matrixHelper = new IntersectionMatrixHelper(
					Assert.NotNull(IntersectionMatrix, "matrix"),
					_constraint, constraintIsDirected,
					QaSpatialRelationUtils.ParseDimensions(_validIntersectionDimensions),
					GetSqlCaseSensitivity());
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
