using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Creates non-overlapping ownership cores and expands each core by a halo for
	/// the independently processed prediction service request. The halo provides context at
	/// core seams; request extents are clipped to the complete raster extent.
	/// </summary>
	public static class AreaPredictionTileGrid
	{
		[NotNull]
		public static IList<AreaPredictionTileExtent> Create(
			[NotNull] IEnvelope completeExtent,
			double coreTileSize,
			double requestHalo)
		{
			Assert.ArgumentNotNull(completeExtent, nameof(completeExtent));
			Assert.ArgumentCondition(coreTileSize > 0,
			                         "Core tile size must be positive");
			Assert.ArgumentCondition(requestHalo >= 0,
			                         "Request halo must not be negative");

			ISpatialReference spatialReference = completeExtent.SpatialReference;
			double xyTolerance = spatialReference == null
				                     ? 0
				                     : GeometryUtils.GetXyTolerance(spatialReference);
			double coordinateTolerance = Math.Max(xyTolerance, coreTileSize * 0.001);

			IList<CoreInterval> xIntervals = GetCoreIntervals(
				completeExtent.XMin, completeExtent.XMax, coreTileSize,
				requestHalo, coordinateTolerance);
			IList<CoreInterval> yIntervals = GetCoreIntervals(
				completeExtent.YMin, completeExtent.YMax, coreTileSize,
				requestHalo, coordinateTolerance);

			var result = new List<AreaPredictionTileExtent>(
				xIntervals.Count * yIntervals.Count);
			foreach (CoreInterval y in yIntervals)
			{
				foreach (CoreInterval x in xIntervals)
				{
					IEnvelope coreExtent = CreateEnvelope(
						x.Minimum, y.Minimum, x.Maximum, y.Maximum,
						spatialReference);
					IEnvelope requestExtent = CreateEnvelope(
						Math.Max(completeExtent.XMin, x.Minimum - requestHalo),
						Math.Max(completeExtent.YMin, y.Minimum - requestHalo),
						Math.Min(completeExtent.XMax, x.Maximum + requestHalo),
						Math.Min(completeExtent.YMax, y.Maximum + requestHalo),
						spatialReference);
					result.Add(new AreaPredictionTileExtent(requestExtent, coreExtent));
				}
			}

			return result;
		}

		[NotNull]
		private static IList<CoreInterval> GetCoreIntervals(
			double minimum, double maximum, double coreTileSize,
			double requestHalo, double coordinateTolerance)
		{
			Assert.ArgumentCondition(maximum >= minimum,
			                         "Maximum coordinate must not be smaller than minimum");

			// Raster mosaics commonly differ from their nominal kilometre grid by a
			// sub-pixel tolerance. Snap only such near-grid coordinates; arbitrary
			// extents remain anchored at their actual minimum.
			double gridMinimum = SnapToGrid(
				minimum, coreTileSize, coordinateTolerance);
			double gridMaximum = SnapToGrid(
				maximum, coreTileSize, coordinateTolerance);
			double gridLength = Math.Max(0, gridMaximum - gridMinimum);
			double singleRequestCapacity = coreTileSize + 2 * requestHalo;
			int intervalCount;
			if (gridLength <= singleRequestCapacity + coordinateTolerance)
			{
				intervalCount = 1;
			}
			else
			{
				// The first and last cores need a halo on only their inner side, so
				// together they can absorb one extra halo each. This yields the minimum
				// number of requests without exceeding the normal expanded request size.
				intervalCount = Math.Max(
					2, (int) Math.Ceiling(
						(gridLength - 2 * requestHalo - coordinateTolerance) /
						coreTileSize));
			}

			double[] intervalWidths = GetBalancedIntervalWidths(
				gridLength, intervalCount, coreTileSize);

			var result = new List<CoreInterval>(intervalCount);
			double current = gridMinimum;
			for (var index = 0; index < intervalCount; index++)
			{
				double intervalMinimum = index == 0
					                         ? minimum
					                         : current;
				current += intervalWidths[index];
				double intervalMaximum = index == intervalCount - 1
					                         ? maximum
					                         : Math.Min(maximum, current);
				result.Add(new CoreInterval(intervalMinimum, intervalMaximum));
			}

			return result;
		}

		[NotNull]
		private static double[] GetBalancedIntervalWidths(
			double totalLength, int intervalCount, double coreTileSize)
		{
			var result = new double[intervalCount];
			if (intervalCount == 1)
			{
				result[0] = totalLength;
				return result;
			}

			if (totalLength <= intervalCount * coreTileSize)
			{
				// Distribute a short remainder over every core instead of leaving one
				// tiny final request.
				double width = totalLength / intervalCount;
				for (var index = 0; index < intervalCount; index++)
				{
					result[index] = width;
				}

				return result;
			}

			for (var index = 0; index < intervalCount; index++)
			{
				result[index] = coreTileSize;
			}

			// Only edge cores can exceed the nominal core size without making an
			// interior request larger than core size plus two halos.
			double edgeExcess = (totalLength - intervalCount * coreTileSize) / 2;
			result[0] += edgeExcess;
			result[intervalCount - 1] += edgeExcess;
			return result;
		}

		private static double SnapToGrid(double coordinate, double gridSize,
		                                 double coordinateTolerance)
		{
			double nearestGridCoordinate = Math.Round(coordinate / gridSize) * gridSize;
			return Math.Abs(coordinate - nearestGridCoordinate) <= coordinateTolerance
				       ? nearestGridCoordinate
				       : coordinate;
		}

		[NotNull]
		private static IEnvelope CreateEnvelope(
			double xMin, double yMin, double xMax, double yMax,
			[CanBeNull] ISpatialReference spatialReference)
		{
			IEnvelope result = new EnvelopeClass();
			result.PutCoords(xMin, yMin, xMax, yMax);
			result.SpatialReference = spatialReference;
			return result;
		}

		private class CoreInterval
		{
			public CoreInterval(double minimum, double maximum)
			{
				Minimum = minimum;
				Maximum = maximum;
			}

			public double Minimum { get; }
			public double Maximum { get; }
		}
	}

	public class AreaPredictionTileExtent
	{
		public AreaPredictionTileExtent([NotNull] IEnvelope requestExtent,
		                                [NotNull] IEnvelope coreExtent)
		{
			RequestExtent = requestExtent;
			CoreExtent = coreExtent;
		}

		[NotNull] public IEnvelope RequestExtent { get; }
		[NotNull] public IEnvelope CoreExtent { get; }
	}
}
