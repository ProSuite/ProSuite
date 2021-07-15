using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.TestContainer
{
	internal class TerrainRowEnumerable
	{
		[NotNull] private readonly TerrainReference _terrainReference;
		private readonly double _resolution;
		[NotNull] private readonly ITestProgress _progress;

		public TerrainRowEnumerable([NotNull] TerrainReference terrainRef,
		                            double resolution,
		                            [NotNull] ITestProgress progress)
		{
			_terrainReference = terrainRef;
			_resolution = resolution;
			FirstTerrainBox = GetFirstTerrainTileExtent(terrainRef);
			_progress = progress;
		}

		public IBox FirstTerrainBox { get; }

		public int GetTerrainTileCount([NotNull] Box currentTileBox)
		{
			int terrainTileCount;
			int startTileIndexX;
			int endTileIndexX;
			int startTileIndexY;
			double tileSize;
			double terrainXMin;
			double terrainYMin;

			GetTerrainTiling(currentTileBox, FirstTerrainBox,
			                 out terrainTileCount,
			                 out startTileIndexX, out endTileIndexX,
			                 out startTileIndexY,
			                 out tileSize,
			                 out terrainXMin, out terrainYMin);

			return terrainTileCount;
		}

		public IEnumerable<TerrainRow> GetTerrainRows([NotNull] Box box)
		{
			int startTileIndexX;
			int endTileIndexX;
			int startTileIndexY;
			double terrainTileSize;
			double terrainXMin;
			double terrainYMin;

			int terrainTileCount;
			GetTerrainTiling(
				box, FirstTerrainBox,
				out terrainTileCount,
				out startTileIndexX, out endTileIndexX,
				out startTileIndexY,
				out terrainTileSize, out terrainXMin, out terrainYMin);

			for (var terrainTileIndex = 0;
			     terrainTileIndex < terrainTileCount;
			     terrainTileIndex++)
			{
				int iX = terrainTileIndex % (endTileIndexX - startTileIndexX);
				int iY = terrainTileIndex / (endTileIndexX - startTileIndexX);

				double xMin = terrainXMin + (iX + startTileIndexX) * terrainTileSize;
				double yMin = terrainYMin + (iY + startTileIndexY) * terrainTileSize;
				double xMax = terrainXMin + (iX + startTileIndexX + 1) * terrainTileSize;
				double yMax = terrainYMin + (iY + startTileIndexY + 1) * terrainTileSize;

				IEnvelope terrainBox = new EnvelopeClass();
				terrainBox.PutCoords(xMin, yMin, xMax, yMax);
				terrainBox.SpatialReference = _terrainReference.Dataset.SpatialReference;

				var terrainRow = new TerrainRow(terrainBox, _terrainReference,
				                                _resolution, _progress);

				yield return terrainRow;

				if (terrainRow.HasLoadedSurface)
				{
					terrainRow.DisposeSurface();
				}
			}
		}

		[NotNull]
		private static Box GetFirstTerrainTileExtent([NotNull] TerrainReference terrain)
		{
			double xMin = terrain.Tiling.OriginX;
			double yMin = terrain.Tiling.OriginY;
			double tileSize = terrain.Tiling.TileWidth;

			return new Box(new Pnt2D(xMin, yMin),
			               new Pnt2D(xMin + tileSize, yMin + tileSize));
		}

		private static void GetTerrainTiling(
			[NotNull] Box tileBox,
			[NotNull] IBox firstTileExtent,
			out int tileCount,
			out int startTileIndexX,
			out int endTileIndexX,
			out int startTileIndexY,
			out double tileSize,
			out double terrainXMin,
			out double terrainYMin)
		{
			Assert.ArgumentNotNull(tileBox, nameof(tileBox));
			Assert.ArgumentNotNull(firstTileExtent, nameof(firstTileExtent));

			terrainXMin = firstTileExtent.Min.X;
			terrainYMin = firstTileExtent.Min.Y;
			tileSize = firstTileExtent.Max.X - terrainXMin;

			startTileIndexX = (int) Math.Floor((tileBox.Min.X - terrainXMin) / tileSize);
			endTileIndexX = (int) Math.Ceiling((tileBox.Max.X - terrainXMin) / tileSize);

			startTileIndexY = (int) Math.Floor((tileBox.Min.Y - terrainYMin) / tileSize);
			var endTileIndexY = (int) Math.Ceiling((tileBox.Max.Y - terrainYMin) / tileSize);

			tileCount = (endTileIndexX - startTileIndexX) *
			            (endTileIndexY - startTileIndexY);
		}
	}
}
