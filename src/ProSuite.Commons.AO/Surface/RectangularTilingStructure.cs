using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	public class RectangularTilingStructure
	{
		[CLSCompliant(false)]
		public RectangularTilingStructure(
			double originX, double originY,
			double tileWidth, double tileHeight,
			BorderPointTileAllocationPolicy borderPointTileAllocation,
			[CanBeNull] ISpatialReference spatialReference)
		{
			OriginX = originX;
			OriginY = originY;
			TileWidth = tileWidth;
			TileHeight = tileHeight;

			BorderPointTileAllocation = borderPointTileAllocation;
			SpatialReference = spatialReference;
		}

		public double OriginX { get; }

		public double OriginY { get; }

		public double TileWidth { get; }

		public double TileHeight { get; }

		public BorderPointTileAllocationPolicy BorderPointTileAllocation { get; }

		[CLSCompliant(false)]
		[CanBeNull]
		public ISpatialReference SpatialReference { get; }

		[CLSCompliant(false)]
		public RectangularTileIndex GetTileIndexAt([NotNull] IPoint point)
		{
			Assert.ArgumentNotNull(point, nameof(point));

			IPoint projectedPoint;
			GeometryUtils.EnsureSpatialReference(point, SpatialReference, true,
			                                     out projectedPoint);

			return GetTileIndexAt(projectedPoint.X, projectedPoint.Y);
		}

		public RectangularTileIndex GetTileIndexAt(double locationX, double locationY)
		{
			return GetTileIndexAt(locationX, locationY, BorderPointTileAllocation);
		}

		private RectangularTileIndex GetTileIndexAt(
			double locationX, double locationY,
			BorderPointTileAllocationPolicy borderPointTileAllocation)
		{
			return RectangularTilingUtils.GetTileIndex(locationX, locationY,
			                                           OriginX, OriginY,
			                                           TileWidth, TileHeight,
			                                           borderPointTileAllocation);
		}

		[CLSCompliant(false)]
		public IEnvelope GetTileEnvelope([CanBeNull] ISpatialReference spatialReference,
		                                 params RectangularTileIndex[] tileIndexes)
		{
			IEnvelope result = RectangularTilingUtils.GetTileEnvelope(OriginX, OriginY,
			                                                          TileWidth, TileHeight,
			                                                          spatialReference,
			                                                          tileIndexes);

			return result;
		}

		[NotNull]
		[CLSCompliant(false)]
		public IEnvelope GetIntersectedTilesExtent([NotNull] IEnvelope extent,
		                                           [NotNull] IEnvelope constraintExtent)
		{
			Assert.ArgumentNotNull(extent, nameof(extent));
			Assert.False(extent.IsEmpty, "extent is empty");
			Assert.ArgumentNotNull(constraintExtent, nameof(constraintExtent));
			Assert.False(constraintExtent.IsEmpty, "constraintExtent is empty");

			IEnvelope projectedExtent;
			GeometryUtils.EnsureSpatialReference(extent, SpatialReference, true,
			                                     out projectedExtent);
			IEnvelope projectedConstraintExtent;
			GeometryUtils.EnsureSpatialReference(constraintExtent, SpatialReference,
			                                     true,
			                                     out projectedConstraintExtent);

			int? leftIndex;
			int? rightIndex;
			RectangularTilingUtils.GetTileIndexes1D(OriginX, TileWidth,
			                                        projectedExtent.XMin, projectedExtent.XMax,
			                                        projectedConstraintExtent.XMin,
			                                        projectedConstraintExtent.XMax,
			                                        out leftIndex, out rightIndex);

			int? bottomIndex;
			int? topIndex;
			RectangularTilingUtils.GetTileIndexes1D(OriginY, TileHeight,
			                                        projectedExtent.YMin, projectedExtent.YMax,
			                                        projectedConstraintExtent.YMin,
			                                        projectedConstraintExtent.YMax,
			                                        out bottomIndex, out topIndex);

			IEnvelope result;
			if (leftIndex == null || rightIndex == null || bottomIndex == null ||
			    topIndex == null)
			{
				result = new EnvelopeClass();
				result.SpatialReference = SpatialReference;
			}
			else
			{
				var tileLL =
					new RectangularTileIndex(leftIndex.Value, bottomIndex.Value);
				var tileUR =
					new RectangularTileIndex(rightIndex.Value, topIndex.Value);

				result = GetTileEnvelope(SpatialReference, tileLL, tileUR);
			}

			return result;
		}

		[CLSCompliant(false)]
		public IEnumerable<RectangularTileIndex> GetIntersectingTiles([NotNull] IGeometry geometry)
		{
			IEnvelope extent = geometry.Envelope;

			IEnvelope queryEnv = new EnvelopeClass();
			queryEnv.SpatialReference = extent.SpatialReference;

			foreach (RectangularTileIndex tileInExtent in GetIntersectingTiles(extent))
			{
				QueryEnvelope(tileInExtent, queryEnv);

				if (GeometryUtils.Intersects(queryEnv, geometry))
				{
					yield return tileInExtent;
				}
			}
		}

		[CLSCompliant(false)]
		public IEnumerable<RectangularTileIndex> GetIntersectingTiles([NotNull] IEnvelope extent)
		{
			double xMin, yMin, xMax, yMax;
			extent.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			return GetIntersectingTiles(xMin, yMin, xMax, yMax);
		}

		public IEnumerable<RectangularTileIndex> GetIntersectingTiles(
			double xMin, double yMin, double xMax, double yMax)
		{
			RectangularTileIndex minIndex = GetTileIndexAt(xMin, yMin);

			RectangularTileIndex maxIndex = GetTileIndexAt(xMax, yMax);

			return GetAllTilesBetween(minIndex, maxIndex);
		}

		[CLSCompliant(false)]
		public void QueryEnvelope(RectangularTileIndex forTile,
		                          [NotNull] IEnvelope envelope)
		{
			RectangularTilingUtils.QueryTileEnvelope(OriginX, OriginY,
			                                         TileWidth, TileHeight,
			                                         forTile, envelope);
		}

		private static IEnumerable<RectangularTileIndex> GetAllTilesBetween(
			RectangularTileIndex minIndex,
			RectangularTileIndex maxIndex)
		{
			for (int i = minIndex.East; i <= maxIndex.East; i++)
			{
				for (int j = minIndex.North; j <= maxIndex.North; j++)
				{
					yield return new RectangularTileIndex(i, j);
				}
			}
		}
	}
}
