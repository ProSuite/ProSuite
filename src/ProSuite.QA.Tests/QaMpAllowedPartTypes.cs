using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
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
	public class QaMpAllowedPartTypes : ContainerTest
	{
		private readonly List<esriGeometryType> _partTypes;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string InvalidGeometryType = "InvalidGeometryType";

			public Code() : base("MpAllowedPartTypes") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMpAllowedPartTypes_0))]
		public QaMpAllowedPartTypes(
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_multiPatchClass))]
			IReadOnlyFeatureClass multiPatchClass,
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_allowRings))]
			bool allowRings,
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_allowTriangleFans))]
			bool allowTriangleFans,
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_allowTriangleStrips))]
			bool allowTriangleStrips,
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_allowTriangles))]
			bool allowTriangles) :
			base(multiPatchClass)
		{
			_partTypes = new List<esriGeometryType>();
			if (allowRings)
			{
				_partTypes.Add(esriGeometryType.esriGeometryRing);
			}

			if (allowTriangleFans)
			{
				_partTypes.Add(esriGeometryType.esriGeometryTriangleFan);
			}

			if (allowTriangleStrips)
			{
				_partTypes.Add(esriGeometryType.esriGeometryTriangleStrip);
			}

			if (allowTriangles)
			{
				_partTypes.Add(esriGeometryType.esriGeometryTriangles);
			}
		}

		[InternallyUsedTest]
		public QaMpAllowedPartTypes(
			[NotNull] QaMpAllowedPartTypesDefinition definition)
			: this((IReadOnlyFeatureClass)definition.MultiPatchClass,
			       definition.AllowRings,
			       definition.AllowTriangleFans,
			       definition.AllowTriangleStrips,
			       definition.AllowTriangles)
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
			if (feature == null)
			{
				return NoError;
			}

			var shape = feature.Shape as IMultiPatch;
			if (shape == null || shape.IsEmpty)
			{
				return NoError;
			}

			var parts = shape as IGeometryCollection;
			if (parts == null)
			{
				return NoError;
			}

			int partCount = parts.GeometryCount;
			int errorCount = 0;

			for (int partIndex = 0; partIndex < partCount; partIndex++)
			{
				IGeometry part = parts.Geometry[partIndex];
				esriGeometryType type = part.GeometryType;

				bool valid = IsValidType(type);

				if (! valid)
				{
					string description = string.Format("Invalid geometry type '{0}' in multipatch",
					                                   type);

					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row),
						GetErrorGeometry(part), Codes[Code.InvalidGeometryType],
						TestUtils.GetShapeFieldName(row));
				}
			}

			return errorCount;
		}

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] IGeometry part)
		{
			IPointCollection result = new PolygonClass();

			result.AddPointCollection((IPointCollection) part);
			((IPolygon) result).Close();

			((IPolygon) result).SpatialReference = part.SpatialReference;

			return (IGeometry) result;
		}

		private bool IsValidType(esriGeometryType type)
		{
			return _partTypes.Any(validType => type == validType);
		}
	}
}
