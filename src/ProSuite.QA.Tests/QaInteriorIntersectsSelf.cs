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
	public class QaInteriorIntersectsSelf : QaSpatialRelationSelfBase
	{
		private readonly string _constraint;
		private readonly bool _reportIntersectionsAsMultipart;
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

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_0))]
		public QaInteriorIntersectsSelf(
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass)
			: this(featureClass, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_1))]
		public QaInteriorIntersectsSelf(
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_constraint))]
			string constraint)
			: this(new[] { featureClass }, constraint) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_2))]
		public QaInteriorIntersectsSelf(
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				featureClasses)
			: this(featureClasses, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_3))]
		public QaInteriorIntersectsSelf(
				[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClasses))] [NotNull]
				IList<IReadOnlyFeatureClass>
					featureClasses,
				[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_constraint))]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, constraint, false) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_4))]
		public QaInteriorIntersectsSelf(
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_constraint))]
			string constraint,
			[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_reportIntersectionsAsMultipart))]
			bool
				reportIntersectionsAsMultipart)
			: base(featureClasses, _intersectionMatrix)
		{
			_constraint = StringUtils.IsNotEmpty(constraint)
				              ? constraint
				              : null;
			AddCustomQueryFilterExpression(constraint);
			_reportIntersectionsAsMultipart = reportIntersectionsAsMultipart;
		}

		[InternallyUsedTest]
		public QaInteriorIntersectsSelf(QaInteriorIntersectsSelfDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.Constraint, definition.ReportIntersectionsAsMultipart
			)
		{
			ValidIntersectionGeometryConstraint = definition.ValidIntersectionGeometryConstraint;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaInteriorIntersectsSelf_ValidIntersectionGeometryConstraint))]
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

		#region Overrides of QaSpatialRelationSelfBase

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));

			if (row1 == row2)
			{
				return 0;
			}

			if (_matrixHelper == null)
			{
				_matrixHelper = new IntersectionMatrixHelper(
					Assert.NotNull(IntersectionMatrix, "matrix"),
					_constraint,
					constraintIsCaseSensitive: GetSqlCaseSensitivity(),
					intersectionGeometryConstraint: _validIntersectionGeometryConstraint);
			}

			bool reportIndividualErrors = ! _reportIntersectionsAsMultipart;
			return _matrixHelper.ReportErrors((IReadOnlyFeature) row1, tableIndex1,
			                                  (IReadOnlyFeature) row2, tableIndex2,
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
