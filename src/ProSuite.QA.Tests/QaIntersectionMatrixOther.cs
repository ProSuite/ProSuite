using System;
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
	[CLSCompliant(false)]
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

		[Doc("QaIntersectionMatrixOther_0")]
		public QaIntersectionMatrixOther(
			[Doc("QaIntersectionMatrixOther_featureClasses")] [NotNull]
			IList<IFeatureClass>
				featureClasses,
			[Doc("QaIntersectionMatrixOther_relatedClasses")] [NotNull]
			IList<IFeatureClass>
				relatedClasses,
			[Doc("QaIntersectionMatrixOther_intersectionMatrix")] [NotNull]
			string
				intersectionMatrix)
			: this(featureClasses, relatedClasses, intersectionMatrix, string.Empty, null) { }

		[Doc("QaIntersectionMatrixOther_1")]
		public QaIntersectionMatrixOther(
				[Doc("QaIntersectionMatrixOther_featureClasses")] [NotNull]
				IList<IFeatureClass>
					featureClasses,
				[Doc("QaIntersectionMatrixOther_relatedClasses")] [NotNull]
				IList<IFeatureClass>
					relatedClasses,
				[Doc("QaIntersectionMatrixOther_intersectionMatrix")] [NotNull]
				string
					intersectionMatrix,
				[Doc("QaIntersectionMatrixOther_constraint")] [CanBeNull]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, relatedClasses, intersectionMatrix, constraint, null) { }

		[Doc("QaIntersectionMatrixOther_2")]
		public QaIntersectionMatrixOther(
			[Doc("QaIntersectionMatrixOther_featureClass")] [NotNull]
			IFeatureClass
				featureClass,
			[Doc("QaIntersectionMatrixOther_relatedClass")] [NotNull]
			IFeatureClass
				relatedClass,
			[Doc("QaIntersectionMatrixOther_intersectionMatrix")] [NotNull]
			string
				intersectionMatrix)
			: this(featureClass, relatedClass, intersectionMatrix, string.Empty) { }

		[Doc("QaIntersectionMatrixOther_3")]
		public QaIntersectionMatrixOther(
			[Doc("QaIntersectionMatrixOther_featureClass")] [NotNull]
			IFeatureClass
				featureClass,
			[Doc("QaIntersectionMatrixOther_relatedClass")] [NotNull]
			IFeatureClass
				relatedClass,
			[Doc("QaIntersectionMatrixOther_intersectionMatrix")] [NotNull]
			string
				intersectionMatrix,
			[Doc("QaIntersectionMatrixOther_constraint")] [CanBeNull]
			string constraint)
			: this(new[] {featureClass}, new[] {relatedClass}, intersectionMatrix, constraint) { }

		[Doc("QaIntersectionMatrixOther_4")]
		public QaIntersectionMatrixOther(
			[Doc("QaIntersectionMatrixOther_featureClasses")] [NotNull]
			IList<IFeatureClass>
				featureClasses,
			[Doc("QaIntersectionMatrixOther_relatedClasses")] [NotNull]
			IList<IFeatureClass>
				relatedClasses,
			[Doc("QaIntersectionMatrixOther_intersectionMatrix")] [NotNull]
			string
				intersectionMatrix,
			[Doc("QaIntersectionMatrixOther_constraint")] [CanBeNull]
			string constraint,
			[Doc("QaIntersectionMatrixOther_validIntersectionDimensions")] [CanBeNull]
			string
				validIntersectionDimensions)
			: base(featureClasses, relatedClasses, intersectionMatrix)
		{
			_constraint = StringUtils.IsNotEmpty(constraint)
				              ? constraint
				              : null;
			_validIntersectionDimensions = validIntersectionDimensions;
		}

		#endregion

		#region Overrides of QaSpatialRelationOtherBase

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));

			if (row1 == row2)
			{
				return NoError;
			}

			var feature1 = (IFeature) row1;
			var feature2 = (IFeature) row2;

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
