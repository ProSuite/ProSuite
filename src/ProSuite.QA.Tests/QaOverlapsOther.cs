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
	public class QaOverlapsOther : QaSpatialRelationOtherBase
	{
		private readonly string _validRelationConstraintSql;
		private IValidRelationConstraint _validRelationConstraint;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new OverlapsIssueCodes());

		#endregion

		[Doc(nameof(DocStrings.QaOverlapsOther_0))]
		public QaOverlapsOther(
				[Doc(nameof(DocStrings.QaOverlapsOther_overlappedClasses))]
				IList<IFeatureClass> overlapped,
				[Doc(nameof(DocStrings.QaOverlapsOther_overlappingClasses))]
				IList<IFeatureClass> overlapping)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(overlapped, overlapping, null) { }

		[Doc(nameof(DocStrings.QaOverlapsOther_1))]
		public QaOverlapsOther(
				[Doc(nameof(DocStrings.QaOverlapsOther_overlappedClass))]
				IFeatureClass overlapped,
				[Doc(nameof(DocStrings.QaOverlapsOther_overlappingClass))]
				IFeatureClass overlapping)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(overlapped, overlapping, null) { }

		[Doc(nameof(DocStrings.QaOverlapsOther_2))]
		public QaOverlapsOther(
			[Doc(nameof(DocStrings.QaOverlapsOther_overlappedClasses))]
			IList<IFeatureClass> overlappedClasses,
			[Doc(nameof(DocStrings.QaOverlapsOther_overlappingClasses))]
			IList<IFeatureClass> overlappingClasses,
			[Doc(nameof(DocStrings.QaOverlapsOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(
				overlappedClasses, overlappingClasses, esriSpatialRelEnum.esriSpatialRelOverlaps)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
		}

		[Doc(nameof(DocStrings.QaOverlapsOther_3))]
		public QaOverlapsOther(
			[Doc(nameof(DocStrings.QaOverlapsOther_overlappedClass))]
			IFeatureClass overlappedClass,
			[Doc(nameof(DocStrings.QaOverlapsOther_overlappingClass))]
			IFeatureClass overlappingClass,
			[Doc(nameof(DocStrings.QaOverlapsOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] {overlappedClass}, new[] {overlappingClass}, validRelationConstraint) { }

		#region Overrides of QaSpatialRelationOtherBase

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
		{
			if (_validRelationConstraint == null)
			{
				const bool constraintIsDirected = true;
				_validRelationConstraint = new ValidRelationConstraint(
					_validRelationConstraintSql,
					constraintIsDirected,
					GetSqlCaseSensitivity());
			}

			const bool reportIndividualErrors = false;
			return QaSpatialRelationUtils.ReportOverlaps(row1, tableIndex1,
			                                             row2, tableIndex2,
			                                             this, GetIssueCode(),
			                                             _validRelationConstraint,
			                                             reportIndividualErrors);
		}

		#endregion

		[CanBeNull]
		private IssueCode GetIssueCode()
		{
			return _validRelationConstraintSql == null
				       ? Codes[OverlapsIssueCodes.GeometriesOverlap]
				       : Codes[OverlapsIssueCodes.GeometriesOverlap_ConstraintNotFulfilled];
		}
	}
}