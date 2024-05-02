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
	/// <summary>
	/// Check if there are any elements in a group of layers that 
	/// are overlapped by an element within a group of line layers.
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	public class QaOverlapsSelf : QaSpatialRelationSelfBase
	{
		private readonly string _validRelationConstraintSql;
		private IValidRelationConstraint _validRelationConstraint;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new OverlapsIssueCodes());

		#endregion

		[Doc(nameof(DocStrings.QaOverlapsSelf_0))]
		public QaOverlapsSelf(
				[Doc(nameof(DocStrings.QaOverlapsSelf_featureClass))]
				IReadOnlyFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc(nameof(DocStrings.QaOverlapsSelf_1))]
		public QaOverlapsSelf(
				[Doc(nameof(DocStrings.QaOverlapsSelf_featureClasses))]
				IList<IReadOnlyFeatureClass> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaOverlapsSelf_2))]
		public QaOverlapsSelf(
			[Doc(nameof(DocStrings.QaOverlapsSelf_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaOverlapsSelf_validRelationConstraint))]
			string validRelationConstraint)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelOverlaps)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
			AddCustomQueryFilterExpression(validRelationConstraint);
		}

		[Doc(nameof(DocStrings.QaOverlapsSelf_3))]
		public QaOverlapsSelf(
			[Doc(nameof(DocStrings.QaOverlapsSelf_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaOverlapsSelf_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] {featureClass}, validRelationConstraint) { }

		[InternallyUsedTest]
		public QaOverlapsSelf([NotNull] QaOverlapsSelfDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.ValidRelationConstraint) { }

		#region Overrides of QaSpatialRelationSelfBase

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			if (_validRelationConstraint == null)
			{
				const bool constraintIsDirected = false;
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
