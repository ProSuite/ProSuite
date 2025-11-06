using System.Globalization;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaGeometryConstraint : ContainerTest
	{
		private readonly bool _perPart;
		[NotNull] private readonly GeometryConstraint _constraint;
		[NotNull] private readonly string _shapeFieldName;
		private readonly esriGeometryType _geometryType;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ConstraintNotFulfilled_ForShape =
				"ConstraintNotFulfilled.ForShape";

			public const string ConstraintNotFulfilled_ForShapePart =
				"ConstraintNotFulfilled.ForShapePart";

			public Code() : base("GeometryConstraint") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaGeometryConstraint_0))]
		public QaGeometryConstraint(
			[Doc(nameof(DocStrings.QaGeometryConstraint_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaGeometryConstraint_geometryConstraint))] [NotNull]
			string
				geometryConstraint,
			[Doc(nameof(DocStrings.QaGeometryConstraint_perPart))]
			bool perPart)
			: base(featureClass)
		{
			Assert.ArgumentNotNullOrEmpty(geometryConstraint, nameof(geometryConstraint));

			_constraint = new GeometryConstraint(geometryConstraint);
			_shapeFieldName = featureClass.ShapeFieldName;
			_perPart = perPart;
			_geometryType = featureClass.ShapeType;
		}

		[InternallyUsedTest]
		public QaGeometryConstraint(
			[NotNull] QaGeometryConstraintDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       definition.GeometryConstraint, definition.PerPart) { }

		public override bool IsQueriedTable(int tableIndex)
		{
			AssertValidInvolvedTableIndex(tableIndex);
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = (IReadOnlyFeature) row;

			return _perPart
				       ? CheckPerPart(feature)
				       : CheckShape(feature);
		}

		private int CheckPerPart([NotNull] IReadOnlyFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			IGeometry shape = feature.Shape;

			if (_geometryType == esriGeometryType.esriGeometryMultipoint && shape != null)
			{
				// check individual points
				return CheckPoints(feature, (IPointCollection) shape);
			}

			var parts = shape as IGeometryCollection;
			if (parts != null && parts.GeometryCount > 1)
			{
				// there is more than one part
				return CheckParts(feature, parts);
			}

			// geometry is single-part or null
			return CheckShape(feature);
		}

		private int CheckParts([NotNull] IReadOnlyFeature feature,
		                       [NotNull] IGeometryCollection parts)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));
			Assert.ArgumentNotNull(parts, nameof(parts));

			var errorCount = 0;

			foreach (IGeometry part in GeometryUtils.GetParts(parts))
			{
				if (! _constraint.IsFulfilled(part))
				{
					errorCount += ReportError(feature,
					                          part,
					                          isPart: true);
				}
			}

			return errorCount;
		}

		private int CheckPoints([NotNull] IReadOnlyFeature feature,
		                        [NotNull] IPointCollection points)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));
			Assert.ArgumentNotNull(points, nameof(points));

			var errorCount = 0;
			foreach (IPoint point in GeometryUtils.GetPoints(points, recycle: true))
			{
				if (! _constraint.IsFulfilled(point))
				{
					errorCount += ReportError(feature, GeometryFactory.Clone(point), isPart: true);
				}
			}

			return errorCount;
		}

		private int CheckShape([NotNull] IReadOnlyFeature feature)
		{
			return _constraint.IsFulfilled(feature.Shape)
				       ? NoError
				       : ReportError(feature, feature.Shape, isPart: false);
		}

		private int ReportError([NotNull] IReadOnlyFeature feature,
		                        [NotNull] IGeometry geometry,
		                        bool isPart)
		{
			string displayValues = _constraint.FormatValues(geometry,
			                                                CultureInfo.CurrentCulture)
			                                  .Replace("$", string.Empty);

			string rawValues = _constraint.FormatValues(geometry, CultureInfo.InvariantCulture);

			string description = GetErrorDescription(
				_constraint.Constraint.Replace("$", string.Empty),
				displayValues,
				isPart);

			IGeometry errorGeometry;
			IssueCode issueCode;
			if (isPart)
			{
				errorGeometry = GeometryUtils.GetHighLevelGeometry(geometry);
				issueCode = Codes[Code.ConstraintNotFulfilled_ForShapePart];
			}
			else
			{
				errorGeometry = geometry;
				issueCode = Codes[Code.ConstraintNotFulfilled_ForShape];
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), errorGeometry,
				issueCode, _shapeFieldName, values: new[] { rawValues });
		}

		[NotNull]
		private static string GetErrorDescription(
			[NotNull] string geometryConstraint,
			[NotNull] string constraintValues,
			bool perPart)
		{
			return string.Format(
				perPart
					? LocalizableStrings.QaGeometryConstraint_ConstraintNotFulfilled_ForShapePart
					: LocalizableStrings
						.QaGeometryConstraint_QaGeometryConstraint_ConstraintNotFulfilled_ForShape,
				geometryConstraint,
				constraintValues);
		}
	}
}
