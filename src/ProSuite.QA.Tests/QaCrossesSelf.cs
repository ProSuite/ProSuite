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
	public class QaCrossesSelf : QaSpatialRelationSelfBase
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

		[Doc(nameof(DocStrings.QaCrossesSelf_0))]
		public QaCrossesSelf(
				[Doc(nameof(DocStrings.QaCrossesSelf_featureClasses))]
				IList<IReadOnlyFeatureClass> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaCrossesSelf_1))]
		public QaCrossesSelf(
				[Doc(nameof(DocStrings.QaCrossesSelf_featureClass))]
				IReadOnlyFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc(nameof(DocStrings.QaCrossesSelf_2))]
		public QaCrossesSelf(
			[Doc(nameof(DocStrings.QaCrossesSelf_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaCrossesSelf_validRelationConstraint))]
			string validRelationConstraint)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelCrosses)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
			AddCustomQueryFilterExpression(validRelationConstraint);
		}

		[Doc(nameof(DocStrings.QaCrossesSelf_3))]
		public QaCrossesSelf(
			[Doc(nameof(DocStrings.QaCrossesSelf_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaCrossesSelf_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] {featureClass}, validRelationConstraint) { }

		[InternallyUsedTest]
		public QaCrossesSelf(QaCrossesSelfDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.ValidRelationConstraint
			)
		{ }

		#region Overrides of QaSpatialRelationSelfBase

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1, IReadOnlyRow row2,
		                                  int tableIndex2)
		{
			if (_validRelationConstraint == null)
			{
				const bool constraintIsDirected = false;
				_validRelationConstraint = new ValidRelationConstraint(
					_validRelationConstraintSql,
					constraintIsDirected,
					GetSqlCaseSensitivity());
			}

			const bool reportIndividualErrors = true;
			return QaSpatialRelationUtils.ReportCrossings(row1, tableIndex1,
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
				       ? Codes[CrossesIssueCodes.GeometriesCross]
				       : Codes[CrossesIssueCodes.GeometriesCross_ConstraintNotFulfilled];
		}
	}
}
