﻿using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class TestContainerTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_testWs = TestWorkspaceUtils.CreateTestWorkspace("TestContainerTest");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanExecuteContainerOneDiagonalLine()
		{
			IFeatureClass featureClass =
				CreatePolylineFeatureClass("CanExecuteContainerOneDiagonalLine", 0.01);

			// Create error Feature
			IFeature row = featureClass.CreateFeature();
			const double x = 2600000;
			const double y = 1200000;
			row.Shape = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(x, y),
				GeometryFactory.CreatePoint(x + 1000, y + 800));
			row.Store();

			var helper = new CanExecuteContainerHelper();
			helper.ExpectedMaximumRowCountPerTile = 1;

			var test = new VerifyingContainerTest((ITable) featureClass)
			           {
				           OnExecuteCore = helper.ExecuteRow,
				           OnCompleteTile = helper.CompleteTile
			           };
			test.SetSearchDistance(10);

			var container = new TestContainer.TestContainer {TileSize = 600};
			container.AddTest(test);

			container.Execute();

			Assert.AreEqual(4 + 1, helper.CompleteTileCount); // + 1 : wegen initialem Tile
			Assert.AreEqual(4, helper.ExecuteRowCount); // 1 feature x 4 intersected tiles
		}

		[Test]
		public void CanExecuteContainerTwoVerticalLines()
		{
			IFeatureClass featureClass =
				CreatePolylineFeatureClass("CanExecuteContainerTwoVerticalLines", 0.01);

			// Create error Feature
			const double x = 2600000;
			const double y = 1200000;
			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(x, y),
				GeometryFactory.CreatePoint(x, y + 800));
			row1.Store();

			IFeature row2 = featureClass.CreateFeature();
			row2.Shape = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(x + 1000, y),
				GeometryFactory.CreatePoint(x + 1000, y + 800));
			row2.Store();

			var helper = new CanExecuteContainerHelper();
			helper.ExpectedMaximumRowCountPerTile = 1;

			var test = new VerifyingContainerTest((ITable) featureClass)
			           {
				           OnExecuteCore = helper.ExecuteRow,
				           OnCompleteTile = helper.CompleteTile
			           };
			test.SetSearchDistance(10);

			var container = new TestContainer.TestContainer {TileSize = 600};
			container.AddTest(test);

			container.Execute();

			Assert.AreEqual(4 + 1, helper.CompleteTileCount); // + 1 : wegen initialem Tile
			Assert.AreEqual(4, helper.ExecuteRowCount); // 2 features x 2 intersected tiles
		}

		[Test]
		public void CanExecuteContainerLineNearTileBoundary()
		{
			IFeatureClass featureClass =
				CreatePolylineFeatureClass("CanExecuteContainerLineNearTileBoundary", 0.01);

			// Create error Feature
			const double x = 2600000;
			const double y = 1200000;
			IFeature row1 = featureClass.CreateFeature();
			row1.Shape = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(x, y),
				GeometryFactory.CreatePoint(x, y + 800));
			row1.Store();

			IFeature row2 = featureClass.CreateFeature();
			row2.Shape = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(x + 1000, y),
				GeometryFactory.CreatePoint(x + 1000, y + 800));
			row2.Store();

			// row3 is within tile[0,0], but within the search tolerance from tile[0,1]
			IFeature row3 = featureClass.CreateFeature();
			row3.Shape = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(x + 300, y),
				GeometryFactory.CreatePoint(x + 300, y + 599.000));
			row3.Store();

			var helper = new CanExecuteContainerHelper();
			helper.ExpectedMaximumRowCountPerTile = 2;

			var test = new VerifyingContainerTest((ITable) featureClass)
			           {
				           OnExecuteCore = helper.ExecuteRow,
				           OnCompleteTile = helper.CompleteTile,
				           OnCachedRow = helper.CachedRow
			           };
			test.SetSearchDistance(2);

			var container = new TestContainer.TestContainer {TileSize = 600};
			container.AddTest(test);
			container.MaxCachedPointCount = 1; // disable caching
			container.Execute();

			Assert.AreEqual(4 + 1, helper.CompleteTileCount); // + 1 : wegen initialem Tile
			Assert.AreEqual(
				5, helper.ExecuteRowCount); // row1,row in 2 Tiles, row3 only executed in first Tile
			Assert.AreEqual(6, helper.CachedRowCount); // 3 features x 2 intersected tiles
		}

		[Test]
		public void CanExecuteContainePointFeatures()
		{
			IFeatureClass featureClass =
				CreatePointFeatureClass("CanExecuteContainePointFeatures", 0.01);

			// Create error Feature
			const double x = 2600000;
			const double y = 1200000;

			AddPointFeature(featureClass, x, y);
			AddPointFeature(featureClass, x + 1000, y + 1000);

			// create a point 1m south of the upper tile boundary, in the second tile of the first row
			AddPointFeature(featureClass, x + 599, y + 900);

			var helper = new CanExecuteContainerHelper();
			helper.ExpectedMaximumRowCountPerTile = 1;

			var test = new VerifyingContainerTest((ITable) featureClass)
			           {
				           OnExecuteCore = helper.ExecuteRow,
				           OnCompleteTile = helper.CompleteTile
			           };
			test.SetSearchDistance(0.5);

			var container = new TestContainer.TestContainer {TileSize = 600};
			container.AddTest(test);
			container.Execute();

			Assert.AreEqual(4 + 1, helper.CompleteTileCount); // + 1 : wegen initialem Tile
			Assert.AreEqual(3, helper.ExecuteRowCount);
		}

		[Test]
		public void CanHandleCachedPointCount()
		{
			IFeatureClass featureClass = CreatePolylineFeatureClass(
				"CanHandleCachedPointCount", 0.01);

			const double x = 2600000;
			const double y = 1200000;
			// Create features

			IFeature in4Tiles = featureClass.CreateFeature();
			in4Tiles.Shape =
				GeometryFactory.CreateLine(
					GeometryFactory.CreatePoint(x, y),
					GeometryFactory.CreatePoint(x + 1000, y + 800));
			in4Tiles.Store();

			IFeature inTiles_00_01 = featureClass.CreateFeature();
			inTiles_00_01.Shape =
				GeometryFactory.CreateLine(
					GeometryFactory.CreatePoint(x + 100, y + 100),
					GeometryFactory.CreatePoint(x + 800, y + 200));
			inTiles_00_01.Store();

			IFeature inTiles_00_10 = featureClass.CreateFeature();
			inTiles_00_10.Shape =
				GeometryFactory.CreateLine(
					GeometryFactory.CreatePoint(x + 100, y + 100),
					GeometryFactory.CreatePoint(x + 200, y + 200),
					GeometryFactory.CreatePoint(x + 200, y + 300),
					GeometryFactory.CreatePoint(x + 100, y + 400),
					GeometryFactory.CreatePoint(x + 100, y + 500),
					GeometryFactory.CreatePoint(x + 200, y + 600),
					GeometryFactory.CreatePoint(x + 200, y + 700),
					GeometryFactory.CreatePoint(x + 300, y + 800));
			inTiles_00_10.Store();

			IFeature inTiles_01_11 = featureClass.CreateFeature();
			inTiles_01_11.Shape =
				GeometryFactory.CreateLine(
					GeometryFactory.CreatePoint(x + 800, y + 100),
					GeometryFactory.CreatePoint(x + 800, y + 500),
					GeometryFactory.CreatePoint(x + 900, y + 1400));
			inTiles_01_11.Store();

			var helper = new CanHandleCachedPointCountHelper();
			var test = new VerifyingContainerTest((ITable) featureClass)
			           {
				           OnExecuteCore = helper.HandleRows,
				           OnCompleteTile = helper.HandleTiles
			           };
			test.SetSearchDistance(10);

			// Unlimited Cache
			var container1 = new TestContainer.TestContainer
			                 {
				                 TileSize = 600,
				                 MaxCachedPointCount = -1
			                 };
			// Assert.IsTrue(container1.MaxCachedPointCount < 0);

			helper.Reset();
			helper.Container = container1;
			helper.ExpectedCacheCount = new[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1};
			container1.AddTest(test);
			container1.Execute(GeometryFactory.CreateEnvelope(x, y, x + 1500, y + 1500));

			// Very large Cache
			var container2 = new TestContainer.TestContainer
			                 {
				                 MaxCachedPointCount = int.MaxValue,
				                 TileSize = 600
			                 };

			helper.Reset();
			helper.Container = container2;
			helper.ExpectedCacheCount = new[] {0, 0, 8, 13, 3, 0, 3, 3, 0, 0};
			container2.AddTest(test);
			container2.Execute(GeometryFactory.CreateEnvelope(x, y, x + 1500, y + 1500));

			// small feature Cache
			var container3 = new TestContainer.TestContainer
			                 {
				                 MaxCachedPointCount = 10,
				                 TileSize = 600
			                 };

			helper.Reset();
			helper.Container = container3;
			helper.ExpectedCacheCount = new[] {0, 0, 8, 5, 3, 0, 3, 3, 0, 0};
			// in the 5. tile, feature 'inTiles_01_11' with 3 points is used and hence not part of the cache 
			container3.AddTest(test);
			container3.Execute(GeometryFactory.CreateEnvelope(x, y, x + 1500, y + 1500));

			// very small feature Cache
			var container4 = new TestContainer.TestContainer
			                 {
				                 MaxCachedPointCount = 4,
				                 TileSize = 600
			                 };

			helper.Reset();
			helper.Container = container4;
			helper.ExpectedCacheCount = new[] {0, 0, 0, 2, 0, 0, 3, 3, 0, 0};
			// in the 3. tile, feature 'inTiles_01_11' with 3 points gets unloaded because of cache limitation 
			// in the 5. tile, feature 'inTiles_01_11' gets loaded again, so that in the 6. and 7. tile it is cached 
			container4.AddTest(test);
			container4.Execute(GeometryFactory.CreateEnvelope(x, y, x + 1500, y + 1500));
			// no feature Cache
			var container5 = new TestContainer.TestContainer
			                 {
				                 MaxCachedPointCount = 0,
				                 TileSize = 600
			                 };

			helper.Reset();
			helper.Container = container5;
			helper.ExpectedCacheCount = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			container5.AddTest(test);
			container5.Execute(GeometryFactory.CreateEnvelope(x, y, x + 1500, y + 1500));
		}

		[Test]
		public void CanIndexManyCoincidentPointsTest()
		{
			IFeatureClass featureClass =
				CreatePointFeatureClass("CanIndexManyCoincidentPointsTest", 0.001);

			AddPointFeature(featureClass, 2637000, 1193000);
			AddPointFeature(featureClass, 2638000, 1194000);

			// add 1000 coincident points
			const int count = 1000;
			for (int i = 0; i < count; i++)
			{
				AddPointFeature(featureClass, 2637887, 1193150.273400001);
			}

			// when encountering many coincident points, the BoxTree for the features must not be split "eternally"
			var test = new CacheTest(featureClass);
			var container = new TestContainer.TestContainer();
			container.AddTest(test);

			container.Execute();
		}

		[NotNull]
		private IFeatureClass CreatePolylineFeatureClass([NotNull] string name,
		                                                 double tolerance)
		{
			return TestWorkspaceUtils.CreateSimpleFeatureClass(
				_testWs, name, null,
				esriGeometryType.esriGeometryPolyline,
				esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				tolerance);
		}

		[NotNull]
		private IFeatureClass CreatePointFeatureClass([NotNull] string name,
		                                              double tolerance)
		{
			return TestWorkspaceUtils.CreateSimpleFeatureClass(
				_testWs, name, null,
				esriGeometryType.esriGeometryPoint,
				esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, tolerance);
		}

		private static void AddPointFeature([NotNull] IFeatureClass featureClass,
		                                    double x,
		                                    double y)
		{
			IFeature feature = featureClass.CreateFeature();
			feature.Shape = GeometryFactory.CreatePoint(x, y);
			feature.Store();
		}

		private class CacheTest : ContainerTest
		{
			public CacheTest(IFeatureClass dummy)
				: base((ITable) dummy) { }

			public override bool IsQueriedTable(int tableIndex)
			{
				return true;
			}

			protected override int ExecuteCore(IRow row, int tableIndex)
			{
				return 0;
			}
		}

		private class CanExecuteContainerHelper
		{
			private int _rowTileCount;

			public int CompleteTileCount { get; private set; }

			public int ExecuteRowCount { get; private set; }

			public int CachedRowCount { get; private set; }

			public int ExpectedMaximumRowCountPerTile { private get; set; }

			public int CompleteTile(TileInfo args)
			{
				Assert.IsTrue(_rowTileCount <= ExpectedMaximumRowCountPerTile);
				_rowTileCount = 0;
				CompleteTileCount++;
				return 0;
			}

			public int ExecuteRow(IRow row, int tableIndex)
			{
				_rowTileCount++;
				ExecuteRowCount++;
				return 0;
			}

			public int CachedRow(TileInfo args, IRow row, int tableIndex)
			{
				CachedRowCount++;
				return 0;
			}
		}

		private class CanHandleCachedPointCountHelper
		{
			private int _tileCount;
			private int _rowCount;

			private int _rowTileCount;

			public IList<int> ExpectedCacheCount { private get; set; }

			public TestContainer.TestContainer Container { private get; set; }

			public void Reset()
			{
				_tileCount = 0;
				_rowCount = 0;
			}

			public int HandleTiles(TileInfo args)
			{
				int cached = Container.GetCurrentCachedPointCount();
				Assert.AreEqual(ExpectedCacheCount[_tileCount], cached);

				_rowTileCount = 0;
				_tileCount++;
				return 0;
			}

			public int HandleRows(IRow row, int tableIndex)
			{
				Assert.IsNotNull(row);

				int cached = Container.GetCurrentCachedPointCount();
				Assert.AreEqual(ExpectedCacheCount[_tileCount], cached);

				_rowTileCount++;
				_rowCount++;
				return 0;
			}
		}
	}
}