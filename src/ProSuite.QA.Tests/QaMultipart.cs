using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMultipart : ContainerTest
	{
		private readonly bool _singleRing;
		private readonly string _shapeFieldName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string MultipleParts = "MultipleParts";
			public const string MultipleExteriorRings = "MultipleExteriorRings";

			public Code() : base("Multipart") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMultipart_0))]
		public QaMultipart(
				[Doc(nameof(DocStrings.QaMultipart_featureClass))]
				IReadOnlyFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, singleRing: false) { }

		[Doc(nameof(DocStrings.QaMultipart_0))]
		public QaMultipart(
			[Doc(nameof(DocStrings.QaMultipart_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMultipart_singleRing))]
			bool singleRing)
			: base(featureClass)
		{
			_singleRing = singleRing;
			_shapeFieldName = featureClass.ShapeFieldName;
		}

		[InternallyUsedTest]
		public QaMultipart(
			[NotNull] QaMultipartDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClass,
			       definition.SingleRing)
		{ }

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
			var feature = row as IReadOnlyFeature;

			IGeometry shape = feature?.Shape;
			if (shape == null || shape.IsEmpty)
			{
				return NoError;
			}

			if (shape.GeometryType == esriGeometryType.esriGeometryPolyline || _singleRing)
			{
				if (! (shape is IGeometryCollection geometryCollection))
				{
					return NoError;
				}

				int partCount = geometryCollection.GeometryCount;

				if (partCount > 1)
				{
					string description = $"Geometry has {partCount} parts, allowed is 1";
					return ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row), shape,
						Codes[Code.MultipleParts], _shapeFieldName);
				}
			}
			else if (shape.GeometryType == esriGeometryType.esriGeometryPolygon)
			{
				int exteriorRingCount = GeometryUtils.GetExteriorRingCount(
					(IPolygon) shape,
					allowSimplify: false);

				if (exteriorRingCount > 1)
				{
					string description =
						$"Polygon has {exteriorRingCount} exterior rings, allowed is 1";
					return ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row), shape,
						Codes[Code.MultipleExteriorRings], _shapeFieldName);
				}
			}

			return NoError;
		}
	}
}
