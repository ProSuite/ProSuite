using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
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
				[Doc(nameof(DocStrings.QaCrossesOther_crossedClasses))]
				IList<IReadOnlyFeatureClass> crossed,
				[Doc(nameof(DocStrings.QaCrossesOther_crossingClasses))]
				IList<IReadOnlyFeatureClass> crossing)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(crossed, crossing, null) { }

		[Doc(nameof(DocStrings.QaCrossesOther_1))]
		public QaCrossesOther(
				[Doc(nameof(DocStrings.QaCrossesOther_crossedClass))]
				IReadOnlyFeatureClass crossed,
				[Doc(nameof(DocStrings.QaCrossesOther_crossingClass))]
				IReadOnlyFeatureClass crossing)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(crossed, crossing, null) { }

		[Doc(nameof(DocStrings.QaCrossesOther_2))]
		public QaCrossesOther(
			[Doc(nameof(DocStrings.QaCrossesOther_crossedClasses))]
			IList<IReadOnlyFeatureClass> crossedClasses,
			[Doc(nameof(DocStrings.QaCrossesOther_crossingClasses))]
			IList<IReadOnlyFeatureClass> crossingClasses,
			[Doc(nameof(DocStrings.QaCrossesOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(crossedClasses, crossingClasses, esriSpatialRelEnum.esriSpatialRelCrosses)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
			AddCustomQueryFilterExpression(validRelationConstraint);
		}

		[Doc(nameof(DocStrings.QaCrossesOther_3))]
		public QaCrossesOther(
			[Doc(nameof(DocStrings.QaCrossesOther_crossedClass))]
			IReadOnlyFeatureClass crossedClass,
			[Doc(nameof(DocStrings.QaCrossesOther_crossingClass))]
			IReadOnlyFeatureClass crossingClass,
			[Doc(nameof(DocStrings.QaCrossesOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { crossedClass }, new[] { crossingClass }, validRelationConstraint) { }

		[InternallyUsedTest]
		public QaCrossesOther(QaCrossesOtherDefinition definition)
			: this(definition.CrossedClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.CrossingClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.ValidRelationConstraint) { }

		#region Overrides of QaSpatialRelationOtherBase

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1, IReadOnlyRow row2,
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
