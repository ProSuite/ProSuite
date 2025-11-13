using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
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
	public class QaTouchesSelf : QaSpatialRelationSelfBase
	{
		[CanBeNull] private readonly string _validRelationConstraintSql;
		[CanBeNull] private IValidRelationConstraint _validRelationConstraint;
		[CanBeNull] private GeometryConstraint _validTouchGeometryConstraint;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new TouchesIssueCodes());

		#endregion

		[Doc(nameof(DocStrings.QaTouchesSelf_0))]
		public QaTouchesSelf(
				[Doc(nameof(DocStrings.QaTouchesSelf_featureClasses))]
				IList<IReadOnlyFeatureClass> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaTouchesSelf_1))]
		public QaTouchesSelf(
				[Doc(nameof(DocStrings.QaTouchesSelf_featureClass))]
				IReadOnlyFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc(nameof(DocStrings.QaTouchesSelf_2))]
		public QaTouchesSelf(
			[Doc(nameof(DocStrings.QaTouchesSelf_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaTouchesSelf_validRelationConstraint))]
			string validRelationConstraint)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelTouches)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
			AddCustomQueryFilterExpression(validRelationConstraint);
		}

		[Doc(nameof(DocStrings.QaTouchesSelf_3))]
		public QaTouchesSelf(
			[Doc(nameof(DocStrings.QaTouchesSelf_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaTouchesSelf_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] {featureClass}, validRelationConstraint) { }

		[InternallyUsedTest]
		public QaTouchesSelf([NotNull] QaTouchesSelfDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.ValidRelationConstraint)
		{
			ValidTouchGeometryConstraint = definition.ValidTouchGeometryConstraint;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaTouchesSelf_ValidTouchGeometryConstraint))]
		public string ValidTouchGeometryConstraint
		{
			get { return _validTouchGeometryConstraint?.Constraint; }
			set
			{
				_validTouchGeometryConstraint = StringUtils.IsNullOrEmptyOrBlank(value)
					                                ? null
					                                : new GeometryConstraint(value);
			}
		}

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

			return QaSpatialRelationUtils.ReportTouches(row1, tableIndex1,
			                                            row2, tableIndex2,
			                                            this, GetIssueCode(),
			                                            _validRelationConstraint,
			                                            _validTouchGeometryConstraint,
			                                            reportIndividualParts: true);
		}

		#endregion

		[CanBeNull]
		private IssueCode GetIssueCode()
		{
			return _validRelationConstraintSql == null
				       ? Codes[TouchesIssueCodes.GeometriesTouch]
				       : Codes[TouchesIssueCodes.GeometriesTouch_ConstraintNotFulfilled];
		}
	}
}
