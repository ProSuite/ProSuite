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

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaCrossesOther : QaSpatialRelationOtherBase
	{
		[CanBeNull] private readonly string _validRelationConstraintSql;
		[CanBeNull] private IValidRelationConstraint _validRelationConstraint;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes =>
			_codes ?? (_codes = new CrossesIssueCodes());

		#endregion

		[Doc(nameof(DocStrings.QaCrossesOther_0))]
		public QaCrossesOther(
				[Doc(nameof(DocStrings.QaCrossesOther_crossedClasses))] IList<IFeatureClass> crossed,
				[Doc(nameof(DocStrings.QaCrossesOther_crossingClasses))]
				IList<IFeatureClass> crossing)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(crossed, crossing, null) { }

		[Doc(nameof(DocStrings.QaCrossesOther_1))]
		public QaCrossesOther(
				[Doc(nameof(DocStrings.QaCrossesOther_crossedClass))] IFeatureClass crossed,
				[Doc(nameof(DocStrings.QaCrossesOther_crossingClass))] IFeatureClass crossing)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(crossed, crossing, null) { }

		[Doc(nameof(DocStrings.QaCrossesOther_2))]
		public QaCrossesOther(
			[Doc(nameof(DocStrings.QaCrossesOther_crossedClasses))] IList<IFeatureClass> crossedClasses,
			[Doc(nameof(DocStrings.QaCrossesOther_crossingClasses))]
			IList<IFeatureClass> crossingClasses,
			[Doc(nameof(DocStrings.QaCrossesOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(crossedClasses, crossingClasses, esriSpatialRelEnum.esriSpatialRelCrosses)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
		}

		[Doc(nameof(DocStrings.QaCrossesOther_3))]
		public QaCrossesOther(
			[Doc(nameof(DocStrings.QaCrossesOther_crossedClass))] IFeatureClass crossedClass,
			[Doc(nameof(DocStrings.QaCrossesOther_crossingClass))] IFeatureClass crossingClass,
			[Doc(nameof(DocStrings.QaCrossesOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] {crossedClass}, new[] {crossingClass}, validRelationConstraint) { }

		#region Overrides of QaSpatialRelationOtherBase

		protected override int FindErrors(IRow row1, int tableIndex1, IRow row2,
		                                  int tableIndex2)
		{
			if (_validRelationConstraint == null)
			{
				const bool constraintIsDirected = true;
				_validRelationConstraint = new ValidRelationConstraint(
					_validRelationConstraintSql,
					constraintIsDirected,
					GetSqlCaseSensitivity());
			}

			const bool reportIndividualParts = true;
			return QaSpatialRelationUtils.ReportCrossings(row1, tableIndex1,
			                                              row2, tableIndex2,
			                                              this, GetIssueCode(),
			                                              _validRelationConstraint,
			                                              reportIndividualParts);
		}

		#endregion

		[CanBeNull]
		private IssueCode GetIssueCode()
		{
			return _validRelationConstraintSql == null
				       ? Codes[CrossesIssueCodes.GeometriesCross]
				       : Codes[CrossesIssueCodes.GeometriesCross_ConstraintNotFulfilled];
		}
	}
}