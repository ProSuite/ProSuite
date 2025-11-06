using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
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
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaIntersectsSelf : QaSpatialRelationSelfBase
	{
		[CanBeNull] private readonly string _validRelationConstraintSql;
		[CanBeNull] private IValidRelationConstraint _validRelationConstraint;
		[CanBeNull] private GeometryConstraint _validIntersectionGeometryConstraint;
		private const bool _defaultReportIntersectionsAsMultipart = true;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;
		[NotNull] private IList<GeometryComponent> _geometryComponents;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new IntersectsIssueCodes());

		#endregion

		[Doc(nameof(DocStrings.QaIntersectsSelf_0))]
		public QaIntersectsSelf(
				[Doc(nameof(DocStrings.QaIntersectsSelf_featureClass))]
				IReadOnlyFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc(nameof(DocStrings.QaIntersectsSelf_1))]
		public QaIntersectsSelf(
				[Doc(nameof(DocStrings.QaIntersectsSelf_featureClasses))]
				IList<IReadOnlyFeatureClass> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaIntersectsSelf_2))]
		public QaIntersectsSelf(
			[Doc(nameof(DocStrings.QaIntersectsSelf_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaIntersectsSelf_validRelationConstraint))]
			string validRelationConstraint)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			Assert.ArgumentCondition(featureClasses.Count > 0, "empty featureClasses");

			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
			AddCustomQueryFilterExpression(validRelationConstraint);
			_geometryComponents = new ReadOnlyList<GeometryComponent>(
				featureClasses.Select(_ => GeometryComponent.EntireGeometry)
				              .ToList());
		}

		[Doc(nameof(DocStrings.QaIntersectsSelf_3))]
		public QaIntersectsSelf(
			[Doc(nameof(DocStrings.QaIntersectsSelf_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaIntersectsSelf_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { featureClass }, validRelationConstraint) { }

		[InternallyUsedTest]
		public QaIntersectsSelf(QaIntersectsSelfDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.ValidRelationConstraint)
		{
			ReportIntersectionsAsMultipart = definition.ReportIntersectionsAsMultipart;
			ValidIntersectionGeometryConstraint = definition.ValidIntersectionGeometryConstraint;
			GeometryComponents = definition.GeometryComponents;
		}

		#region Overrides of QaSpatialRelationSelfBase

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			if (_validRelationConstraint == null)
			{
				const bool constraintIsDirected = false;
				_validRelationConstraint =
					new ValidRelationConstraint(_validRelationConstraintSql,
					                            constraintIsDirected,
					                            GetSqlCaseSensitivity());
			}

			return QaSpatialRelationUtils.ReportIntersections(
				row1, tableIndex1,
				row2, tableIndex2,
				this, GetIssueCode(),
				_validRelationConstraint,
				! ReportIntersectionsAsMultipart,
				_validIntersectionGeometryConstraint,
				GetGeometryComponent(tableIndex1),
				GetGeometryComponent(tableIndex2));
		}

		#endregion

		[TestParameter(_defaultReportIntersectionsAsMultipart)]
		[Doc(nameof(DocStrings.QaIntersectsSelf_ReportIntersectionsAsMultipart))]
		public bool ReportIntersectionsAsMultipart { get; set; } =
			_defaultReportIntersectionsAsMultipart;

		[TestParameter]
		[Doc(nameof(DocStrings.QaIntersectsSelf_ValidIntersectionGeometryConstraint))]
		public string ValidIntersectionGeometryConstraint
		{
			get { return _validIntersectionGeometryConstraint?.Constraint; }
			set
			{
				_validIntersectionGeometryConstraint =
					StringUtils.IsNullOrEmptyOrBlank(value)
						? null
						: new GeometryConstraint(value);
			}
		}

		[Doc(nameof(DocStrings.QaIntersectsSelf_GeometryComponents))]
		[TestParameter]
		public IList<GeometryComponent> GeometryComponents
		{
			get { return _geometryComponents; }
			set
			{
				Assert.ArgumentCondition(value == null ||
				                         value.Count == 1 ||
				                         value.Count == InvolvedTables.Count,
				                         "unexpected number of geometry components " +
				                         "(must be 0, 1, or equal to the number of feature classes");

				_geometryComponents =
					new ReadOnlyList<GeometryComponent>(
						value?.ToList() ?? InvolvedTables
						                   .Select(_ => GeometryComponent.EntireGeometry)
						                   .ToList());
			}
		}

		[CanBeNull]
		private IssueCode GetIssueCode()
		{
			return _validRelationConstraintSql != null ||
			       _validIntersectionGeometryConstraint != null
				       ? Codes[IntersectsIssueCodes.GeometriesIntersect]
				       : Codes[
					       IntersectsIssueCodes
						       .GeometriesIntersect_ConstraintNotFulfilled];
		}

		private GeometryComponent GetGeometryComponent(int tableIndex)
		{
			return _geometryComponents.Count == 1
				       ? _geometryComponents[0]
				       : _geometryComponents[tableIndex];
		}
	}
}
