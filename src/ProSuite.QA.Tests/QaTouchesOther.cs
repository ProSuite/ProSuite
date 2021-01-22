using System;
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
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
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

		[Doc("QaTouchesOther_0")]
		public QaTouchesOther(
				[Doc("QaTouchesOther_touchingClasses")]
				IList<IFeatureClass> touching,
				[Doc("QaTouchesOther_touchedClasses")] IList<IFeatureClass> touched)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(touching, touched, null) { }

		[Doc("QaTouchesOther_1")]
		public QaTouchesOther(
				[Doc("QaTouchesOther_touchingClass")] IFeatureClass touching,
				[Doc("QaTouchesOther_touchedClass")] IFeatureClass touched)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(touching, touched, null) { }

		[Doc("QaTouchesOther_2")]
		public QaTouchesOther(
			[Doc("QaTouchesOther_touchingClasses")]
			IList<IFeatureClass> touching,
			[Doc("QaTouchesOther_touchedClasses")] IList<IFeatureClass> touched,
			[Doc("QaTouchesOther_validRelationConstraint")]
			string validRelationConstraint)
			: base(touching, touched, esriSpatialRelEnum.esriSpatialRelTouches)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
		}

		[Doc("QaTouchesOther_3")]
		public QaTouchesOther(
			[Doc("QaTouchesOther_touchingClass")] IFeatureClass touching,
			[Doc("QaTouchesOther_touchedClass")] IFeatureClass touched,
			[Doc("QaTouchesOther_validRelationConstraint")]
			string validRelationConstraint)
			: this(new[] {touching}, new[] {touched}, validRelationConstraint) { }

		[TestParameter]
		[Doc("QaTouchesOther_ValidTouchGeometryConstraint")]
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
