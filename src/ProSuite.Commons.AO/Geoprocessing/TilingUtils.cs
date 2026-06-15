using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geoprocessing
{
	public static class TilingUtils
	{
		public static IList<IEnvelope> GetRegularSubdivisions(
			[NotNull] IPolygon areaOfInterest, double tileSize, double tileOverlap)
		{
			var result = new List<IEnvelope>();

			IEnvelope currentTile = null;

			while (
				(currentTile =
					 GetNextTile(currentTile, areaOfInterest.Envelope, tileSize, tileOverlap)) !=
				null)
			{
				if (GeometryUtils.Intersects(currentTile, areaOfInterest))
				{
					result.Add(currentTile);
				}
			}

			return result;
		}

		[CanBeNull]
		private static IEnvelope GetNextTile([CanBeNull] IEnvelope currentTile,
		                                     [NotNull] IEnvelope totalArea,
		                                     double tileSize,
		                                     double tileOverlap)
		{
			double currentXMin, currentYMin;

			double stepSize = tileSize - tileOverlap;

			if (currentTile == null)
			{
				// move to before the start
				currentXMin = totalArea.XMin - stepSize;
				currentYMin = totalArea.YMin;
			}
			else
			{
				currentXMin = currentTile.XMin;
				currentYMin = currentTile.YMin;
			}

			// iterate in X
			double nextXMinProposed = currentXMin + stepSize;

			if (nextXMinProposed <= totalArea.XMax)
			{
				// just move one column to the right
				return CreateNewTile(nextXMinProposed, currentYMin, tileSize,
				                     totalArea.SpatialReference);
			}

			// set back one row up
			double newXMin = totalArea.XMin;
			double newYMin = currentYMin + stepSize;

			// make sure it has not moved outside the terrain
			return newYMin < totalArea.YMax
				       ? CreateNewTile(newXMin, newYMin, tileSize, totalArea.SpatialReference)
				       : null; // completely outside
		}

		private static IEnvelope CreateNewTile(double nextXMinProposed, double currentYMin,
		                                       double tileSize, ISpatialReference spatialReference)
		{
			return GeometryFactory.CreateEnvelope(nextXMinProposed, currentYMin,
			                                      nextXMinProposed + tileSize,
			                                      currentYMin + tileSize,
			                                      spatialReference);
		}
	}
}
