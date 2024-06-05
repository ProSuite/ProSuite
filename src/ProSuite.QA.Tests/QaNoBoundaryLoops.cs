using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
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
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaNoBoundaryLoops : ContainerTest
	{
		private readonly BoundaryLoopErrorGeometry _errorGeometry;
		private readonly BoundaryLoopAreaRelation _areaRelation;
		private readonly double _areaLimit;
		private readonly double _xyTolerance;
		private readonly ISpatialReference _spatialReference;
		private readonly string _shapeFieldName;
		private readonly esriGeometryType _shapeType;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string BoundaryLoop = "BoundaryLoop";

			public const string BoundaryLoop_AreaLargerThanLimit =
				"BoundaryLoop.AreaLargerThanLimit";

			public const string BoundaryLoop_AreaSmallerThanLimit =
				"BoundaryLoop.AreaSmallerThanLimit";

			public const string BoundaryLoop_UnableToConstructLoopPolygon =
				"BoundaryLoop.UnableToConstructLoopPolygon";

			public Code() : base("BoundaryLoops") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaNoBoundaryLoops_0))]
		public QaNoBoundaryLoops(
				[Doc(nameof(DocStrings.QaNoBoundaryLoops_polygonClass))] [NotNull]
				IReadOnlyFeatureClass polygonClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, BoundaryLoopErrorGeometry.LoopPolygon) { }

		[Doc(nameof(DocStrings.QaNoBoundaryLoops_1))]
		public QaNoBoundaryLoops(
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_polygonClass))] [NotNull]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_errorGeometry))]
			BoundaryLoopErrorGeometry errorGeometry)
			: this(
				// ReSharper disable once IntroduceOptionalParameters.Global
				polygonClass, errorGeometry, BoundaryLoopAreaRelation.IgnoreSmallerOrEqual, 0) { }

		[Doc(nameof(DocStrings.QaNoBoundaryLoops_2))]
		public QaNoBoundaryLoops(
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_polygonClass))] [NotNull]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_errorGeometry))]
			BoundaryLoopErrorGeometry errorGeometry,
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_areaRelation))]
			BoundaryLoopAreaRelation areaRelation,
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_areaLimit))]
			double areaLimit)
			: base(polygonClass)
		{
			Assert.ArgumentNotNull(polygonClass, nameof(polygonClass));
			Assert.ArgumentCondition(
				polygonClass.ShapeType == esriGeometryType.esriGeometryPolygon ||
				polygonClass.ShapeType == esriGeometryType.esriGeometryMultiPatch,
				"polygon or multipatch feature class expected");

			_errorGeometry = errorGeometry;
			_areaRelation = areaRelation;
			_areaLimit = areaLimit;
			_shapeFieldName = polygonClass.ShapeFieldName;
			_shapeType = polygonClass.ShapeType;

			_spatialReference = polygonClass.SpatialReference;
			double xyTolerance =
				SpatialReferenceUtils.GetXyResolution(polygonClass.SpatialReference);
			_xyTolerance = xyTolerance;
		}

		[InternallyUsedTest]
		public QaNoBoundaryLoops([NotNull] QaNoBoundaryLoopsDefinition definition)
			: this((IReadOnlyFeatureClass) definition.PolygonClass, definition.ErrorGeometry,
			       definition.AreaRelation, definition.AreaLimit) { }

		public override bool IsQueriedTable(int tableIndex)
		{
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

			switch (_shapeType)
			{
				case esriGeometryType.esriGeometryPolygon:
					return CheckPolygon(feature);

				case esriGeometryType.esriGeometryMultiPatch:
					return CheckMultiPatch(feature);

				default:
					return NoError;
			}
		}

		private int CheckMultiPatch([NotNull] IReadOnlyFeature feature)
		{
			var multiPatch = (IMultiPatch) feature.Shape;

			var parts = (IGeometryCollection) multiPatch;
			int partCount = parts.GeometryCount;

			int errorCount = 0;
			for (int i = 0; i < partCount; i++)
			{
				IGeometry part = parts.Geometry[i];

				var ring = part as IRing;
				if (ring == null)
				{
					continue;
				}

				errorCount += CheckGeometry3D(ring, feature);

				Marshal.ReleaseComObject(ring);
			}

			return errorCount;
		}

		private int CheckPolygon([NotNull] IReadOnlyFeature feature)
		{
			var polygon = (IPolygon) feature.Shape;

			if (GeometryUtils.GetPartCount(polygon) == 1)
			{
				// process single-part polygons directly, to avoid
				// unpacking the rings for *all* polygons (for the
				// polygons having loops unpacking will be required)
				return CheckGeometry2D(polygon, feature);
			}

			// multi-part polygon, process each ring separately
			int errorCount = 0;

			foreach (IRing ring in GeometryUtils.GetRings(polygon))
			{
				errorCount += CheckGeometry2D(ring, feature);

				Marshal.ReleaseComObject(ring);
			}

			return errorCount;
		}

		private int CheckGeometry2D([NotNull] IGeometry geometry,
		                            [NotNull] IReadOnlyRow row)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(row, nameof(row));

			return CheckLocations(geometry, row, GetLocations2D(geometry),
			                      new LocationComparer(_xyTolerance));
		}

		private int CheckGeometry3D([NotNull] IGeometry geometry,
		                            [NotNull] IReadOnlyRow row)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(row, nameof(row));

			return CheckLocations(geometry, row, GetLocations3D(geometry),
			                      new Location3DComparer(_xyTolerance));
		}

		private int CheckLocations<T>([NotNull] IGeometry geometry,
		                              [NotNull] IReadOnlyRow row,
		                              [NotNull] IEnumerable<T> locations,
		                              [NotNull] IEqualityComparer<T> comparer)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentNotNull(locations, nameof(locations));
			Assert.ArgumentNotNull(comparer, nameof(comparer));
			var points = geometry as IPointCollection4;
			Assert.ArgumentCondition(points != null, "Unexpected geometry type");

			int pointCount = points.PointCount;

			var vertexLocations = new Dictionary<T, int>(pointCount, comparer);

			int errorCount = 0;

			int pointIndex = 0;
			foreach (T location in locations)
			{
				int existingIndex;
				if (vertexLocations.TryGetValue(location, out existingIndex))
				{
					// ignore expected match of last point to first point
					if (existingIndex != 0 || pointIndex != pointCount - 1)
					{
						// ignore matches of consecutive vertices: these should be caught by QaSimpleGeometry
						if (pointIndex - existingIndex > 1)
						{
							errorCount += CheckLoop(geometry, existingIndex, pointIndex, row);
						}
					}
				}
				else
				{
					vertexLocations.Add(location, pointIndex);
				}

				pointIndex++;
			}

			return errorCount;
		}

		private int CheckLoop([NotNull] IGeometry geometry,
		                      int startVertexIndex,
		                      int endVertexIndex,
		                      [NotNull] IReadOnlyRow row)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(row, nameof(row));

			const string baseMessage = "Boundary loop";
			const string baseMessageStartPoint = "Boundary loop (start point)";

			string noPolygonReason = string.Empty;
			IPolygon loopPolygon = null;
			if (_errorGeometry == BoundaryLoopErrorGeometry.LoopPolygon || _areaLimit > 0)
			{
				// the loop polygon will be needed, try to create it
				loopPolygon = BoundaryLoopUtils.TryCreateLoopPolygon(geometry, startVertexIndex,
					endVertexIndex,
					out noPolygonReason);
			}

			if (_areaLimit > 0)
			{
				return CheckLoopArea(geometry, startVertexIndex, loopPolygon,
				                     noPolygonReason, row,
				                     baseMessage, baseMessageStartPoint);
			}

			// no area check applied:

			if (_errorGeometry == BoundaryLoopErrorGeometry.LoopStartPoint)
			{
				return ReportError(
					baseMessage, InvolvedRowUtils.GetInvolvedRows(row),
					GetPoint(geometry, startVertexIndex),
					Codes[Code.BoundaryLoop], _shapeFieldName);
			}

			// polygon is requested as error geometry
			if (loopPolygon == null)
			{
				// ... but the polygon could not be created
				string description = string.Format("{0} - unable to determine loop polygon: {1}",
				                                   baseMessageStartPoint, noPolygonReason);
				return ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row),
					GetPoint(geometry, startVertexIndex),
					Codes[Code.BoundaryLoop_UnableToConstructLoopPolygon],
					_shapeFieldName);
			}

			return ReportError(
				baseMessage, InvolvedRowUtils.GetInvolvedRows(row), loopPolygon,
				Codes[Code.BoundaryLoop], _shapeFieldName);
		}

		private int CheckLoopArea([NotNull] IGeometry geometry,
		                          int startVertexIndex,
		                          [CanBeNull] IPolygon loopPolygon,
		                          [NotNull] string noPolygonReason,
		                          [NotNull] IReadOnlyRow row,
		                          [NotNull] string baseMessage,
		                          [NotNull] string baseMessageStartPoint)
		{
			string description;
			if (loopPolygon == null)
			{
				description =
					string.Format("{0} - unable to determine loop polygon for area comparison: {1}",
					              baseMessageStartPoint, noPolygonReason);
				return ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row),
					GetPoint(geometry, startVertexIndex),
					Codes[Code.BoundaryLoop_UnableToConstructLoopPolygon],
					_shapeFieldName);
			}

			double loopArea = ((IArea) loopPolygon).Area;

			if (IsIgnored(loopArea))
			{
				// no longer needed, release to avoid VM impact
				Marshal.ReleaseComObject(loopPolygon);

				return NoError;
			}

			string comparison = FormatAreaComparison(loopArea, GetRelationString(),
			                                         _areaLimit, _spatialReference);

			IssueCode issueCode = GetAreaRelationIssueCode(_areaRelation);

			if (_errorGeometry == BoundaryLoopErrorGeometry.LoopPolygon)
			{
				description = string.Format("{0}: {1}", baseMessage, comparison);
				return ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row), loopPolygon,
					issueCode, _shapeFieldName);
			}

			// report start point only

			// loop polygon was created for area check, but is not reported -> release to avoid VM impact
			Marshal.ReleaseComObject(loopPolygon);

			description = string.Format("{0}: {1}", baseMessageStartPoint, comparison);
			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				GetPoint(geometry, startVertexIndex),
				issueCode, _shapeFieldName);
		}

		private static IssueCode GetAreaRelationIssueCode(
			BoundaryLoopAreaRelation areaRelation)
		{
			switch (areaRelation)
			{
				case BoundaryLoopAreaRelation.IgnoreSmallerOrEqual:
					return Codes[Code.BoundaryLoop_AreaLargerThanLimit];

				case BoundaryLoopAreaRelation.IgnoreLarger:
					return Codes[Code.BoundaryLoop_AreaSmallerThanLimit];

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[NotNull]
		private string GetRelationString()
		{
			switch (_areaRelation)
			{
				case BoundaryLoopAreaRelation.IgnoreLarger:
					return "<=";

				case BoundaryLoopAreaRelation.IgnoreSmallerOrEqual:
					return ">";

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private bool IsIgnored(double loopArea)
		{
			switch (_areaRelation)
			{
				case BoundaryLoopAreaRelation.IgnoreLarger:
					return loopArea > _areaLimit;

				case BoundaryLoopAreaRelation.IgnoreSmallerOrEqual:
					return loopArea <= _areaLimit;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[NotNull]
		private static IGeometry GetPoint([NotNull] IGeometry geometry, int firstVertexIndex)
		{
			var points = (IPointCollection) geometry;

			return points.Point[firstVertexIndex];
		}

		[NotNull]
		private static IEnumerable<Location> GetLocations2D([NotNull] IGeometry geometry)
		{
			WKSPoint[] pointArray = GeometryUtils.GetWKSPoints(geometry);

			return pointArray.Select(point => new Location(point.X, point.Y));
		}

		[NotNull]
		private static IEnumerable<Location3D> GetLocations3D([NotNull] IGeometry geometry)
		{
			WKSPointZ[] pointArray = GeometryUtils.GetWKSPointZs(geometry);

			return pointArray.Select(point => new Location3D(point.X, point.Y, point.Z));
		}
	}
}
