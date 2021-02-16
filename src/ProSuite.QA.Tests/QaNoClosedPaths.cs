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
	public class QaNoClosedPaths : ContainerTest
	{
		private readonly string _shapeFieldName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ClosedPath = "ClosedPath";

			public Code() : base("NoClosedPaths") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaNoClosedPaths_0))]
		public QaNoClosedPaths(
			[Doc(nameof(DocStrings.QaNoClosedPaths_polylineClass))] [NotNull]
			IFeatureClass polyLineClass)
			: base((ITable) polyLineClass)
		{
			Assert.ArgumentNotNull(polyLineClass, nameof(polyLineClass));
			Assert.ArgumentCondition(
				polyLineClass.ShapeType == esriGeometryType.esriGeometryPolyline,
				"polyline feature class expected");

			_shapeFieldName = polyLineClass.ShapeFieldName;
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

			var polyline = feature.Shape as IPolyline;
			if (polyline == null || polyline.IsEmpty)
			{
				return NoError;
			}

			var geometryCollection = (IGeometryCollection) polyline;

			if (geometryCollection.GeometryCount <= 1)
			{
				return CheckCurve(polyline, row);
			}

			// more than one part:
			int errorCount = 0;

			foreach (IPath path in GeometryUtils.GetPaths(polyline))
			{
				errorCount += CheckCurve(path, row);

				Marshal.ReleaseComObject(path);
			}

			return errorCount;
		}

		private int CheckCurve([NotNull] ICurve curve, [NotNull] IRow row)
		{
			return ! curve.IsEmpty && curve.IsClosed
				       ? ReportError("Closed path", curve.FromPoint,
				                     Codes[Code.ClosedPath], _shapeFieldName,
				                     row)
				       : NoError;
		}
	}
}
