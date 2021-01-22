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

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
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

		[Doc("QaCrossesSelf_0")]
		public QaCrossesSelf(
				[Doc("QaCrossesSelf_featureClasses")] IList<IFeatureClass> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc("QaCrossesSelf_1")]
		public QaCrossesSelf(
				[Doc("QaCrossesSelf_featureClass")] IFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc("QaCrossesSelf_2")]
		public QaCrossesSelf(
			[Doc("QaCrossesSelf_featureClasses")] IList<IFeatureClass> featureClasses,
			[Doc("QaCrossesSelf_validRelationConstraint")]
			string validRelationConstraint)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelCrosses)
		{
			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
		}

		[Doc("QaCrossesSelf_3")]
		public QaCrossesSelf(
			[Doc("QaCrossesSelf_featureClass")] IFeatureClass featureClass,
			[Doc("QaCrossesSelf_validRelationConstraint")]
			string validRelationConstraint)
			: this(new[] {featureClass}, validRelationConstraint) { }

		#region Overrides of QaSpatialRelationSelfBase

		protected override int FindErrors(IRow row1, int tableIndex1, IRow row2,
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
