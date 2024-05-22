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
	public class QaTouchesOther : QaSpatialRelationOtherBase
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

		[Doc(nameof(DocStrings.QaTouchesOther_0))]
		public QaTouchesOther(
				[Doc(nameof(DocStrings.QaTouchesOther_touchingClasses))]
				IList<IReadOnlyFeatureClass> touching,
				[Doc(nameof(DocStrings.QaTouchesOther_touchedClasses))]
				IList<IReadOnlyFeatureClass> touched)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(touching, touched, null) { }

		[Doc(nameof(DocStrings.QaTouchesOther_1))]
		public QaTouchesOther(
				[Doc(nameof(DocStrings.QaTouchesOther_touchingClass))]
				IReadOnlyFeatureClass touching,
				[Doc(nameof(DocStrings.QaTouchesOther_touchedClass))]
				IReadOnlyFeatureClass touched)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(touching, touched, null) { }

		[Doc(nameof(DocStrings.QaTouchesOther_2))]
		public QaTouchesOther(
			[Doc(nameof(DocStrings.QaTouchesOther_touchingClasses))]
			IList<IReadOnlyFeatureClass> touching,
			[Doc(nameof(DocStrings.QaTouchesOther_touchedClasses))]
			IList<IReadOnlyFeatureClass> touched,
			[Doc(nameof(DocStrings.QaTouchesOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(touching, touched, esriSpatialRelEnum.esriSpatialRelTouches)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
			AddCustomQueryFilterExpression(validRelationConstraint);
		}

		[Doc(nameof(DocStrings.QaTouchesOther_3))]
		public QaTouchesOther(
			[Doc(nameof(DocStrings.QaTouchesOther_touchingClass))]
			IReadOnlyFeatureClass touching,
			[Doc(nameof(DocStrings.QaTouchesOther_touchedClass))]
			IReadOnlyFeatureClass touched,
			[Doc(nameof(DocStrings.QaTouchesOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] {touching}, new[] {touched}, validRelationConstraint) { }


		[InternallyUsedTest]
		public QaTouchesOther([NotNull] QaTouchesOtherDefinition definition)
			: this(definition.Touching.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.Touched.Cast<IReadOnlyFeatureClass>().ToList(),
				   definition.ValidRelationConstraint)
		{
			ValidTouchGeometryConstraint = definition.ValidTouchGeometryConstraint;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaTouchesOther_ValidTouchGeometryConstraint))]
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
