using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	public static class RectangularTilingUtils
	{
		public static RectangularTileIndex GetTileIndex(
			double locationX, double locationY,
			double originX, double originY,
			double tileWidth, double tileHeight,
			BorderPointTileAllocationPolicy borderPointTileAllocation)
		{
			int indexEast;
			int indexNorth;

			double tilePositionX = (locationX - originX) / tileWidth;
			double tilePositionY = (locationY - originY) / tileHeight;

			switch (borderPointTileAllocation)
			{
				case BorderPointTileAllocationPolicy.BottomLeft:

					indexEast = GetIntegerIndex(tilePositionX,
					                            AllocationPolicy1D.ExcludeUpperBound);
					indexNorth = GetIntegerIndex(tilePositionY,
					                             AllocationPolicy1D.ExcludeUpperBound);
					break;

				case BorderPointTileAllocationPolicy.TopLeft:

					indexEast = GetIntegerIndex(tilePositionX,
					                            AllocationPolicy1D.ExcludeUpperBound);
					indexNorth = GetIntegerIndex(tilePositionY,
					                             AllocationPolicy1D.IncludeUpperBound);
					break;

				case BorderPointTileAllocationPolicy.TopRight:

					indexEast = GetIntegerIndex(tilePositionX,
					                            AllocationPolicy1D.IncludeUpperBound);
					indexNorth = GetIntegerIndex(tilePositionY,
					                             AllocationPolicy1D.IncludeUpperBound);
					break;

				case BorderPointTileAllocationPolicy.BottomRight:

					indexEast = GetIntegerIndex(tilePositionX,
					                            AllocationPolicy1D.IncludeUpperBound);
					indexNorth = GetIntegerIndex(tilePositionY,
					                             AllocationPolicy1D.ExcludeUpperBound);
					break;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(borderPointTileAllocation),
						borderPointTileAllocation,
						@"Unexpected value");
			}

			return new RectangularTileIndex(indexEast, indexNorth);
		}

		[NotNull]
		public static IEnvelope GetTileEnvelope(
			double originX, double originY,
			double tileWidth, double tileHeight,
			[CanBeNull] ISpatialReference spatialReference = null,
			params RectangularTileIndex[] tileIndexes)
		{
			Assert.ArgumentNotNull(tileIndexes, nameof(tileIndexes));
			Assert.ArgumentCondition(tileIndexes.Length > 0,
			                         "At least one tile must be passed");

			double xMin = double.MaxValue;
			double yMin = double.MaxValue;
			double xMax = double.MinValue;
			double yMax = double.MinValue;

			foreach (RectangularTileIndex index in tileIndexes)
			{
				double tileXMin;
				double tileYMin;
				double tileXMax;
				double tileYMax;
				GetTileBounds(index, originX, originY, tileWidth, tileHeight,
				              out tileXMin, out tileYMin, out tileXMax, out tileYMax);

				xMin = Math.Min(tileXMin, xMin);
				yMin = Math.Min(tileYMin, yMin);
				xMax = Math.Max(tileXMax, xMax);
				yMax = Math.Max(tileYMax, yMax);
			}

			return GeometryFactory.CreateEnvelope(xMin, yMin, xMax, yMax, spatialReference);
		}

		public static void QueryTileEnvelope(double originX, double originY,
		                                     double tileWidth, double tileHeight,
		                                     RectangularTileIndex tileIndex,
		                                     [NotNull] IEnvelope envelope)
		{
			double xMin;
			double yMin;
			double xMax;
			double yMax;
			GetTileBounds(tileIndex, originX, originY, tileWidth, tileHeight,
			              out xMin, out yMin, out xMax, out yMax);

			envelope.SetEmpty();
			envelope.SpatialReference = null;

			envelope.PutCoords(xMin, yMin, xMax, yMax);
		}

		public static void GetTileBounds(RectangularTileIndex index,
		                                 double originX,
		                                 double originY,
		                                 double tileWidth, double tileHeight,
		                                 out double xMin, out double yMin,
		                                 out double xMax, out double yMax)
		{
			xMin = originX + (index.East * tileWidth);
			yMin = originY + (index.North * tileHeight);
			xMax = xMin + tileWidth;
			yMax = yMin + tileHeight;
		}

		public static void GetTileIndexes1D(double origin, double tileSize,
		                                    double start, double end,
		                                    double constraintMinimum,
		                                    double constraintMaximum,
		                                    out int? minIndex, out int? maxIndex)
		{
			double tilePositionStart = (start - origin) / tileSize;
			double tilePositionEnd = (end - origin) / tileSize;

			int startIndex = GetIntegerIndex(tilePositionStart,
			                                 AllocationPolicy1D.ExcludeUpperBound);
			int endIndex = GetIntegerIndex(tilePositionEnd,
			                               AllocationPolicy1D.IncludeUpperBound);

			minIndex = null;
			for (int index = startIndex; index <= endIndex; index++)
			{
				double tileXMin = origin + (index * tileSize);
				if (tileXMin >= constraintMinimum)
				{
					minIndex = index;
					break;
				}
			}

			maxIndex = null;
			for (int index = endIndex; index >= startIndex; index--)
			{
				double tileXMin = origin + (index * tileSize);
				if (tileXMin <= constraintMaximum)
				{
					maxIndex = index;
					break;
				}
			}
		}

		private static int GetIntegerIndex(double currentPosition,
		                                   AllocationPolicy1D boundaryAllocationPolicy)
		{
			if (boundaryAllocationPolicy == AllocationPolicy1D.ExcludeUpperBound)
			{
				return (int) Math.Floor(currentPosition);
			}

			if (Math.Abs(currentPosition - Math.Ceiling(currentPosition)) <
			    double.Epsilon)
			{
				// even though the point is already on the next integer value, 
				// return the lower one, because it is still included.
				return (int) currentPosition - 1;
			}

			return (int) Math.Floor(currentPosition);
		}

		private enum AllocationPolicy1D
		{
			IncludeUpperBound,
			ExcludeUpperBound
		}
	}
}
