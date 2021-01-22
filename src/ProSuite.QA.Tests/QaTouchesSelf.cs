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

		[Doc("QaTouchesSelf_0")]
		public QaTouchesSelf(
				[Doc("QaTouchesSelf_featureClasses")] IList<IFeatureClass> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc("QaTouchesSelf_1")]
		public QaTouchesSelf(
				[Doc("QaTouchesSelf_featureClass")] IFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc("QaTouchesSelf_2")]
		public QaTouchesSelf(
			[Doc("QaTouchesSelf_featureClasses")] IList<IFeatureClass> featureClasses,
			[Doc("QaTouchesSelf_validRelationConstraint")]
			string validRelationConstraint)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelTouches)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
		}

		[Doc("QaTouchesSelf_3")]
		public QaTouchesSelf(
			[Doc("QaTouchesSelf_featureClass")] IFeatureClass featureClass,
			[Doc("QaTouchesSelf_validRelationConstraint")]
			string validRelationConstraint)
			: this(new[] {featureClass}, validRelationConstraint) { }

		[TestParameter]
		[Doc("QaTouchesSelf_ValidTouchGeometryConstraint")]
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

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
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
