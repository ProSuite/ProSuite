using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpNonIntersectingRingFootprints : ContainerTest
	{
		private readonly bool _allowIntersectionsForDifferentPointIds;
		[ThreadStatic] private static IPoint _queryPoint;
		private const double _defaultResolutionFactor = 1;
		private double _resolutionFactor = _defaultResolutionFactor;

		[CanBeNull] private ISpatialReference _reducedResolutionSpatialReference;
		[CanBeNull] private ISpatialReference _minimumToleranceSpatialReference;

		[NotNull] private readonly ISpatialReference _spatialReference;
		[NotNull] private readonly string _shapeFieldName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string RingFootprintsIntersect = "RingFootprintsIntersect";
			public const string PointIdNotUniqueWithinFace = "PointIdNotUniqueWithinFace";

			public Code() : base("MpNonIntersectingRingFootprints") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMpNonIntersectingRingFootprints_0))]
		public QaMpNonIntersectingRingFootprints(
			[Doc(nameof(DocStrings.QaMpNonIntersectingRingFootprints_multiPatchClass))] [NotNull]
			IReadOnlyFeatureClass multiPatchClass,
			[Doc(nameof(DocStrings
				            .QaMpNonIntersectingRingFootprints_allowIntersectionsForDifferentPointIds))]
			bool allowIntersectionsForDifferentPointIds)
			: base(multiPatchClass)
		{
			Assert.ArgumentNotNull(multiPatchClass, nameof(multiPatchClass));

			_allowIntersectionsForDifferentPointIds = allowIntersectionsForDifferentPointIds;
			_spatialReference = multiPatchClass.SpatialReference;
			_shapeFieldName = multiPatchClass.ShapeFieldName;
		}

		[InternallyUsedTest]
		public QaMpNonIntersectingRingFootprints(
			[NotNull] QaMpNonIntersectingRingFootprintsDefinition definition)
			: this((IReadOnlyFeatureClass) definition.MultiPatchClass,
			       definition.AllowIntersectionsForDifferentPointIds)
		{
			ResolutionFactor = definition.ResolutionFactor;
		}

		[UsedImplicitly]
		[TestParameter(_defaultResolutionFactor)]
		[Doc(nameof(DocStrings.QaMpNonIntersectingRingFootprints_ResolutionFactor))]
		public double ResolutionFactor
		{
			get { return _resolutionFactor; }
			set
			{
				Assert.ArgumentCondition(value >= 1, "value must be >= 1");

				_resolutionFactor = value;
			}
		}

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

			var multiPatch = feature?.Shape as IMultiPatch;
			if (multiPatch == null)
			{
				return NoError;
			}

			if (_reducedResolutionSpatialReference == null && _resolutionFactor > 1)
			{
				_reducedResolutionSpatialReference =
					SpatialReferenceUtils.CreateSpatialReferenceWithMinimumTolerance(
						_spatialReference, _resolutionFactor);

				// for eliminating vertical walls, don't use the resolution factor, 
				// but the original resolution with the minimum tolerance (2x) for it
				_minimumToleranceSpatialReference =
					SpatialReferenceUtils.CreateSpatialReferenceWithMinimumTolerance(
						_spatialReference);
			}

			return CheckRings(multiPatch, feature);
		}

		private static IPoint QueryPoint => _queryPoint ?? (_queryPoint = new PointClass());

		private int CheckRings([NotNull] IMultiPatch multiPatch, [NotNull] IReadOnlyFeature feature)
		{
			int errorCount;
			IEnumerable<KeyValuePair<int, List<IPolygon>>> ringPolygonGroups =
				GetRingPolygonGroups(multiPatch, feature, out errorCount);

			foreach (KeyValuePair<int, List<IPolygon>> pair in ringPolygonGroups)
			{
				List<IPolygon> ringPolygons = pair.Value;

				IList<IPolygon> intersections = GetInteriorIntersections(ringPolygons);

				switch (intersections.Count)
				{
					case 0:
						continue;

					case 1:
						errorCount += ReportErrors(feature, intersections[0], pair.Key);
						break;

					default:
						var union = (IPolygon) GeometryUtils.Union(intersections);
						errorCount += ReportErrors(feature, union, pair.Key);
						break;
				}
			}

			return errorCount;
		}

		private int ReportErrors([NotNull] IReadOnlyFeature feature,
		                         [NotNull] IPolygon intersection,
		                         int id)
		{
			if (intersection.ExteriorRingCount < 2)
			{
				return ReportError(feature, intersection, id);
			}

			var errorCount = 0;

			foreach (IGeometry geometry in GeometryUtils.Explode(intersection))
			{
				errorCount += ReportError(feature, (IPolygon) geometry, id);
			}

			return errorCount;
		}

		private int ReportError([NotNull] IReadOnlyFeature feature,
		                        [NotNull] IPolygon intersection,
		                        int id)
		{
			double area = ((IArea) intersection).Area;

			string description;
			if (_allowIntersectionsForDifferentPointIds)
			{
				description =
					string.Format(
						"Footprints of rings with point id {0} intersect (intersection area: {1})",
						id, FormatArea(area, _spatialReference));
			}
			else
			{
				description =
					string.Format("Footprints of rings intersect (intersection area: {0})",
					              FormatArea(area, _spatialReference));
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), intersection,
				Codes[Code.RingFootprintsIntersect], _shapeFieldName);
		}

		[NotNull]
		private static IList<IPolygon> GetInteriorIntersections(
			[NotNull] ICollection<IPolygon> ringPolygons)
		{
			if (ringPolygons.Count == 1)
			{
				return new List<IPolygon>();
			}

			// sort the ring polygons ascending on the point count
			// -> check/union the least complex rings first (assumed to reduce overall complexity)
			List<PolygonWithPointCount> sortedPolygons = ringPolygons.Select(
					polygon =>
						new
							PolygonWithPointCount
							(polygon))
				.ToList();
			sortedPolygons.Sort((p1, p2) => p1.PointCount.CompareTo(p2.PointCount));

			var intersections = new List<IPolygon>();

			IPolygon union = null;
			foreach (PolygonWithPointCount ringPolygonWithCount in sortedPolygons)
			{
				IPolygon ringPolygon = ringPolygonWithCount.Polygon;

				if (union == null)
				{
					union = ringPolygon;
					continue;
				}

				if (! ((IRelationalOperator) union).Disjoint(ringPolygon))
				{
					var intersection = (IPolygon) ((ITopologicalOperator) union).Intersect(
						ringPolygon, esriGeometryDimension.esriGeometry2Dimension);

					if (intersection != null && ! intersection.IsEmpty)
					{
						// there may be self-intersections in the Intersect() result --> simplify
						Simplify(intersection);

						if (! intersection.IsEmpty)
						{
							intersections.Add(intersection);
						}
					}
				}

				union = (IPolygon) ((ITopologicalOperator) union).Union(ringPolygon);
			}

			return intersections;
		}

		[NotNull]
		private IEnumerable<KeyValuePair<int, List<IPolygon>>> GetRingPolygonGroups(
			[NotNull] IMultiPatch multiPatch,
			[NotNull] IReadOnlyFeature feature,
			out int errorCount)
		{
			var result = new Dictionary<int, List<IPolygon>>();

			errorCount = 0;

			foreach (IPolygon polygon in GetRingsAsPolygons(multiPatch))
			{
				int groupId;
				if (! _allowIntersectionsForDifferentPointIds)
				{
					groupId = 0;
				}
				else
				{
					IList<int> pointIds = GetPointIds(polygon);

					switch (pointIds.Count)
					{
						case 0:
							continue;

						case 1:
							groupId = pointIds[0];
							break;

						default:
						{
							string description = string.Format(
								"Point ids are not unique within face. Point ids: {0}",
								StringUtils.ConcatenateSorted(pointIds, ","));

							errorCount += ReportError(
								description, InvolvedRowUtils.GetInvolvedRows(feature), polygon,
								Codes[Code.PointIdNotUniqueWithinFace], _shapeFieldName);
							continue;
						}
					}
				}

				IPolygon simplifiedPolygon = GetSimplifiedPolygon(polygon);

				if (simplifiedPolygon != null)
				{
					List<IPolygon> polygonGroup;
					if (! result.TryGetValue(groupId, out polygonGroup))
					{
						polygonGroup = new List<IPolygon>();
						result.Add(groupId, polygonGroup);
					}

					polygonGroup.Add(simplifiedPolygon);
				}
			}

			return result;
		}

		[CanBeNull]
		private IPolygon GetSimplifiedPolygon([NotNull] IPolygon polygon)
		{
			IPolygon polygonCopy = _reducedResolutionSpatialReference != null
				                       ? GeometryFactory.Clone(polygon)
				                       : null;

			if (_minimumToleranceSpatialReference != null)
			{
				polygon.SpatialReference = _minimumToleranceSpatialReference;
			}

			// simplify first with the original tolerance, to get rid of vertical walls
			Simplify(polygon);

			if (polygon.IsEmpty)
			{
				// the polygon has become empty by the simplify (vertical or too small or invalid)
				// -> ignore
				return null;
			}

			if (_reducedResolutionSpatialReference == null)
			{
				// return the simplified polygon
				return polygon;
			}

			// simplify the original, with the minimum spatial reference
			Assert.NotNull(polygonCopy);

			polygonCopy.SpatialReference = _reducedResolutionSpatialReference;
			Simplify(polygonCopy);

			return polygonCopy;
		}

		private static void Simplify([NotNull] IPolygon polygon)
		{
			const bool allowReorder = true;
			const bool allowPathSplitAtIntersections = false;
			GeometryUtils.Simplify(polygon, allowReorder, allowPathSplitAtIntersections);
		}

		[NotNull]
		private static IList<int> GetPointIds([NotNull] IPolygon polygon)
		{
			var pointIds = new SimpleSet<int>();

			var points = (IPointCollection) polygon;

			int pointCount = points.PointCount;

			IPoint point = QueryPoint;

			for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
			{
				points.QueryPoint(pointIndex, point);

				pointIds.TryAdd(point.ID);
			}

			var result = new List<int>(pointIds);

			result.Sort();

			return result;
		}

		[NotNull]
		private static IEnumerable<IPolygon> GetRingsAsPolygons(
			[NotNull] IMultiPatch multiPatch)
		{
			var parts = (IGeometryCollection) multiPatch;

			var result = new List<IPolygon>(parts.GeometryCount);

			foreach (IGeometry part in GeometryUtils.GetParts(parts))
			{
				var ring = part as IRing;
				if (ring == null)
				{
					continue;
				}

				var isBeginningRing = false;
				multiPatch.GetRingType(ring, ref isBeginningRing);

				if (isBeginningRing)
				{
					IPolygon ringPolygon = MultiPatchUtils.GetFace(multiPatch, ring);

					if (! ringPolygon.IsEmpty)
					{
						result.Add(ringPolygon);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Helper for sorting polygons on point count (making sure that point count is accessed only once per polygon)
		/// </summary>
		private class PolygonWithPointCount
		{
			public PolygonWithPointCount([NotNull] IPolygon polygon)
			{
				Polygon = polygon;
				PointCount = ((IPointCollection) polygon).PointCount;
			}

			[NotNull]
			public IPolygon Polygon { get; }

			public int PointCount { get; }
		}
	}
}
