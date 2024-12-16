using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.TestContainer
{
	public class TileEnum
	{
		private readonly TestSorter _testSorter;
		private readonly IEnvelope _executeEnvelope;
		public double TileSize { get; }
		[CanBeNull] private readonly IBox _firstTerrainBox;

		private readonly Dictionary<int[], Tile> _tileCache =
			new Dictionary<int[], Tile>(new TileKeyComparer());

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
			Assert.ArgumentNotNull(assembledTests, nameof(assembledTests));
			Assert.ArgumentCondition(tileSize > 0, "Tile size must be greater 0");

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
				return ProxyUtils.CreateBox(executeEnvelope);
			}

			IEnvelope tableExtentUnion = TestUtils.GetFullExtent(GetInvolvedGeoDatasets());
			return tableExtentUnion == null
				       ? null
				       : ProxyUtils.CreateBox(tableExtentUnion);
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
		private double GetXIndex(double x)
		{
			double tileXMin = TestRunBox.Min.X;
			if (x < tileXMin)
			{
				return (x - tileXMin) / TileSize;
			}

			double allMaxX = TestRunBox.Max.X;
			int tileCountX = 0;
			while (tileXMin < allMaxX)
			{
				double nextX = GetTileXMax(tileXMin);
				if (nextX > x && nextX < TestRunBox.Max.X)
				{
					// x in TestRunBox
					return tileCountX + (x - tileXMin) / (nextX - tileXMin);
				}

				tileXMin = nextX;
				tileCountX++;
			}

			// x > TestRunBox.Max.X
			return tileCountX + (x - allMaxX) / TileSize;
		}

		private double GetYIndex(double y)
		{
			double tileYMin = TestRunBox.Min.Y;
			if (y < tileYMin)
			{
				return (y - tileYMin) / TileSize;
			}

			double allMaxY = TestRunBox.Max.Y;
			int tileCountY = 0;
			while (tileYMin < allMaxY)
			{
				double nextY = GetTileYMax(tileYMin);
				if (nextY > y && nextY < TestRunBox.Max.Y)
				{
					// x in TestRunBox
					return tileCountY + (y - tileYMin) / (nextY - tileYMin);
				}

				tileYMin = nextY;
				tileCountY++;
			}

			// x > TestRunBox.Max.Y
			return tileCountY + (y - allMaxY) / TileSize;
		}

		public int GetTotalTileCount()
		{
			int tileCountX = (int) GetXIndex(TestRunBox.Max.X);
			int tileCountY = (int) GetYIndex(TestRunBox.Max.Y);

			return tileCountX * tileCountY;
		}

		private double GetX(int ix)
		{
			if (ix <= 0)
			{
				return TestRunBox.Min.X + ix * TileSize;
			}

			double x = TestRunBox.Min.X;
			for (int i = 0; i < ix; i++)
			{
				double xMax = GetTileXMax(x);

				if (xMax > TestRunBox.Max.X)
				{
					x = TestRunBox.Max.X + (ix - i - 1) * TileSize;
					break;
				}

				x = xMax;
			}

			return x;
		}

		private double GetY(int iy)
		{
			if (iy <= 0)
			{
				return TestRunBox.Min.Y + iy * TileSize;
			}

			double y = TestRunBox.Min.Y;
			for (int i = 0; i < iy; i++)
			{
				double yMax = GetTileYMax(y);

				if (yMax > TestRunBox.Max.Y)
				{
					y = TestRunBox.Max.Y + (iy - i - 1) * TileSize;
					break;
				}

				y = yMax;
			}

			return y;
		}

		private double GetTileXMax(double tileXMin)
		{
			double tileXMax = tileXMin + TileSize;

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
				}
			}

			return tileXMax;
		}

		private double GetTileYMax(double tileYMin)
		{
			double tileYMax = tileYMin + TileSize;

			if (_firstTerrainBox != null)
			{
				IBox terrainFirstTileExtent = _firstTerrainBox;

				double terrainTileSize = terrainFirstTileExtent.Max.X -
				                         terrainFirstTileExtent.Min.X;

				if (terrainTileSize < TileSize)
				{
					if (terrainFirstTileExtent.Min.Y < tileYMax)
					{
						tileYMax = Math.Ceiling((tileYMax - terrainFirstTileExtent.Min.Y) /
						                        terrainTileSize)
						           * terrainTileSize + terrainFirstTileExtent.Min.Y;
					}
				}
			}

			return tileYMax;
		}

		[NotNull]
		internal Tile GetTile(double tileXMin, double tileYMin, int totalTileCount)
		{
			double tileXMax = GetTileXMax(tileXMin);
			double tileYMax = GetTileYMax(tileYMin);

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

		private class TileKeyComparer : IEqualityComparer<int[]>
		{
			public bool Equals(int[] x, int[] y)
			{
				return Assert.NotNull(x)[0] == Assert.NotNull(y)[0] && x[1] == y[1];
			}

			public int GetHashCode(int[] obj)
			{
				return obj[0] ^ 29 * obj[1];
			}
		}

		internal IEnumerable<Tile> EnumTiles(IGeometry geometry)
		{
			if (! SpatialReferenceUtils.AreEqual(SpatialReference, geometry.SpatialReference))
			{
				throw new InvalidOperationException(
					"Spatial reference of search geometry is different from Tile Enumerator." +
					Environment.NewLine +
					$"Search geometry: {SpatialReferenceUtils.ToString(geometry.SpatialReference)}." +
					Environment.NewLine +
					$"Tile Enumerator: {SpatialReferenceUtils.ToString(SpatialReference)}");
			}

			IEnvelope geomEnv = geometry.Envelope;
			geomEnv.QueryWKSCoords(out WKSEnvelope wksEnv);
			int ixMin = (int) Math.Floor(GetXIndex(wksEnv.XMin));
			int iyMin = (int) Math.Floor(GetYIndex(wksEnv.YMin));

			int ixMax = (int) Math.Ceiling(GetXIndex(wksEnv.XMax));
			int iyMax = (int) Math.Ceiling(GetYIndex(wksEnv.YMax));

			double tileYMax = GetY(iyMin);
			double tileXMax0 = GetX(ixMin);

			for (int iy = iyMin; iy < iyMax; iy++)
			{
				double tileYMin = tileYMax;
				tileYMax = GetY(iy + 1);

				double tileXMax = tileXMax0;
				for (int ix = ixMin; ix < ixMax; ix++)
				{
					double tileXMin = tileXMax;
					tileXMax = GetX(ix + 1);

					Tile tile = new Tile(tileXMin, tileYMin,
					                     tileXMax, tileYMax,
					                     SpatialReference, -1);

					yield return tile;
				}
			}
		}

		public IEnumerable<Tile> EnumTiles()
		{
			double tileXMin = TestRunBox.Min.X;
			double tileYMin = TestRunBox.Min.Y;

			int totalTileCount = GetTotalTileCount();

			int ix = 0;
			int iy = 0;
			for (int i = 0; i < totalTileCount; i++)
			{
				int[] tileKey = { ix, iy };
				if (! _tileCache.TryGetValue(tileKey, out Tile tile))
				{
					tile = GetTile(tileXMin, tileYMin, totalTileCount);
				}

				yield return tile;

				ix++;

				tileXMin = tile.Box.Max.X;
				if (tileXMin >= TestRunBox.Max.X)
				{
					tileXMin = TestRunBox.Min.X;
					tileYMin = tile.Box.Max.Y;

					iy++;
					ix = 0;
				}

				if (tileYMin > TestRunBox.Max.Y)
				{
					yield break;
				}
			}
		}
	}
}
