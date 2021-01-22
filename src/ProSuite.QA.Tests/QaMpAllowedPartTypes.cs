using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
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

		[Doc("QaMpAllowedPartTypes_0")]
		public QaMpAllowedPartTypes(
			[Doc("QaMpAllowedPartTypes_multiPatchClass")]
			IFeatureClass multiPatchClass,
			[Doc("QaMpAllowedPartTypes_allowRings")]
			bool allowRings,
			[Doc("QaMpAllowedPartTypes_allowTriangleFans")]
			bool allowTriangleFans,
			[Doc("QaMpAllowedPartTypes_allowTriangleStrips")]
			bool allowTriangleStrips,
			[Doc("QaMpAllowedPartTypes_allowTriangles")]
			bool allowTriangles) :
			base((ITable) multiPatchClass)
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

		public override bool IsQueriedTable(int tableIndex)
		{
			AssertValidInvolvedTableIndex(tableIndex);
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			var feature = row as IFeature;
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

					errorCount += ReportError(description,
					                          GetErrorGeometry(part),
					                          Codes[Code.InvalidGeometryType],
					                          TestUtils.GetShapeFieldName(row),
					                          row);
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
