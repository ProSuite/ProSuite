using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Tests
{
	public class KnownGaps
	{
		private readonly double _maxGapArea;
		private readonly double _tolerance;
		private readonly Box _allBox;

		[NotNull] private readonly List<IPolygon> _uncompletedGapPolygons =
			new List<IPolygon>();

		[NotNull] private readonly List<IPolyline> _largeGapCuttingEdgeIntersections =
			new List<IPolyline>();

		/// <summary>
		/// Initializes a new instance of the <see cref="KnownGaps"/> class.
		/// </summary>
		/// <param name="maxGapArea">The maximum gap area.</param>
		/// <param name="tolerance">The xy tolerance.</param>
		/// <param name="allBox">The envelope of the test perimeter.</param>
		public KnownGaps(double maxGapArea, double tolerance, [NotNull] Box allBox)
		{
			Assert.ArgumentNotNull(allBox, nameof(allBox));

			_maxGapArea = maxGapArea;
			_tolerance = tolerance;
			_allBox = allBox;
		}

		public double Tolerance => _tolerance;

		[NotNull]
		public IEnumerable<IPolygon> GetCompletedGaps(
			[NotNull] IEnumerable<IPolygon> currentTileGaps,
			[NotNull] IEnvelope clipEnvelope,
			[NotNull] IEnvelope tileEnvelope)
		{
			Assert.ArgumentNotNull(currentTileGaps, nameof(currentTileGaps));
			Assert.ArgumentNotNull(clipEnvelope, nameof(clipEnvelope));
			Assert.ArgumentNotNull(tileEnvelope, nameof(tileEnvelope));

			var largeCrossingGaps = new List<IPolygon>();
			var smallCrossingGaps = new List<IPolygon>();
			var gapEnvelope = new EnvelopeClass();

			foreach (IPolygon gapPolygon in currentTileGaps)
			{
				gapPolygon.QueryEnvelope(gapEnvelope);

				if (IsGapFullyInside(gapEnvelope, clipEnvelope))
				{
					if (IsSmallerThanLimit(gapPolygon))
					{
						// gap is fully inside and smaller than limit (or there is no limit) -> report
						yield return gapPolygon;
					}
				}
				else
				{
					if (IsSmallerThanLimit(gapPolygon))
					{
						smallCrossingGaps.Add(gapPolygon);
					}
					else
					{
						largeCrossingGaps.Add(gapPolygon);
					}
				}
			}

			IPolyline tileBoundary = GetTileBoundary(tileEnvelope);
			IPolyline advancingTileBoundary = GetAdvancingTileBoundary(tileEnvelope);

			RememberLargeGapTileBoundaryIntersections(largeCrossingGaps, tileBoundary);

			foreach (IPolygon polygon in GetCompletedCrossingGaps(
				smallCrossingGaps, clipEnvelope, tileEnvelope, advancingTileBoundary))
			{
				yield return polygon;
			}
		}

		[NotNull]
		private IPolyline GetAdvancingTileBoundary([NotNull] IEnvelope tileEnvelope)
		{
			IPolyline result = GeometryFactory.CreatePolyline(
				tileEnvelope.SpatialReference,
				GeometryFactory.CreatePoint(_allBox.Min.X, tileEnvelope.YMax),
				GeometryFactory.CreatePoint(tileEnvelope.XMax, tileEnvelope.YMax),
				GeometryFactory.CreatePoint(tileEnvelope.XMax, tileEnvelope.YMin));

			GeometryUtils.AllowIndexing(result);

			return result;
		}

		private void RememberLargeGapTileBoundaryIntersections(
			[NotNull] IEnumerable<IPolygon> largeCrossingGaps,
			[NotNull] IPolyline tileBoundary)
		{
			foreach (IPolygon largeCrossingGap in largeCrossingGaps)
			{
				IPolyline intersection = GetLinearIntersection(
					largeCrossingGap, tileBoundary);

				if (intersection != null)
				{
					_largeGapCuttingEdgeIntersections.Add(intersection);
				}
			}
		}

		[CanBeNull]
		private static IPolyline GetLinearIntersection(
			[NotNull] IPolygon polygon,
			[NotNull] IPolyline polyline)
		{
			var polygonTopoOp = (ITopologicalOperator) polygon;

			var result = (IPolyline) IntersectionUtils.Intersect(
				polygonTopoOp, polyline, esriGeometryDimension.esriGeometry1Dimension);

			if (result.IsEmpty)
			{
				return null;
			}

			const bool allowReorder = true;
			const bool allowPathSplitAtIntersections = false;
			GeometryUtils.Simplify(result, allowReorder, allowPathSplitAtIntersections);

			GeometryUtils.AllowIndexing(result);

			return result;
		}

		[NotNull]
		private static IPolyline GetTileBoundary([NotNull] IEnvelope tileEnvelope)
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(tileEnvelope);
			try
			{
				var result = (IPolyline) ((ITopologicalOperator) polygon).Boundary;

				GeometryUtils.AllowIndexing(result);

				return result;
			}
			finally
			{
				Marshal.ReleaseComObject(polygon);
			}
		}

		[NotNull]
		private IEnumerable<IPolygon> GetCompletedCrossingGaps(
			[NotNull] IEnumerable<IPolygon> currentSmallCrossingGaps,
			[NotNull] IEnvelope clipEnvelope,
			[NotNull] IEnvelope tileEnvelope,
			[NotNull] IPolyline advancingTileBoundary)
		{
			IEnumerable<IPolygon> allCrossingGaps = GetAllCrossingGaps(
				currentSmallCrossingGaps, tileEnvelope, _uncompletedGapPolygons);

			WKSEnvelope tileBox;
			tileEnvelope.QueryWKSCoords(out tileBox);

			WKSEnvelope clipBox;
			clipEnvelope.QueryWKSCoords(out clipBox);

			_uncompletedGapPolygons.Clear();

			var envelopeTemplate = new EnvelopeClass();

			foreach (IPolygon crossingGap in allCrossingGaps)
			{
				crossingGap.QueryEnvelope(envelopeTemplate);

				WKSEnvelope crossingGapBox;
				envelopeTemplate.QueryWKSCoords(out crossingGapBox);

				if (crossingGapBox.XMax < tileBox.XMax - _tolerance &&
				    crossingGapBox.YMax < tileBox.YMax - _tolerance)
				{
					// the gap is to left and bottom of current tile xmax/ymax

					if (crossingGapBox.XMin <= clipBox.XMin || crossingGapBox.YMin <= clipBox.YMin)
					{
						// the gap exceeds the left or bottom tile border
						// (should always be the case, otherwise the gap would not be crossing)

						if (IsSmallerThanLimit(crossingGap))
						{
							// this must include large gaps from current tile also
							if (! ExceedsOrTouchesAllBox(crossingGapBox))
							{
								// the gap is within the test run box
								if (! BelongsToLargeGap(crossingGap))
								{
									yield return crossingGap;
								}
							}
						}
					}
				}
				else
				{
					// the gap crosses xmax or ymax of the tile
					// -> remember it 
					if (IsSmallerThanLimit(crossingGap) && ! BelongsToLargeGap(crossingGap))
					{
						// we might need the polygon for reporting an error -> remember it
						_uncompletedGapPolygons.Add(crossingGap);

						// TODO: if the crossing gap exceeds or touches the test extent, 
						// we know that we won't need it for error reporting; however we 
						// still need to know about the intersections with the advancing
						// boundary of the processed area, so we would have to keep the 
						// intersection line with that advancing boundary
						// by creating a polyline with three points:
						// 1 allbox.xmin, tile.ymax
						// 2 tile.xmax, tile.ymax
						// 3 tile.xmax, tile.ymin
					}
					else
					{
						// just remember the linear intersection between gap polygon and 
						// the advancing boundary of the *current* tile
						IPolyline intersection = GetLinearIntersection(crossingGap,
						                                               advancingTileBoundary);

						if (intersection != null)
						{
							_largeGapCuttingEdgeIntersections.Add(intersection);
						}
					}
				}
			}

			RemoveObsoleteCuttingEdgeIntersections(tileBox.XMax, tileBox.YMax, _tolerance);
		}

		private void RemoveObsoleteCuttingEdgeIntersections(double tileXMax,
		                                                    double tileYMax,
		                                                    double tolerance)
		{
			var intersectionEnvelope = new EnvelopeClass();

			var remaining = new List<IPolyline>();
			foreach (IPolyline intersection in _largeGapCuttingEdgeIntersections)
			{
				intersection.QueryEnvelope(intersectionEnvelope);

				double xMax;
				double yMax;
				intersectionEnvelope.QueryCoords(out _, out _, out xMax, out yMax);

				if (xMax >= tileXMax - tolerance || yMax >= tileYMax - tolerance)
				{
					remaining.Add(intersection);
				}
			}

			_largeGapCuttingEdgeIntersections.Clear();
			_largeGapCuttingEdgeIntersections.AddRange(remaining);
		}

		private bool BelongsToLargeGap([NotNull] IPolygon crossingGap)
		{
			Assert.ArgumentNotNull(crossingGap, nameof(crossingGap));

			var gapRelOp = (IRelationalOperator) crossingGap;
			var gapTopoOp = (ITopologicalOperator) crossingGap;

			foreach (IPolyline intersection in _largeGapCuttingEdgeIntersections)
			{
				if (gapRelOp.Disjoint(intersection))
				{
					continue;
				}

				IGeometry linearIntersection = IntersectionUtils.Intersect(
					gapTopoOp, intersection, esriGeometryDimension.esriGeometry1Dimension);

				try
				{
					if (! linearIntersection.IsEmpty)
					{
						return true;
					}
				}
				finally
				{
					Marshal.ReleaseComObject(linearIntersection);
				}
			}

			return false;
		}

		[NotNull]
		private static IEnumerable<IPolygon> GetAllCrossingGaps(
			[NotNull] IEnumerable<IPolygon> currentCrossingGaps,
			[NotNull] IEnvelope tileEnvelope,
			[NotNull] ICollection<IPolygon> uncompletedGapPolygons)
		{
			Assert.ArgumentNotNull(currentCrossingGaps, nameof(currentCrossingGaps));
			Assert.ArgumentNotNull(tileEnvelope, nameof(tileEnvelope));
			Assert.ArgumentNotNull(uncompletedGapPolygons, nameof(uncompletedGapPolygons));

			if (uncompletedGapPolygons.Count == 0)
			{
				// no previous tile gaps around (standard case for small test extents)
				// just return the crossing gaps from the current tile
				return currentCrossingGaps;
			}

			var allGaps = new List<IGeometry>();
			foreach (IPolygon uncompletedGapPolygon in uncompletedGapPolygons)
			{
				allGaps.Add(uncompletedGapPolygon);
			}

			foreach (IPolygon crossingGap in currentCrossingGaps)
			{
				((ITopologicalOperator) crossingGap).Clip(tileEnvelope);
				GeometryUtils.AllowIndexing(crossingGap);
				allGaps.Add(crossingGap);
			}

			var combined = (IPolygon4) GeometryUtils.UnionGeometries(allGaps);

			var result = new List<IPolygon>();
			var combinedGaps = (IGeometryCollection) combined.ConnectedComponentBag;

			int combinedGapCount = combinedGaps.GeometryCount;
			for (var index = 0; index < combinedGapCount; index++)
			{
				var combinedGap = (IPolygon) combinedGaps.get_Geometry(index);
				GeometryUtils.AllowIndexing(combinedGap);
				result.Add(combinedGap);
			}

			return result;
		}

		private bool ExceedsOrTouchesAllBox(WKSEnvelope envelope)
		{
			return envelope.XMin <= _allBox.Min.X + _tolerance ||
			       envelope.YMin <= _allBox.Min.Y + _tolerance ||
			       envelope.XMax >= _allBox.Max.X - _tolerance ||
			       envelope.YMax >= _allBox.Max.Y - _tolerance;
		}

		private bool IsGapFullyInside([NotNull] IEnvelope gapEnvelope,
		                              [NotNull] IEnvelope clipEnvelope)
		{
			double xMin;
			double yMin;
			double xMax;
			double yMax;
			gapEnvelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			double clipXMin;
			double clipYMin;
			double clipXMax;
			double clipYMax;
			clipEnvelope.QueryCoords(out clipXMin, out clipYMin, out clipXMax, out clipYMax);

			return xMin >= clipXMin + _tolerance &&
			       yMin >= clipYMin + _tolerance &&
			       xMax < clipXMax - _tolerance &&
			       yMax < clipYMax - _tolerance;
		}

		private static double GetArea([NotNull] IPolygon polygon)
		{
			return Math.Abs(((IArea) polygon).Area);
		}

		private bool IsSmallerThanLimit([NotNull] IPolygon polygon)
		{
			return _maxGapArea <= 0 || GetArea(polygon) <= _maxGapArea;
		}
	}
}
