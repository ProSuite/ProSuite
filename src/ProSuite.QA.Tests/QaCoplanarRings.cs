using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.PointEnumerators;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using IPnt = ProSuite.Commons.Geometry.IPnt;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports non-linear polycurve segments as errors
	/// </summary>
	[CLSCompliant(false)]
	[UsedImplicitly]
	[GeometryTest]
	public class QaCoplanarRings : ContainerTest
	{
		private readonly double _coplanarityTolerance;
		private readonly bool _includeAssociatedParts;
		private readonly double _xyResolution;
		private readonly double _zResolution;
		private readonly esriGeometryType _shapeType;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string FaceDoesNotDefineValidPlane =
				"FaceDoesNotDefineValidPlane";

			public const string FaceNotCoplanar = "FaceNotCoplanar";

			public Code() : base("CoplanarRings") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaCoplanarRings_0))]
		public QaCoplanarRings(
			[Doc(nameof(DocStrings.QaCoplanarRings_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaCoplanarRings_coplanarityTolerance))]
			double coplanarityTolerance,
			[Doc(nameof(DocStrings.QaCoplanarRings_includeAssociatedParts))]
			bool includeAssociatedParts)
			: base((ITable) featureClass)
		{
			_coplanarityTolerance = coplanarityTolerance;
			_includeAssociatedParts = includeAssociatedParts;
			_shapeType = featureClass.ShapeType;

			var geodataset = (IGeoDataset) featureClass;
			var srt = (ISpatialReferenceResolution) geodataset.SpatialReference;
			_xyResolution = srt.XYResolution[false];
			_zResolution = srt.XYResolution[false];
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
			int errorCount = 0;
			var feature = row as IFeature;
			if (feature == null)
			{
				return errorCount;
			}

			var geometryCollection = feature.Shape as IGeometryCollection;
			if (geometryCollection == null)
			{
				return errorCount;
			}

			SegmentsPlaneProvider segmentsPlaneProvider = GetPlaneProvider(feature);
			SegmentsPlane segmentsPlane;
			while ((segmentsPlane = segmentsPlaneProvider.ReadPlane()) != null)
			{
				Plane3D plane;
				try
				{
					plane = segmentsPlane.Plane;
				}
				catch (Exception e)
				{
					errorCount += ReportInvalidPlane(
						$"Unable to determine plane: {e.Message}", segmentsPlane,
						feature);
					continue;
				}

				if (! plane.IsDefined)
				{
					errorCount += ReportInvalidPlane(
						"The segments of this face are collinear and do not define a valid plane",
						segmentsPlane, feature);
					continue;
				}

				double coplanarityTolerance = GeomUtils.AdjustCoplanarityTolerance(plane,
				                                                                   _coplanarityTolerance,
				                                                                   _zResolution,
				                                                                   _xyResolution);

				double maxOffset = -1;
				int segmentsCount = 0;
				foreach (SegmentProxy segment in segmentsPlane.Segments)
				{
					segmentsCount++;

					IPnt point = segment.GetStart(true);
					//double f = normal.X * point.X + normal.Y * point.Y +
					//           normalZ * point[2] + nf;

					// double offset = Math.Abs(f);
					double offset = plane.GetDistanceAbs(point.X, point.Y, point[2]);

					if (offset <= coplanarityTolerance)
					{
						continue;
					}

					maxOffset = Math.Max(offset, maxOffset);
				}

				if (segmentsCount < 3)
				{
					// TODO no need to check here once the plane is undefined in this case --> REMOVE
					const string description =
						"The segments of this face are collinear and do not define a valid plane";
					IGeometry errorGeometry = GetErrorGeometry(feature.Shape.GeometryType,
					                                           segmentsPlane.Segments);
					errorCount += ReportError(description, errorGeometry,
					                          Codes[Code.FaceDoesNotDefineValidPlane],
					                          null,
					                          row);
					continue;
				}

				if (maxOffset > 0)
				{
					string comparison = FormatLengthComparison(
						maxOffset, ">", _coplanarityTolerance,
						feature.Shape.SpatialReference);
					string description =
						$"Face with {segmentsCount} segments is not planar, max. offset = {comparison}";
					IGeometry errorGeometry = GetErrorGeometry(feature.Shape.GeometryType,
					                                           segmentsPlane.Segments);
					errorCount += ReportError(description, errorGeometry,
					                          Codes[Code.FaceNotCoplanar],
					                          TestUtils.GetShapeFieldName(feature),
					                          InvolvedRowUtils.GetInvolvedRows(row),
					                          new object[] {maxOffset});
				}
			}

			return errorCount;
		}

		private int ReportInvalidPlane([NotNull] string description,
		                               [NotNull] SegmentsPlane segmentsPlane,
		                               [NotNull] IFeature feature)
		{
			IGeometry errorGeometry = GetErrorGeometry(feature.Shape.GeometryType,
			                                           segmentsPlane.Segments);
			return ReportError(description, errorGeometry,
			                   Codes[Code.FaceDoesNotDefineValidPlane],
			                   null,
			                   feature);
		}

		[NotNull]
		private static IGeometry GetErrorGeometry(
			esriGeometryType geometryType,
			[NotNull] IEnumerable<SegmentProxy> segments)
		{
			return geometryType == esriGeometryType.esriGeometryMultiPatch
				       ? (IGeometry) SegmentUtils.CreateMultiPatch(segments)
				       : SegmentUtils.CreatePolygon(segments);
		}

		[NotNull]
		private SegmentsPlaneProvider GetPlaneProvider([NotNull] IFeature feature)
		{
			return SegmentsPlaneProvider.Create(feature, _shapeType,
			                                    _includeAssociatedParts);
		}
	}
}
