using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMaxVertexCount : ContainerTest
	{
		private readonly double _limit;
		private readonly bool _perPart;
		private readonly string _shapeFieldName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string TooManyVertices = "TooManyVertices";

			public Code() : base("MaxVertexCount") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMaxVertexCount_0))]
		public QaMaxVertexCount(
			[Doc(nameof(DocStrings.QaMaxVertexCount_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMaxVertexCount_limit))] double limit,
			[Doc(nameof(DocStrings.QaMaxVertexCount_perPart))] bool perPart)
			: base((ITable) featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			_limit = limit;
			_perPart = perPart;
			_shapeFieldName = featureClass.ShapeFieldName;
		}

		public override bool IsQueriedTable(int tableIndex)
		{
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

			IGeometry shape = feature.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return NoError;
			}

			var allPoints = shape as IPointCollection;
			if (allPoints == null)
			{
				return NoError;
			}

			var geometryCollection = shape as IGeometryCollection;

			if (! _perPart || geometryCollection == null ||
			    geometryCollection.GeometryCount == 1)
			{
				return CheckPoints(allPoints, row);
			}

			int errorCount = 0;

			foreach (IGeometry part in GeometryUtils.GetParts(geometryCollection))
			{
				errorCount += CheckPoints((IPointCollection) part, row);

				Marshal.ReleaseComObject(part);
			}

			return errorCount;
		}

		private int CheckPoints([NotNull] IPointCollection points,
		                        [NotNull] IRow row)
		{
			int pointCount = points.PointCount;

			if (pointCount <= _limit)
			{
				return NoError;
			}

			string description = string.Format("Too many vertices: {0:N0} > {1:N0}",
			                                   pointCount, _limit);

			return ReportError(description, GetErrorGeometry(points),
			                   Codes[Code.TooManyVertices], _shapeFieldName,
			                   row);
		}

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] IPointCollection points)
		{
			// TODO use envelope if vertex count is too big?
			// return shape.Envelope;

			var input = (IGeometry) points;

			IGeometry highlevel = GeometryUtils.GetHighLevelGeometry(input);

			IGeometry copy = highlevel == points
				                 ? GeometryFactory.Clone(input)
				                 : highlevel;

			var polycurve = copy as IPolycurve;

			if (polycurve != null)
			{
				const int maxAllowableOffsetFactor = 10;
				polycurve.Weed(maxAllowableOffsetFactor);
			}

			return copy;
		}
	}
}
