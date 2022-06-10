using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	public class TileEnum
	{
		private readonly TestSorter _testSorter;
		private readonly IEnvelope _executeEnvelope;
		public double TileSize { get; }
		[CanBeNull] private readonly IBox _firstTerrainBox;

		/// <summary>
		/// Gets the current tile spatial reference.
		/// </summary>
		/// <value>
		/// The current tile spatial reference.
		/// </value>
		/// <remarks>currently the tiles are in same spatial reference as data </remarks>
		[CanBeNull]
		private ISpatialReference SpatialReference { get; }

		/// <summary>
		/// Envelope of current test run
		/// </summary>
		public IBox TestRunBox { get; }

		public TileEnum([NotNull] IList<ContainerTest> tests,
		                [CanBeNull] IEnvelope executeEnvelope, double tileSize,
		                [CanBeNull] ISpatialReference spatialReference)
			: this(new TestSorter(tests), executeEnvelope,
			       tileSize, spatialReference, null)
		{
			var _terrainRowEnumerables = _testSorter.PrepareTerrains(null);
			if (_terrainRowEnumerables?.Count > 0)
			{
				// TODO why take the first?
				const int firstTerrainIndex = 0;
				_firstTerrainBox =
					_terrainRowEnumerables[firstTerrainIndex].FirstTerrainBox;
			}
		}

		internal TileEnum([NotNull] TestSorter assembledTests,
		                  [CanBeNull] IEnvelope executeEnvelope, double tileSize,
		                  [CanBeNull] ISpatialReference spatialReference,
		                  [CanBeNull] IBox firstTerrainBox)
		{
			_testSorter = assembledTests;
			_executeEnvelope = executeEnvelope;
			TileSize = tileSize;
			SpatialReference = spatialReference;
			_firstTerrainBox = firstTerrainBox;

			TestRunBox = CreateTestRunBox(executeEnvelope);
		}

		[CanBeNull]
		private Box CreateTestRunBox(IEnvelope executeEnvelope)
		{
			if (executeEnvelope != null)
			{
				return QaGeometryUtils.CreateBox(executeEnvelope);
			}

			IEnvelope tableExtentUnion = TestUtils.GetFullExtent(GetInvolvedGeoDatasets());
			return tableExtentUnion == null
				       ? null
				       : QaGeometryUtils.CreateBox(tableExtentUnion);
		}

		private IEnumerable<IReadOnlyGeoDataset> GetInvolvedGeoDatasets()
		{
			foreach (IReadOnlyTable table in _testSorter.TestsPerTable.Keys)
			{
				var geoDataset = table as IReadOnlyGeoDataset;
				if (geoDataset != null)
				{
					yield return geoDataset;
				}
			}

			foreach (var terrainReference in _testSorter.TestsPerTerrain.Keys)
			{
				yield return terrainReference.Dataset;
			}

			foreach (RasterReference rasterReference in _testSorter.TestsPerRaster.Keys)
			{
				yield return rasterReference.GeoDataset;
			}
		}

		// TODO get this after *really* adjusting the tile size to the terrain tiles
		public int GetTotalTileCount()
		{
			const int tileExtentFraction = 1000;
			double roundingIncrement = TileSize / tileExtentFraction;

			double tileXMin = TestRunBox.Min.X;
			double tileYMin = TestRunBox.Min.Y;
			double dSize = TileSize + roundingIncrement;
			double allMaxX = TestRunBox.Max.X;
			double allMaxY = TestRunBox.Max.Y;

			double terrMinX = allMaxX + TileSize + roundingIncrement;
			double terrMinY = allMaxY + TileSize + roundingIncrement;

			if (_firstTerrainBox != null)
			{
				IBox terrBox = _firstTerrainBox;
				dSize = terrBox.Max.X - terrBox.Min.X;

				if (dSize < TileSize)
				{
					terrMinX = terrBox.Min.X;
					terrMinY = terrBox.Min.Y;
				}
			}

			var tileCountX = 0;
			double dXMax = tileXMin;
			while (dXMax < allMaxX)
			{
				dXMax += TileSize;
				if (terrMinX < dXMax)
				{
					dXMax = Math.Ceiling((dXMax - terrMinX) / dSize) * dSize + terrMinX;
				}

				tileCountX++;
			}

			var tileCountY = 0;
			double dYMax = tileYMin;
			while (dYMax < allMaxY)
			{
				dYMax += TileSize;
				if (terrMinY < dYMax)
				{
					dYMax = Math.Ceiling((dYMax - terrMinY) / dSize) * dSize + terrMinY;
				}

				tileCountY++;
			}

			return tileCountX * tileCountY;
		}

		[NotNull]
		internal Tile GetTile(double tileXMin, double tileYMin, int totalTileCount)
		{
			double tileXMax = tileXMin + TileSize;
			double tileYMax = tileYMin + TileSize;

			if (_firstTerrainBox != null)
			{
				IBox terrainFirstTileExtent = _firstTerrainBox;

				double terrainTileSize = terrainFirstTileExtent.Max.X -
				                         terrainFirstTileExtent.Min.X;

				if (terrainTileSize < TileSize)
				{
					if (terrainFirstTileExtent.Min.X < tileXMax)
					{
						tileXMax = Math.Ceiling((tileXMax - terrainFirstTileExtent.Min.X) /
						                        terrainTileSize)
						           * terrainTileSize + terrainFirstTileExtent.Min.X;
					}

					if (terrainFirstTileExtent.Min.Y < tileYMax)
					{
						tileYMax = Math.Ceiling((tileYMax - terrainFirstTileExtent.Min.Y) /
						                        terrainTileSize)
						           * terrainTileSize + terrainFirstTileExtent.Min.Y;
					}
				}
			}

			Tile tile = new Tile(tileXMin, tileYMin,
			                     Math.Min(tileXMax, TestRunBox.Max.X),
			                     Math.Min(tileYMax, TestRunBox.Max.Y),
			                     SpatialReference,
			                     totalTileCount);

			return tile;
		}

		[NotNull]
		public IEnvelope GetTestRunEnvelope()
		{
			IEnvelope result = new EnvelopeClass();

			result.PutCoords(TestRunBox.Min.X, TestRunBox.Min.Y,
			                 TestRunBox.Max.X, TestRunBox.Max.Y);

			return result;
		}

		[NotNull]
		public IEnvelope GetInitialTileEnvelope()
		{
			IEnvelope result = new EnvelopeClass();

			// no current tile box yet - create zero-sized box at minx, miny of test run box
			result.PutCoords(TestRunBox.Min.X, TestRunBox.Min.Y,
			                 TestRunBox.Min.X, TestRunBox.Min.Y);
			result.SpatialReference = SpatialReference;

			return result;
		}

		public IEnumerable<Tile> EnumTiles()
		{
			double tileXMin = TestRunBox.Min.X;
			double tileYMin = TestRunBox.Min.Y;

			int totalTileCount = GetTotalTileCount();

			for (int i = 0; i < totalTileCount; i++)
			{
				Tile tile = GetTile(tileXMin, tileYMin, totalTileCount);
				yield return tile;

				tileXMin = tile.Box.Max.X;
				if (tileXMin >= TestRunBox.Max.X)
				{
					tileXMin = TestRunBox.Min.X;
					tileYMin = tile.Box.Max.Y;
				}

				if (tileYMin > TestRunBox.Max.Y)
				{
					yield break;
				}
			}
		}
	}
}
