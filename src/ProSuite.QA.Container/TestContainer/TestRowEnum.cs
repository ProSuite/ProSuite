using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Geometry.SpatialIndex;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	// TODO revise MoveNextCachedRow (IsFirstOccurrence assignment)
	internal class TestRowEnum : ISearchable, IDisposable
	{
		#region Fields

		private readonly bool _nothingToDo;

		private readonly int _cachedTableCount;
		private readonly IList<ITable> _cachedTables;
		private readonly IList<TableFields> _nonCachedTables;

		private readonly ITestContainer _container;
		private readonly IEnvelope _executeEnvelope;
		private readonly IPolygon _executePolygon;
		private readonly IRelationalOperator _executePolygonRelOp;
		private readonly TileCache _tileCache;
		private readonly UniqueIdProvider[] _uniqueIdProviders;
		private readonly Dictionary<ITable, string> _commonFilterExpressions;

		private readonly IList<TerrainRowEnumerable> _terrainRowEnumerables;
		private readonly RastersRowEnumerable _rastersRowEnumerable;

		private readonly IDictionary<ITable, IList<ContainerTest>> _testsPerTable;
		private readonly IDictionary<RasterReference, IList<ContainerTest>> _testsPerRaster;
		private readonly IDictionary<TerrainReference, IList<ContainerTest>> _testsPerTerrain;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly Dictionary<ITable, int> _totalRowCountPerTable;
		// might be used later

		private readonly Dictionary<ITable, int> _loadedRowCountPerTable;

		// tiling structure
		private readonly IBox _testRunBox; // Envelope of current test run 
		private readonly double _tileSize;

		// enumerator state

		// features whose box intersect the box + tolerance 

		// of the current row (per table)

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TestRowEnum"/> class.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <param name="executeEnvelope">The execute envelope.</param>
		/// <param name="executePolygon">The execute polygon.</param>
		/// <param name="tileSize">Size of the tile.</param>
		public TestRowEnum([NotNull] ITestContainer container,
		                   [CanBeNull] IEnvelope executeEnvelope,
		                   [CanBeNull] IPolygon executePolygon,
		                   double tileSize)
		{
			Assert.ArgumentNotNull(container, nameof(container));

			_container = container;
			_executeEnvelope = executeEnvelope;
			_executePolygon = executePolygon;
			_tileSize = tileSize;

			if (executePolygon != null)
			{
				_executePolygonRelOp = (IRelationalOperator) executePolygon;
			}

			_testsPerTable = TestUtils.GetTestsByTable(_container.ContainerTests);

			_testsPerRaster = TestUtils.GetTestsByInvolvedType(
				_container.ContainerTests, (test) => test.InvolvedRasters);

			_testsPerTerrain = TestUtils.GetTestsByInvolvedType(
				_container.ContainerTests, (test) => test.InvolvedTerrains);

			foreach (ContainerTest containerTest in _container.ContainerTests)
			{
				containerTest.DataContainer = this;
			}

			_rastersRowEnumerable = new RastersRowEnumerable(_testsPerRaster.Keys, _container);

			ClassifyTables(_testsPerTable, out _cachedTables, out _nonCachedTables);

			_cachedTableCount = _cachedTables.Count;

			_terrainRowEnumerables = PrepareTerrains(_testsPerTerrain);

			if (_testsPerTable.Count == 0 && _testsPerTerrain.Count == 0)
			{
				_nothingToDo = true;
				return;
			}

			_testRunBox = CreateTestRunBox();

			if (_testRunBox == null || Math.Abs(_testRunBox.GetMaxExtent()) < double.Epsilon)
			{
				_nothingToDo = true;
				return;
			}

			_tileCache = new TileCache(_cachedTables, _testRunBox, _container,
			                           _testsPerTable);
			_uniqueIdProviders = new UniqueIdProvider[_cachedTables.Count];
			for (var i = 0; i < _cachedTables.Count; i++)
			{
				_uniqueIdProviders[i] = UniqueIdProviderFactory.Create(_cachedTables[i]);
			}

			_commonFilterExpressions = GetCommonFilterExpressions(_testsPerTable);

			if (_container.CalculateRowCounts)
			{
				int tableCount = _testsPerTable.Count;

				_totalRowCountPerTable = new Dictionary<ITable, int>(tableCount);
				_loadedRowCountPerTable = new Dictionary<ITable, int>(tableCount);

				CalculateRowCounts(_totalRowCountPerTable, _loadedRowCountPerTable);
			}

			InitDelegates();
		}

		#endregion

		/// <summary>
		/// Gets the current tile spatial reference.
		/// </summary>
		/// <value>
		/// The current tile spatial reference.
		/// </value>
		/// <remarks>currently the tiles are in same spatial reference as data </remarks>
		[CanBeNull]
		private ISpatialReference CurrentTileSpatialReference =>
			_container.SpatialReference;

		// whose boxes intersect with (extended) Box or _pRow

		#region IEnumerator<TestRow> Members

		void IDisposable.Dispose()
		{
			Dispose(true);
		}

		private bool _disposed;

		public void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			ClearDelegates();
			_disposed = true;
		}

		public IEnumerable<TestRow> EnumTestRows()
		{
			if (_nothingToDo)
			{
				yield break;
			}

			foreach (var tile in EnumTiles())
			{
				if (_container.Cancelled)
				{
					yield break;
				}

				foreach (var testRow in EnumRows(tile))
				{
					if (_container.Cancelled)
					{
						yield break;
					}

					_tileCache.CurrentTestRow = testRow;
					yield return testRow;
				}
			}
		}

		#endregion

		#region ISearchable Members

		UniqueIdProvider ISearchable.GetUniqueIdProvider(ITable table)
		{
			int tableIndex = _cachedTables.IndexOf(table);

			return _uniqueIdProviders[tableIndex];
		}

		IEnumerable<IRow> ISearchable.Search(ITable table, IQueryFilter queryFilter,
		                                     QueryFilterHelper filterHelper,
		                                     IGeometry cacheGeometry)
		{
			return Search(table, queryFilter, filterHelper, cacheGeometry);
		}

		#endregion

		#region Searching the cache

		[CanBeNull]
		private IEnumerable<IRow> Search([NotNull] ITable table,
		                                 [NotNull] IQueryFilter queryFilter,
		                                 [NotNull] QueryFilterHelper filterHelper,
		                                 [CanBeNull] IGeometry cacheGeometry)
		{
			int tableIndex = _cachedTables.IndexOf(table);

			// if the table was not passed to the container, return null
			// to trigger a search in the database
			return tableIndex < 0
				       ? null
				       : _tileCache.Search(table, tableIndex, queryFilter, filterHelper,
				                           cacheGeometry);
		}

		#endregion

		~TestRowEnum()
		{
			Dispose(false);
		}

		[CanBeNull]
		private Box CreateTestRunBox()
		{
			if (_executeEnvelope != null)
			{
				return QaGeometryUtils.CreateBox(_executeEnvelope);
			}

			IEnvelope tableExtentUnion = TestUtils.GetFullExtent(GetInvolvedGeoDatasets());
			return tableExtentUnion == null
				       ? null
				       : QaGeometryUtils.CreateBox(tableExtentUnion);
		}

		private IEnumerable<IGeoDataset> GetInvolvedGeoDatasets()
		{
			foreach (ITable table in _testsPerTable.Keys)
			{
				var geoDataset = table as IGeoDataset;
				if (geoDataset != null)
				{
					yield return geoDataset;
				}
			}

			foreach (var terrainReference in _testsPerTerrain.Keys)
			{
				yield return terrainReference.Dataset;
			}

			foreach (RasterReference rasterReference in _testsPerRaster.Keys)
			{
				yield return (IGeoDataset) rasterReference.RasterDataset;
			}
		}

		private IList<TerrainRowEnumerable> PrepareTerrains(
			[NotNull] IDictionary<TerrainReference, IList<ContainerTest>> testsPerTerrain)
		{
			Assert.ArgumentNotNull(testsPerTerrain, nameof(testsPerTerrain));

			var result = new List<TerrainRowEnumerable>();

			{
				foreach (TerrainReference terrainRef in testsPerTerrain.Keys)
				{
					// get the list of tests for this terrain
					IList<ContainerTest> tests = testsPerTerrain[terrainRef];
					Assert.True(tests.Count > 0, "No tests for terrain {0}", terrainRef);

					// determine the lowest terrain tolerance
					double terrainResolution = tests[0].TerrainTolerance;

					foreach (ContainerTest test in tests)
					{
						terrainResolution = Math.Min(terrainResolution, test.TerrainTolerance);
					}

					var terrainRowEnumerable =
						new TerrainRowEnumerable(terrainRef, terrainResolution, _container);

					result.Add(terrainRowEnumerable);
				}
			}

			return result;
		}

		private void InitDelegates()
		{
			foreach (ContainerTest containerTest in _container.ContainerTests)
			{
				_container.SubscribeTestEvents(containerTest);

				containerTest.DataContainer = this;
				containerTest.IgnoreUndirected = true; //  ! _container.UseIstQuality;
			}
		}

		private void ClearDelegates()
		{
			foreach (ContainerTest containerTest in _container.ContainerTests)
			{
				containerTest.DataContainer = null;

				_container.UnsubscribeTestEvents(containerTest);
			}
		}

		private void CalculateRowCounts(
			[NotNull] IDictionary<ITable, int> totalRowCountPerTable,
			[NotNull] IDictionary<ITable, int> loadedRowCountPerTable)
		{
			IQueryFilter filter = new QueryFilterClass();

			foreach (ITable table in _testsPerTable.Keys)
			{
				filter.WhereClause = _container.FilterExpressionsUseDbSyntax
					                     ? _commonFilterExpressions[table]
					                     : string.Empty;

				int count;
				try
				{
					count = table.RowCount(filter);
				}
				catch (Exception exp)
				{
					throw new DataException(EnumCursor.CreateMessage(table, filter), exp);
				}

				totalRowCountPerTable.Add(table, count);
				loadedRowCountPerTable.Add(table, 0);
			}
		}

		// TODO get this after *really* adjusting the tile size to the terrain tiles
		private int GetTotalTileCount()
		{
			const int tileExtentFraction = 1000;
			double roundingIncrement = _tileSize / tileExtentFraction;

			double tileXMin = _testRunBox.Min.X;
			double tileYMin = _testRunBox.Min.Y;
			double dSize = _tileSize + roundingIncrement;
			double allMaxX = _testRunBox.Max.X;
			double allMaxY = _testRunBox.Max.Y;

			double terrMinX = allMaxX + _tileSize + roundingIncrement;
			double terrMinY = allMaxY + _tileSize + roundingIncrement;

			if (_terrainRowEnumerables != null && _terrainRowEnumerables.Count > 0)
			{
				const int firstTerrainIndex = 0;

				IBox terrBox = _terrainRowEnumerables[firstTerrainIndex].FirstTerrainBox;
				dSize = terrBox.Max.X - terrBox.Min.X;

				if (dSize < _tileSize)
				{
					terrMinX = terrBox.Min.X;
					terrMinY = terrBox.Min.Y;
				}
			}

			var tileCountX = 0;
			double dXMax = tileXMin;
			while (dXMax < allMaxX)
			{
				dXMax += _tileSize;
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
				dYMax += _tileSize;
				if (terrMinY < dYMax)
				{
					dYMax = Math.Ceiling((dYMax - terrMinY) / dSize) * dSize + terrMinY;
				}

				tileCountY++;
			}

			return tileCountX * tileCountY;
		}

		/// <summary>
		/// Iterate cached rows that are at least partly within the current tile
		/// </summary>
		private IEnumerable<TestRow> EnumCachedRows(Tile tile, int tileRowIndex, int tileRowCount)
		{
			int cachedTableIndex = 0;
			foreach (ITable table in _cachedTables)
			{
				double? cachedRowSearchTolerance = null;

				foreach (BoxTree<CachedRow>.TileEntry entry in _tileCache.EnumEntries(
					cachedTableIndex, tile.Box))
				{
					tileRowIndex++;

					CachedRow cachedRow = entry.Value;
					if (cachedRow.DisjointFromExecuteArea)
					{
						continue;
					}

					ITable cachedTable = _cachedTables[cachedTableIndex];

					IList<ContainerTest> testsPerTable = _testsPerTable[cachedTable];

					// TODO: use new class Class_with_ContainerTest_and_InvolvedTableIndex instead of containerTest for applicable tests
					IList<ContainerTest> applicableTests = GetApplicableTests(
						testsPerTable, cachedRow.Feature, tile.SpatialFilter.Geometry,
						out IList<ContainerTest> reducedTests);

					cachedRowSearchTolerance = cachedRowSearchTolerance ??
					                           _tileCache.OverlappingFeatures.GetSearchTolerance(
						                           cachedTable);

					_tileCache.OverlappingFeatures.RegisterTestedFeature(
						cachedRow, cachedRowSearchTolerance.Value, reducedTests);

					TestRow cachedTestRow =
						new TestRow(new RowReference(cachedRow.Feature, recycled: false),
						            entry.Box, applicableTests);

					_container.OnProgressChanged(Step.TestRowCreated, tileRowIndex,
					                             tileRowCount, cachedTestRow);

					yield return cachedTestRow;
				}

				cachedTableIndex++;
			}
		}

		private IEnumerable<TestRow> EnumNonCachedRows(Tile tile, int tileRowIndex,
		                                               int tileRowCount)
		{
			foreach (TableFields tableFields in _nonCachedTables)
			{
				ITable table = tableFields.Table;
				string origFields = tile.SpatialFilter.SubFields;

				tile.SpatialFilter.WhereClause = _container.FilterExpressionsUseDbSyntax
					                                 ? _commonFilterExpressions[table]
					                                 : string.Empty;

				try
				{
					tile.SpatialFilter.SubFields = tableFields.Fields;

					IList<ContainerTest> currentTests = _testsPerTable[table];

					foreach (IRow row in new EnumCursor(
						table, tile.SpatialFilter, recycle: true))
					{
						IList<ContainerTest> applicableTests = GetApplicableTests(
							currentTests, row, tile.SpatialFilter.Geometry,
							out IList<ContainerTest> reducedTests);

						var feature = row as IFeature;
						if (feature != null)
						{
							bool shapeFieldExcluded = tableFields.ShapeFieldExcluded;
							if (! shapeFieldExcluded)
							{
								BaseRow cachedRow = new SimpleBaseRow(feature);
								_tileCache.OverlappingFeatures.RegisterTestedFeature(
									cachedRow, 0, reducedTests);
							}
						}

						TestRow nonCachedTestRow = new TestRow(
							new RowReference(row, recycled: true), null,
							applicableTests);
						_container.OnProgressChanged(Step.TestRowCreated, tileRowIndex,
						                             tileRowCount,
						                             nonCachedTestRow);

						yield return nonCachedTestRow;

						tileRowCount++;
					}
				}
				finally
				{
					tile.SpatialFilter.SubFields = origFields;
				}
			}
		}

		[NotNull]
		private IList<ContainerTest> GetApplicableTests(
			[NotNull] IList<ContainerTest> containerTests,
			[NotNull] IRow row,
			[NotNull] IGeometry filterGeometry,
			[CanBeNull] out IList<ContainerTest> reducedTests)
		{
			reducedTests = null;
			foreach (ContainerTest test in containerTests)
			{
				if (IsTestSpatiallyApplicable(test, filterGeometry, row) &&
				    ! IsFullyTested(row, test))
				{
					// the test is applicable and needed for the row
					continue;
				}

				if (reducedTests == null)
				{
					reducedTests = new List<ContainerTest>(containerTests);
				}

				reducedTests.Remove(test);
			}

			return reducedTests ?? containerTests;
		}

		private bool IsFullyTested([NotNull] IRow row, [NotNull] ContainerTest test)
		{
			var feature = row as IFeature;

			ITable table = row.Table;
			if (table == null || feature == null)
			{
				return false;
			}

			if (! _tileCache.OverlappingFeatures.WasAlreadyTested(feature, test))
			{
				return false;
			}

			foreach (int tableIndex in test.GetTableIndexes(table))
			{
				if (test.RetestRowsPerIntersectedTile(tableIndex))
				{
					return false;
				}
			}

			return true;
		}

		private IEnumerable<TestRow> EnumRasterRows(Tile tile, int tileRowIndex, int tileRowCount)
		{
			if (_rastersRowEnumerable == null)
			{
				yield break;
			}

			foreach (RasterRow rasterRow in _rastersRowEnumerable.GetRasterRows(tile.Box))
			{
				TestRow rasterTestRow = new TestRow(rasterRow,
				                                    QaGeometryUtils.CreateBox(rasterRow.Extent),
				                                    _testsPerRaster[rasterRow.RasterReference]);

				// _container.OnProgressChanged(string.Format("Loaded TIN Part {0} of {1} for current Tile", _cachedRowIndex, nTiles));

				_container.OnProgressChanged(Step.TestRowCreated, tileRowIndex, tileRowCount,
				                             rasterTestRow);

				yield return rasterTestRow;

				tileRowIndex++;
			}
		}

		private IEnumerable<TestRow> EnumTinRows(Tile tile, int tileRowIndex, int tileRowCount)
		{
			foreach (TerrainRowEnumerable terrainRowEnumerable in _terrainRowEnumerables)
			{
				foreach (TerrainRow terrainRow in terrainRowEnumerable.GetTerrainRows(tile.Box))
				{
					IList<ContainerTest> tests = _testsPerTerrain[terrainRow.TerrainReference];

					TestRow terrainTestRow = new TestRow(
						terrainRow, QaGeometryUtils.CreateBox(terrainRow.Extent), tests);

					_container.OnProgressChanged(Step.TestRowCreated, tileRowIndex, tileRowCount,
					                             terrainTestRow);

					yield return terrainTestRow;

					tileRowIndex++;
				}
			}
		}

		private static bool IsTestSpatiallyApplicable([NotNull] ContainerTest test,
		                                              [NotNull] IGeometry tileGeometry,
		                                              [NotNull] IRow row)
		{
			if (test.AreaOfInterest == null)
			{
				return true;
			}

			if (((IRelationalOperator) test.AreaOfInterest).Contains(tileGeometry))
			{
				// The current tile is completely contained in the area of interest.
				// All rows 
				return true;
			}

			return ! test.IsOutsideAreaOfInterest(row);
		}

		/// <summary>
		/// Gets the number of cached rows for the current tile
		/// </summary>
		/// <returns></returns>
		private int GetTileCachedTablesRowCount()
		{
			return _tileCache.GetTablesRowCount();
		}

		/// <summary>
		/// Gets the number of terrain rows for the current tile
		/// </summary>
		/// <returns></returns>
		private int GetTileRasterRowCount(Tile tile)
		{
			int tileRasterCount = _rastersRowEnumerable.GetRastersTileCount(tile.Box);
			return tileRasterCount;
		}

		/// <summary>
		/// Gets the number of terrain rows for the current tile
		/// </summary>
		/// <returns></returns>
		private int GetTileTerrainRowCount(Tile tile)
		{
			var result = 0;

			foreach (TerrainRowEnumerable terrainRowEnumerable in _terrainRowEnumerables)
			{
				int terrainTileCount = terrainRowEnumerable.GetTerrainTileCount(tile.Box);

				result += terrainTileCount;
			}

			return result;
		}

		/// <summary>
		/// Gets the number of non cached rows for the current tile.
		/// </summary>
		/// <param name="tileSpatialFilter">The tile spatial filter.</param>
		/// <returns></returns>
		private int GetTileNonCachedTablesRowCount(
			[NotNull] ISpatialFilter tileSpatialFilter)
		{
			var result = 0;

			foreach (TableFields tableFields in _nonCachedTables)
			{
				ITable table = tableFields.Table;

				string origFields = tileSpatialFilter.SubFields;
				string origWhereClause = tileSpatialFilter.WhereClause;

				try
				{
					var queryName = ((IDataset) table).FullName as IQueryName2;
					if (queryName != null && table is IFeatureClass)
					{
						// Workaround for TOP-4975: crash for certain joins/extents if OID field 
						// (which was incorrectly changed by IName.Open()!) is used as only subfields field
						// Note: when not crashing, the resulting row count was incorrect when that OID field was used.
						tileSpatialFilter.SubFields = ((IFeatureClass) table).ShapeFieldName;
					}
					else
					{
						tileSpatialFilter.SubFields = table.OIDFieldName;
					}

					tileSpatialFilter.WhereClause = _container.FilterExpressionsUseDbSyntax
						                                ? _commonFilterExpressions[table]
						                                : string.Empty;

					try
					{
						result += table.RowCount(tileSpatialFilter);
					}
					catch (Exception exp)
					{
						throw new DataException(
							EnumCursor.CreateMessage(table, tileSpatialFilter), exp);
					}
				}
				finally
				{
					tileSpatialFilter.SubFields = origFields;
					tileSpatialFilter.WhereClause = origWhereClause;
				}
			}

			return result;
		}

		[NotNull]
		private static Dictionary<ITable, string> GetCommonFilterExpressions(
			[NotNull] ICollection<KeyValuePair<ITable, IList<ContainerTest>>> testsPerTable)
		{
			Assert.ArgumentNotNull(testsPerTable, nameof(testsPerTable));

			var result = new Dictionary<ITable, string>(testsPerTable.Count);

			foreach (KeyValuePair<ITable, IList<ContainerTest>> pair in testsPerTable)
			{
				ITable table = pair.Key;

				result.Add(table, GetCommonFilterExpression(table, tests: pair.Value));
			}

			return result;
		}

		[CanBeNull]
		private static string GetCommonFilterExpression(
			[NotNull] ITable table,
			[NotNull] IEnumerable<ContainerTest> tests)
		{
			string commonExpression = null;

			foreach (ContainerTest test in tests)
			{
				foreach (int tableIndex in test.GetTableIndexes(table))
				{
					string constraint = test.GetConstraint(tableIndex);

					if (constraint == null)
					{
						commonExpression = string.Empty;
					}
					else if (commonExpression == null)
					{
						commonExpression = constraint;
					}
					else if (! string.Equals(commonExpression, constraint,
					                         StringComparison.Ordinal))
					{
						commonExpression = string.Empty;
					}
				}
			}

			return commonExpression;
		}

		// optimize filter to exclude existing large objects

		private static void ClassifyTables(
			[NotNull] IDictionary<ITable, IList<ContainerTest>> testsPerTable,
			[NotNull] out IList<ITable> cachedTables,
			[NotNull] out IList<TableFields> nonCachedTables)
		{
			Assert.ArgumentNotNull(testsPerTable, nameof(testsPerTable));

			cachedTables = new List<ITable>();
			nonCachedTables = new List<TableFields>();

			foreach (KeyValuePair<ITable, IList<ContainerTest>> pair in testsPerTable)
			{
				ITable table = pair.Key;
				IList<ContainerTest> tests = pair.Value;
				var queried = false;
				var geometryUsed = false;

				foreach (ContainerTest test in tests)
				{
					IList<ITable> tableList = test.InvolvedTables;
					int involvedTableCount = tableList.Count;

					for (var tableIndex = 0;
					     tableIndex < involvedTableCount;
					     tableIndex++)
					{
						if (tableList[tableIndex] != table)
						{
							continue;
						}

						if (test.IsQueriedTable(tableIndex))
						{
							queried = true;
							geometryUsed = true;
						}
						else if (test.IsGeometryUsedTable(tableIndex))
						{
							geometryUsed = true;
						}
					}

					if (queried)
					{
						break;
					}
				}

				if (queried)
				{
					cachedTables.Add(table);
				}
				else
				{
					// if the geometry is not to be used for a feature class,
					// don't get the shape field. Exception: Access Gdb (spatial queries
					// do not work there if shape field is missing)
					bool excludeShapeField = ! geometryUsed && (table is IFeatureClass) &&
					                         ! IsInLocalDatabaseWorkspace(table);

					const string allFields = "*";

					string subFields = excludeShapeField
						                   ? GetFieldNamesExcludingShapeField(
							                   (IFeatureClass) table)
						                   : allFields;

					nonCachedTables.Add(new TableFields(table, subFields,
					                                    excludeShapeField));
				}
			}
		}

		private static bool IsInLocalDatabaseWorkspace([NotNull] ITable table)
		{
			var dataset = (IDataset) table;

			return dataset.Workspace.Type == esriWorkspaceType.esriLocalDatabaseWorkspace;
		}

		[NotNull]
		private static string GetFieldNamesExcludingShapeField(
			[NotNull] IFeatureClass featureClass)
		{
			string shapeField = featureClass.ShapeFieldName;

			IFields fields = featureClass.Fields;
			int fieldCount = fields.FieldCount;
			var sb = new StringBuilder();

			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				string name = fields.Field[fieldIndex].Name;

				if (
					string.Compare(name, shapeField, StringComparison.OrdinalIgnoreCase) !=
					0)
				{
					sb.AppendFormat("{0},", name);
				}
			}

			return sb.ToString(0, sb.Length - 1);
		}

		#region nested classes

		#region Nested type: TableFields

		private class TableFields
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="TableFields"/> class.
			/// </summary>
			/// <param name="table">The table.</param>
			/// <param name="fields">The fields.</param>
			/// <param name="shapeFieldExcluded">if set to <c>true</c> the shape field was excluded for the feature class.</param>
			public TableFields([NotNull] ITable table, [NotNull] string fields,
			                   bool shapeFieldExcluded)
			{
				Table = table;
				Fields = fields;
				ShapeFieldExcluded = shapeFieldExcluded;
			}

			[NotNull]
			public string Fields { get; }

			[NotNull]
			public ITable Table { get; }

			public bool ShapeFieldExcluded { get; }
		}

		#endregion

		#endregion

		#region moving to next tile

		private void LoadCachedRows(Tile tile)
		{
			//if (!PrepareNextTile()) return false;

			for (var cachedTableIndex = 0;
			     cachedTableIndex < _cachedTableCount;
			     cachedTableIndex++)
			{
				ITable cachedTable = _cachedTables[cachedTableIndex];

				using (_container.UseProgressWatch(
					Step.DataLoading, Step.DataLoaded, cachedTableIndex, _cachedTableCount,
					cachedTable))
				{
					LoadCachedTableRows(cachedTable, cachedTableIndex, tile);
				}
			}

			_tileCache.SetCurrentTileBox(tile.Box);

			//InitializeTile();
			//// TestUtils.AddGarbageCollectionRequest();
			//_currentTileIndex++;
		}

		private class Tile
		{
			private readonly IEnvelope _filterEnvelope;
			private readonly Box _box;
			private readonly ISpatialFilter _filter;

			public Tile(double tileXMin, double tileYMin, double tileXMax, double tileYMax,
			            ISpatialReference spatialReference, int totalTileCount)
			{
				_filterEnvelope = new EnvelopeClass();
				_filterEnvelope.PutCoords(tileXMin, tileYMin, tileXMax, tileYMax);
				_filterEnvelope.SpatialReference = spatialReference;

				_box = new Box(new Pnt2D(tileXMin, tileYMin), new Pnt2D(tileXMax, tileYMax));

				_filter = new SpatialFilterClass();
				_filter.Geometry = _filterEnvelope;
				_filter.SpatialRel = totalTileCount == 1
					                     ? esriSpatialRelEnum.esriSpatialRelIntersects
					                     : esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}

			public Box Box => _box;
			public IEnvelope FilterEnvelope => _filterEnvelope;
			public ISpatialFilter SpatialFilter => _filter;
		}

		private IEnumerable<Tile> EnumTiles()
		{
			double tileXMin = _testRunBox.Min.X;
			double tileYMin = _testRunBox.Min.Y;

			int totalTileCount = GetTotalTileCount();

			IEnvelope testRunEnvelope = GetTestRunEnvelope();
			IEnvelope previousTileEnvelope = GetInitialTileEnvelope();

			_container.CompleteTile(
				TileState.Initial, previousTileEnvelope, testRunEnvelope,
				_tileCache.OverlappingFeatures);

			int currentTileIndex = 0;
			for (int i = 0; i < totalTileCount; i++)
			{
				_container.OnProgressChanged(Step.TileProcessed, currentTileIndex,
				                             totalTileCount,
				                             previousTileEnvelope, testRunEnvelope);

				Tile tile = GetTile(tileXMin, tileYMin, totalTileCount);

				_tileCache.OverlappingFeatures.SetCurrentTile(tile.Box);
				LoadCachedRows(tile);

				_container.OnProgressChanged(Step.TileProcessing,
				                             currentTileIndex + 1, totalTileCount,
				                             tile.FilterEnvelope, testRunEnvelope);

				_container.BeginTile(tile.FilterEnvelope, testRunEnvelope);

				yield return tile;

				previousTileEnvelope = tile.FilterEnvelope;
				TileState state = currentTileIndex + 1 < totalTileCount
					                  ? TileState.Progressing
					                  : TileState.Final;
				_container.CompleteTile(
					state, previousTileEnvelope, testRunEnvelope, _tileCache.OverlappingFeatures);

				tileXMin = tile.Box.Max.X;
				if (tileXMin >= _testRunBox.Max.X)
				{
					tileXMin = _testRunBox.Min.X;
					tileYMin = tile.Box.Max.Y;
				}

				if (tileYMin > _testRunBox.Max.Y)
				{
					ClearDelegates();
					yield break;
				}

				currentTileIndex++;
			}

			ClearDelegates();
		}

		private IEnumerable<TestRow> EnumRows(Tile tile)
		{
			int cachedRowCount = GetTileCachedTablesRowCount();
			int nonCachedRowCount = GetTileNonCachedTablesRowCount(tile.SpatialFilter);
			int rasterRowCount = GetTileRasterRowCount(tile);

			int tileRowCount = cachedRowCount;
			tileRowCount += nonCachedRowCount;
			tileRowCount += rasterRowCount;
			tileRowCount += GetTileTerrainRowCount(tile);

			int preRowCount = 0;
			foreach (var cachedRow in EnumCachedRows(tile, preRowCount, tileRowCount))
			{
				yield return cachedRow;
			}

			preRowCount = cachedRowCount;

			foreach (var nonCachedRow in EnumNonCachedRows(tile, preRowCount, tileRowCount))
			{
				yield return nonCachedRow;
			}

			preRowCount += nonCachedRowCount;

			foreach (var rasterRow in EnumRasterRows(tile, preRowCount, tileRowCount))
			{
				yield return rasterRow;
			}

			preRowCount += rasterRowCount;

			foreach (var tinRow in EnumTinRows(tile, preRowCount, tileRowCount))
			{
				yield return tinRow;
			}
		}

		[NotNull]
		private Tile GetTile(double tileXMin, double tileYMin, int totalTileCount)
		{
			double tileXMax = tileXMin + _tileSize;
			double tileYMax = tileYMin + _tileSize;

			if (_terrainRowEnumerables != null && _terrainRowEnumerables.Count > 0)
			{
				// TODO why take the first?
				const int firstTerrainIndex = 0;
				IBox terrainFirstTileExtent =
					_terrainRowEnumerables[firstTerrainIndex].FirstTerrainBox;

				double terrainTileSize = terrainFirstTileExtent.Max.X -
				                         terrainFirstTileExtent.Min.X;

				if (terrainTileSize < _tileSize)
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
			                     Math.Min(tileXMax, _testRunBox.Max.X),
			                     Math.Min(tileYMax, _testRunBox.Max.Y),
			                     CurrentTileSpatialReference,
			                     totalTileCount);

			return tile;
		}

		[NotNull]
		private IEnvelope GetTestRunEnvelope()
		{
			IEnvelope result = new EnvelopeClass();

			result.PutCoords(_testRunBox.Min.X, _testRunBox.Min.Y,
			                 _testRunBox.Max.X, _testRunBox.Max.Y);

			return result;
		}

		[NotNull]
		private IEnvelope GetInitialTileEnvelope()
		{
			IEnvelope result = new EnvelopeClass();

			// no current tile box yet - create zero-sized box at minx, miny of test run box
			result.PutCoords(_testRunBox.Min.X, _testRunBox.Min.Y,
			                 _testRunBox.Min.X, _testRunBox.Min.Y);
			result.SpatialReference = CurrentTileSpatialReference;

			return result;
		}

		#endregion

		#region loading the cache

		private void LoadCachedTableRows([NotNull] ITable table,
		                                 int tableIndex,
		                                 [NotNull] Tile tile)
		{
			IBox allBox = null;

			IDictionary<BaseRow, CachedRow> cachedRows =
				_tileCache.OverlappingFeatures.GetOverlappingCachedRows(table, tile.Box);

			int previousCachedRowCount = cachedRows.Count;

			// avoid rereading overlapping large features (with a max extent > tile size)
			double maxExtent = _tileSize;
			// TODO: use more detailed info ignore / improve notInExpression
			bool isQueryTable = ((IDataset) table).FullName is IQueryName2;
			string notInExpression =
				isQueryTable
					? string.Empty
					: GetFilterOldLargeRows(cachedRows.Values, maxExtent, ref allBox);

			ISpatialFilter loadSpatialFilter =
				GetLoadSpatialFilter(table, tile.SpatialFilter, notInExpression);

			AddRowsToCache(cachedRows, table, loadSpatialFilter, _uniqueIdProviders[tableIndex],
			               ref allBox);

			UpdateXYOccurance(cachedRows.Values, tile);

			_tileCache.CreateBoxTree(tableIndex, cachedRows.Values, allBox);

			if (_loadedRowCountPerTable != null)
			{
				int newlyLoadedRows = cachedRows.Count - previousCachedRowCount;

				_loadedRowCountPerTable[table] += newlyLoadedRows;
			}

			_tileCache.IgnoredRowsByTableAndTest[tableIndex] =
				GetIgnoredRows(table, cachedRows.Values, tile.SpatialFilter.Geometry);

			Marshal.ReleaseComObject(loadSpatialFilter);
		}

		private void UpdateXYOccurance(IEnumerable<CachedRow> cachedRows, Tile tile)
		{
			double tileEpsilon = _tileSize / 1000;
			bool notFirstTileColumn = tile.Box.Min.X > (_testRunBox.Min.X + tileEpsilon);
			bool notFirstTileRow = tile.Box.Min.Y > (_testRunBox.Min.Y + tileEpsilon);

			foreach (CachedRow cachedRow in cachedRows)
			{
				Box cachedRowExtent = cachedRow.Extent;

				// cachedRow may exist for several tile-rows 

				//  _currentTileBox.Min.X is reset for each tile row 
				//    -> need to reset IsFirstOccurencyX to true in new tile rows
				if (notFirstTileColumn)
				{
					if (cachedRowExtent.Min.X < tile.Box.Min.X)
					{
						// the feature must have been encountered by a tile to the left
						cachedRow.IsFirstOccurrenceX = false;
					}
					else
					{
						cachedRow.IsFirstOccurrenceX = true;
					}
				}
				else
				{
					cachedRow.IsFirstOccurrenceX = true;
				}

				//  _currentTileBox.Min.Y is steadily growing -> no need to reset IsFirstOccurencyY to true
				if (notFirstTileRow)
				{
					if (cachedRowExtent.Min.Y < tile.Box.Min.Y)
					{
						// the feature must have been encountered by a tile to the bottom
						cachedRow.IsFirstOccurrenceY = false;
					}
				}
			}
		}

		/// <summary>
		/// returns a spatial filter
		/// with geometry = tileExtent expanded by searchTolerance('table')
		/// and whereclause
		/// </summary>
		[NotNull]
		private ISpatialFilter GetLoadSpatialFilter(
			[NotNull] ITable table,
			[NotNull] ISpatialFilter tileSpatialFilter,
			[CanBeNull] string notInExpression)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(tileSpatialFilter, nameof(tileSpatialFilter));

			var result = (ISpatialFilter) ((IClone) tileSpatialFilter).Clone();

			result.WhereClause = _container.FilterExpressionsUseDbSyntax
				                     ? _commonFilterExpressions[table]
				                     : string.Empty;

			if (StringUtils.IsNotEmpty(notInExpression))
			{
				result.WhereClause = string.IsNullOrEmpty(result.WhereClause)
					                     ? notInExpression
					                     : result.WhereClause + " AND " + notInExpression;
			}

			double searchTolerance = _tileCache.OverlappingFeatures.GetSearchTolerance(table);

			if (searchTolerance > 0)
			{
				var loadEnvelope = (IEnvelope) ((IClone) tileSpatialFilter.Geometry).Clone();

				loadEnvelope.Expand(searchTolerance, searchTolerance, false);

				const bool filterOwnsGeometry = true;
				result.set_GeometryEx(loadEnvelope, filterOwnsGeometry);
			}

			return result;
		}

		[NotNull]
		private static string GetFilterOldLargeRows(
			[NotNull] IEnumerable<CachedRow> cachedRows,
			double maxExtent,
			[CanBeNull] ref IBox allBox)
		{
			StringBuilder sb = null;

			foreach (CachedRow cachedRow in cachedRows)
			{
				if (cachedRow.Extent.GetMaxExtent() > maxExtent && cachedRow.HasFeatureCached())
				{
					IFeature feature = cachedRow.Feature;

					if (sb == null)
					{
						sb = new StringBuilder(
							string.Format("{0} NOT IN ({1}",
							              feature.Table.OIDFieldName, feature.OID));
					}
					else
					{
						sb.AppendFormat(",{0}", feature.OID);
					}
				}

				if (allBox == null)
				{
					allBox = cachedRow.Extent.Clone();
				}
				else
				{
					allBox.Include(cachedRow.Extent);
				}
			}

			if (sb != null)
			{
				sb.Append(")");
			}

			return string.Format("{0}", sb);
		}

		private void AddRowsToCache([NotNull] IDictionary<BaseRow, CachedRow> cachedRows,
		                            [NotNull] ITable table,
		                            [NotNull] ISpatialFilter filter,
		                            [CanBeNull] UniqueIdProvider uniqueIdProvider,
		                            [CanBeNull] ref IBox allBox)
		{
			// gather all cached rows that currently have no cached feature
			// (the feature was released due to the max cached point count limit)
			var rowsWithoutCachedFeature = new List<CachedRow>();
			foreach (CachedRow cachedRow in cachedRows.Values)
			{
				if (! cachedRow.HasFeatureCached())
				{
					rowsWithoutCachedFeature.Add(cachedRow);
				}
			}

			// get data from database
			foreach (IRow row in GetRows(table, filter))
			{
				var feature = (IFeature) row;
				IGeometry shape = feature.Shape;

				if (shape == null || shape.IsEmpty)
				{
					_msg.DebugFormat(
						"Skipping feature with null/empty geometry (not added to tile cache): {0}",
						GdbObjectUtils.ToString(feature));

					continue;
				}

				var keyRow = new CachedRow(feature, uniqueIdProvider);
				CachedRow cachedRow;
				if (cachedRows.TryGetValue(keyRow, out cachedRow))
				{
					cachedRow.UpdateFeature(feature, uniqueIdProvider);
				}
				else
				{
					cachedRow = new CachedRow(feature, uniqueIdProvider);
					// cachedRow must always be added to cachedRows, even if disjoint !
					// otherwise it may be processed several times
					cachedRows.Add(keyRow, cachedRow);
				}

				IBox cachedRowExtent = cachedRow.Extent;

				bool disjoint = IsDisjointFromExecuteArea(feature.Shape);

				cachedRow.DisjointFromExecuteArea = disjoint;

				if (allBox == null)
				{
					allBox = cachedRowExtent.Clone();
				}
				else
				{
					allBox.Include(cachedRowExtent);
				}
			}

			foreach (CachedRow cachedRow in rowsWithoutCachedFeature)
			{
				if (! cachedRow.HasFeatureCached())
				{
					// the missing feature was not restored during the load
					// this would probably be some edge case related to snapped/non-snapped coordinates
					cachedRows.Remove(cachedRow);
				}
			}
		}

		/// <summary>
		/// Workaround : Shapefile-Tables may return wrong feature is querying with esriSpatialRelEnvelopeIntersects
		/// </summary>
		[NotNull]
		private static IEnumerable<IRow> GetRows([NotNull] ITable table,
		                                         [NotNull] ISpatialFilter filter)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(filter, nameof(filter));

			IWorkspace workspace = ((IDataset) table).Workspace;
			esriSpatialRelEnum origRel = filter.SpatialRel;

			var changedSpatialRel = false;

			try
			{
				if (origRel == esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects &&
				    workspace.Type == esriWorkspaceType.esriFileSystemWorkspace)
				{
					filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
					changedSpatialRel = true;
				}

				const bool recycle = false;
				return new EnumCursor(table, filter, recycle);
			}
			finally
			{
				if (changedSpatialRel)
				{
					filter.SpatialRel = origRel;
				}
			}
		}

		[NotNull]
		private List<IList<BaseRow>> GetIgnoredRows(
			[NotNull] ITable table,
			[NotNull] ICollection<CachedRow> cachedRows,
			[NotNull] IGeometry tileGeometry)
		{
			var result = new List<IList<BaseRow>>();

			foreach (ContainerTest test in _testsPerTable[table])
			{
				List<BaseRow> ignoreList = null;

				if (test.AreaOfInterest != null &&
				    ! ((IRelationalOperator) test.AreaOfInterest).Contains(tileGeometry))
				{
					foreach (CachedRow cachedRow in cachedRows)
					{
						if (! test.IsOutsideAreaOfInterest(cachedRow.Feature))
						{
							continue;
						}

						if (ignoreList == null)
						{
							ignoreList = new List<BaseRow>();
						}

						ignoreList.Add(cachedRow);
					}
				}

				result.Add(ignoreList);
			}

			return result;
		}

		private bool IsDisjointFromExecuteArea([NotNull] IGeometry geometry)
		{
			return _executePolygon != null && _executePolygonRelOp.Disjoint(geometry);
		}

		#endregion
	}
}
