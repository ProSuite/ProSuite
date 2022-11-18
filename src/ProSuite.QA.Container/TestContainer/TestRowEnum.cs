using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	// TODO revise MoveNextCachedRow (IsFirstOccurrence assignment)
	internal class TestRowEnum : IDataContainer, IDisposable, ITileEnumContext
	{
		#region Fields

		private readonly bool _nothingToDo;

		private readonly int _cachedTableCount;
		private readonly IDictionary<IReadOnlyTable, double> _cachedSet;
		private readonly IList<TableFields> _nonCachedTables;

		private readonly ITestContainer _container;
		private readonly IEnvelope _executeEnvelope;
		private readonly IPolygon _executePolygon;
		private readonly IRelationalOperator _executePolygonRelOp;
		private readonly TileCache _tileCache;
		private readonly OverlappingFeatures _overlappingFeatures;

		private readonly IDictionary<IReadOnlyTable, UniqueIdProvider> _uniqueIdProviders;
		private readonly Dictionary<IReadOnlyTable, string> _commonFilterExpressions;

		private readonly IList<TerrainRowEnumerable> _terrainRowEnumerables;
		private readonly RastersRowEnumerable _rastersRowEnumerable;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly Dictionary<IReadOnlyTable, long> _totalRowCountPerTable;
		// might be used later

		private readonly Dictionary<IReadOnlyTable, long> _loadedRowCountPerTable;

		private readonly TestSorter _testSorter;
		private readonly TileEnum _tileEnum;

		// enumerator state

		// features whose box intersect the box + tolerance 

		// of the current row (per table)

		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

			_testSorter = new TestSorter(container.ContainerTests);

			if (executePolygon != null)
			{
				_executePolygonRelOp = (IRelationalOperator) executePolygon;
			}

			foreach (ContainerTest containerTest in _container.ContainerTests)
			{
				containerTest.SetDataContainer(this);
			}

			_rastersRowEnumerable =
				new RastersRowEnumerable(_testSorter.TestsPerRaster.Keys, _container, tileSize);

			ClassifyTables(_testSorter.TestsPerTable,
			               out Dictionary<IReadOnlyTable, double> cachedSet,
			               out IList<TableFields> nonCachedTables);
			AddProcessorTables(_container.ContainerTests, cachedSet);
			_nonCachedTables =
				new List<TableFields>(nonCachedTables.Where(x => ! cachedSet.ContainsKey(x.Table)));

			_cachedSet = cachedSet;

			_cachedTableCount = _cachedSet.Count;

			if (_testSorter.TestsPerTable.Count == 0 && _testSorter.TestsPerTerrain.Count == 0)
			{
				_nothingToDo = true;
				return;
			}

			_terrainRowEnumerables = _testSorter.PrepareTerrains(_container);
			IBox terrainFirstTileExtent = null;
			if (_terrainRowEnumerables?.Count > 0)
			{
				// TODO why take the first?
				const int firstTerrainIndex = 0;
				terrainFirstTileExtent =
					_terrainRowEnumerables[firstTerrainIndex].FirstTerrainBox;
			}

			_tileEnum = new TileEnum(
				_testSorter, executeEnvelope, tileSize, container.SpatialReference,
				terrainFirstTileExtent);

			if (_tileEnum.TestRunBox == null ||
			    Math.Abs(_tileEnum.TestRunBox.GetMaxExtent()) < double.Epsilon)
			{
				_nothingToDo = true;
				return;
			}

			_tileCache = GetTileCache();
			_overlappingFeatures = InitOverlappingFeatures();

			_uniqueIdProviders = new ConcurrentDictionary<IReadOnlyTable, UniqueIdProvider>();
			foreach (IReadOnlyTable cachedTable in _cachedSet.Keys)
			{
				_uniqueIdProviders.Add(cachedTable, UniqueIdProviderFactory.Create(cachedTable));
			}

			_commonFilterExpressions = GetCommonFilterExpressions(_testSorter.TestsPerTable);

			if (_container.CalculateRowCounts)
			{
				int tableCount = _testSorter.TestsPerTable.Count;

				_totalRowCountPerTable = new Dictionary<IReadOnlyTable, long>(tableCount);
				_loadedRowCountPerTable = new Dictionary<IReadOnlyTable, long>(tableCount);

				CalculateRowCounts(
					_testSorter.TestsPerTable.Keys,
					_totalRowCountPerTable, _loadedRowCountPerTable);
			}

			InitDelegates();
		}

		private OverlappingFeatures InitOverlappingFeatures()
		{
			OverlappingFeatures overlappingFeatures =
				new OverlappingFeatures(_container.MaxCachedPointCount);

			foreach (KeyValuePair<IReadOnlyTable, double> pair in _cachedSet)
			{
				overlappingFeatures.AdaptSearchTolerance(pair.Key, pair.Value);
			}

			foreach (IList<ContainerTest> tests in _testSorter.TestsPerTable.Values)
			{
				foreach (ContainerTest containerTest in tests)
				{
					if (Math.Abs(containerTest.SearchDistance) < double.Epsilon)
					{
						continue;
					}

					foreach (IReadOnlyTable involvedTable in containerTest.InvolvedTables)
					{
						if (! _cachedSet.ContainsKey(involvedTable))
						{
							continue;
						}

						overlappingFeatures.AdaptSearchTolerance(
							involvedTable, containerTest.SearchDistance);
					}
				}
			}

			return overlappingFeatures;
		}

		private TileCache GetTileCache()
		{
			TileCache tileCache = new TileCache(
				new List<IReadOnlyTable>(_cachedSet.Keys), _tileEnum.TestRunBox, _container,
				_testSorter.TestsPerTable);

			return tileCache;
		}

		#endregion

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

				foreach (var testRow in EnumRows(tile, _tileCache))
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

		UniqueIdProvider IDataContainer.GetUniqueIdProvider(IReadOnlyTable table)
		{
			return _uniqueIdProviders[table];
		}

		IEnumerable<IReadOnlyRow> IDataContainer.Search(IReadOnlyTable table,
		                                                IQueryFilter queryFilter,
		                                                QueryFilterHelper filterHelper,
		                                                IGeometry cacheGeometry)
		{
			return Search(table, queryFilter, filterHelper, cacheGeometry);
		}

		WKSEnvelope IDataContainer.CurrentTileExtent
		{
			get
			{
				IBox b = _tileCache?.CurrentTileBox;
				if (b == null)
				{
					return new WKSEnvelope();
				}

				WKSEnvelope ext =
					WksGeometryUtils.CreateWksEnvelope(b.Min.X, b.Min.Y, b.Max.X, b.Max.Y);
				return ext;
			}
		}

		IEnvelope IDataContainer.GetLoadedExtent(IReadOnlyTable table)
		{
			return _tileCache?.GetLoadedExtent(table);
		}

		double IDataContainer.GetSearchTolerance(IReadOnlyTable table)
		{
			return _overlappingFeatures.GetSearchTolerance(table);
		}

		#endregion

		#region Searching the cache

		[CanBeNull]
		private IEnumerable<IReadOnlyRow> Search([NotNull] IReadOnlyTable table,
		                                         [NotNull] IQueryFilter queryFilter,
		                                         [NotNull] QueryFilterHelper filterHelper,
		                                         [CanBeNull] IGeometry cacheGeometry)
		{
			// if the table was not passed to the container, return null
			// to trigger a search in the database
			if (! _cachedSet.ContainsKey(table))
			{
				return null;
			}

			if ((queryFilter is ITileFilter tf && tf.TileExtent != null) || filterHelper.FullGeometrySearch)
			{
				if (queryFilter is ISpatialFilter sf)
				{
					IEnvelope loaded = _tileCache.GetLoadedExtent(table);
					if (sf.Geometry != null &&
					    ! ((IRelationalOperator) loaded).Contains(sf.Geometry))
					{
						_tilesAdmin = _tilesAdmin ?? new TilesAdmin(this, _tileCache);
						return _tilesAdmin.Search(table, sf, filterHelper);
					}
				}
			}

			return _tileCache.Search(table, queryFilter, filterHelper,
			                         cacheGeometry);
		}

		private TilesAdmin _tilesAdmin;

		#endregion

		~TestRowEnum()
		{
			Dispose(false);
		}

		private void InitDelegates()
		{
			foreach (ContainerTest containerTest in _container.ContainerTests)
			{
				_container.SubscribeTestEvents(containerTest);

				containerTest.SetDataContainer(this);
				containerTest.IgnoreUndirected = true; //  ! _container.UseIstQuality;
			}
		}

		private void ClearDelegates()
		{
			foreach (ContainerTest containerTest in _container.ContainerTests)
			{
				containerTest.SetDataContainer(null);

				_container.UnsubscribeTestEvents(containerTest);
			}
		}

		private void CalculateRowCounts(
			IEnumerable<IReadOnlyTable> tables,
			[NotNull] IDictionary<IReadOnlyTable, long> totalRowCountPerTable,
			[NotNull] IDictionary<IReadOnlyTable, long> loadedRowCountPerTable)
		{
			IQueryFilter filter = new QueryFilterClass();

			foreach (IReadOnlyTable table in tables)
			{
				filter.WhereClause = _container.FilterExpressionsUseDbSyntax
					                     ? _commonFilterExpressions[table]
					                     : string.Empty;

				long count;
				try
				{
					count = table.RowCount(filter);
				}
				catch (Exception exp)
				{
					throw new DataException(EnumCursor.CreateMessage(table.Name, filter), exp);
				}

				totalRowCountPerTable.Add(table, count);
				loadedRowCountPerTable.Add(table, 0);
			}
		}

		/// <summary>
		/// Iterate cached rows that are at least partly within the current tile
		/// </summary>
		private IEnumerable<TestRow> EnumCachedRows(Tile tile, TileCache tileCache,
		                                            long tileRowIndex, long tileRowCount)
		{
			foreach (var pair in _cachedSet)
			{
				double? cachedRowSearchTolerance = null;

				IReadOnlyTable cachedTable = pair.Key;
				_testSorter.TestsPerTable.TryGetValue(
					cachedTable, out IList<ContainerTest> testsPerTable);

				foreach (BoxTree<CachedRow>.TileEntry entry in tileCache.EnumEntries(
					         cachedTable, tile.Box))
				{
					tileRowIndex++;

					cachedRowSearchTolerance = cachedRowSearchTolerance ??
					                           _overlappingFeatures.GetSearchTolerance(
						                           cachedTable);

					CachedRow cachedRow = entry.Value;
					if (testsPerTable == null)
					{
						_overlappingFeatures.RegisterTestedFeature(
							cachedRow, cachedRowSearchTolerance.Value, null);
						continue;
					}

					if (cachedRow.DisjointFromExecuteArea)
					{
						continue;
					}

					// TODO: use new class Class_with_ContainerTest_and_InvolvedTableIndex instead of containerTest for applicable tests
					IList<ContainerTest> applicableTests = GetApplicableTests(
						testsPerTable, cachedRow.Feature, tile.SpatialFilter.Geometry,
						out IList<ContainerTest> reducedTests);

					_overlappingFeatures.RegisterTestedFeature(
						cachedRow, cachedRowSearchTolerance.Value, reducedTests);

					TestRow cachedTestRow =
						new TestRow(new RowReference(cachedRow.Feature, recycled: false),
						            entry.Box, applicableTests);

					_container.OnProgressChanged(Step.TestRowCreated, (int) tileRowIndex,
					                             (int) tileRowCount, cachedTestRow);

					yield return cachedTestRow;
				}
			}
		}

		private IEnumerable<TestRow> EnumNonCachedRows(Tile tile, long tileRowIndex,
		                                               long tileRowCount)
		{
			foreach (TableFields tableFields in _nonCachedTables)
			{
				IReadOnlyTable table = tableFields.Table;
				string origFields = tile.SpatialFilter.SubFields;

				tile.SpatialFilter.WhereClause = _container.FilterExpressionsUseDbSyntax
					                                 ? _commonFilterExpressions[table]
					                                 : string.Empty;

				try
				{
					tile.SpatialFilter.SubFields = tableFields.Fields;

					IList<ContainerTest> currentTests =
						_testSorter.TestsPerTable[table];

					foreach (IReadOnlyRow row in table.EnumRows(tile.SpatialFilter, recycle: true))
					{
						IList<ContainerTest> applicableTests = GetApplicableTests(
							currentTests, row, tile.SpatialFilter.Geometry,
							out IList<ContainerTest> reducedTests);

						var feature = row as IReadOnlyFeature;
						if (feature != null)
						{
							bool shapeFieldExcluded = tableFields.ShapeFieldExcluded;
							if (! shapeFieldExcluded)
							{
								BaseRow cachedRow = new SimpleBaseRow(feature);
								_overlappingFeatures.RegisterTestedFeature(
									cachedRow, 0, reducedTests);
							}
						}

						TestRow nonCachedTestRow = new TestRow(
							new RowReference(row, recycled: true), null,
							applicableTests);
						_container.OnProgressChanged(Step.TestRowCreated, (int) tileRowIndex,
						                             (int) tileRowCount,
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
			[NotNull] IReadOnlyRow row,
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

		private bool IsFullyTested([NotNull] IReadOnlyRow row, [NotNull] ContainerTest test)
		{
			var feature = row as IReadOnlyFeature;

			IReadOnlyTable table = row.Table;
			if (table == null || feature == null)
			{
				return false;
			}

			if (! _overlappingFeatures.WasAlreadyTested(feature, test))
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

		private IEnumerable<TestRow> EnumRasterRows(Tile tile, long tileRowIndex, long tileRowCount)
		{
			if (_rastersRowEnumerable == null)
			{
				yield break;
			}

			foreach (RasterRow rasterRow in _rastersRowEnumerable.GetRasterRows(tile.Box))
			{
				TestRow rasterTestRow = new TestRow(
					rasterRow,
					QaGeometryUtils.CreateBox(rasterRow.Extent),
					_testSorter.TestsPerRaster[rasterRow.RasterReference]);

				// _container.OnProgressChanged(string.Format("Loaded TIN Part {0} of {1} for current Tile", _cachedRowIndex, nTiles));

				_container.OnProgressChanged(Step.TestRowCreated, (int) tileRowIndex,
				                             (int) tileRowCount, rasterTestRow);

				yield return rasterTestRow;

				tileRowIndex++;
			}
		}

		private IEnumerable<TestRow> EnumTinRows(Tile tile, long tileRowIndex, long tileRowCount)
		{
			foreach (TerrainRowEnumerable terrainRowEnumerable in _terrainRowEnumerables)
			{
				foreach (TerrainRow terrainRow in terrainRowEnumerable.GetTerrainRows(tile.Box))
				{
					IList<ContainerTest> tests =
						_testSorter.TestsPerTerrain[terrainRow.TerrainReference];

					TestRow terrainTestRow = new TestRow(
						terrainRow, QaGeometryUtils.CreateBox(terrainRow.Extent), tests);

					_container.OnProgressChanged(Step.TestRowCreated, (int) tileRowIndex,
					                             (int) tileRowCount, terrainTestRow);

					yield return terrainTestRow;

					tileRowIndex++;
				}
			}
		}

		private static bool IsTestSpatiallyApplicable([NotNull] ContainerTest test,
		                                              [NotNull] IGeometry tileGeometry,
		                                              [NotNull] IReadOnlyRow row)
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
		private long GetTileNonCachedTablesRowCount(
			[NotNull] ISpatialFilter tileSpatialFilter)
		{
			long result = 0;

			foreach (TableFields tableFields in _nonCachedTables)
			{
				IReadOnlyTable table = tableFields.Table;

				string origFields = tileSpatialFilter.SubFields;
				string origWhereClause = tileSpatialFilter.WhereClause;

				try
				{
					var queryName = table.FullName as IQueryName2;
					if (queryName != null && table is IReadOnlyFeatureClass)
					{
						// Workaround for TOP-4975: crash for certain joins/extents if OID field 
						// (which was incorrectly changed by IName.Open()!) is used as only subfields field
						// Note: when not crashing, the resulting row count was incorrect when that OID field was used.
						tileSpatialFilter.SubFields =
							((IReadOnlyFeatureClass) table).ShapeFieldName;
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
							EnumCursor.CreateMessage(table.Name, tileSpatialFilter), exp);
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
		private static Dictionary<IReadOnlyTable, string> GetCommonFilterExpressions(
			[NotNull] ICollection<KeyValuePair<IReadOnlyTable, IList<ContainerTest>>> testsPerTable)
		{
			Assert.ArgumentNotNull(testsPerTable, nameof(testsPerTable));

			var result = new Dictionary<IReadOnlyTable, string>(testsPerTable.Count);

			foreach (KeyValuePair<IReadOnlyTable, IList<ContainerTest>> pair in testsPerTable)
			{
				IReadOnlyTable table = pair.Key;

				result.Add(table, GetCommonFilterExpression(table, tests: pair.Value));
			}

			return result;
		}

		[CanBeNull]
		private static string GetCommonFilterExpression(
			[NotNull] IReadOnlyTable table,
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
			[NotNull] IDictionary<IReadOnlyTable, IList<ContainerTest>> testsPerTable,
			[NotNull] out Dictionary<IReadOnlyTable, double> cachedTables,
			[NotNull] out IList<TableFields> nonCachedTables)
		{
			Assert.ArgumentNotNull(testsPerTable, nameof(testsPerTable));

			cachedTables = new Dictionary<IReadOnlyTable, double>();
			nonCachedTables = new List<TableFields>();

			foreach (KeyValuePair<IReadOnlyTable, IList<ContainerTest>> pair in testsPerTable)
			{
				IReadOnlyTable table = pair.Key;

				AddRecursive(table, cachedTables);

				IList<ContainerTest> tests = pair.Value;
				GetNeeds(table, tests, out bool queried, out bool geometryUsed);

				if (queried)
				{
					if (! cachedTables.TryGetValue(table, out double searchDist))
					{
						searchDist = 0;
					}

					cachedTables[table] = Math.Max(searchDist, 0); // TODO
				}
				else
				{
					// if the geometry is not to be used for a feature class,
					// don't get the shape field. Exception: Access Gdb (spatial queries
					// do not work there if shape field is missing)
					bool excludeShapeField = ! geometryUsed && table is IReadOnlyFeatureClass &&
					                         ! IsInLocalDatabaseWorkspace(table);

					const string allFields = "*";

					string subFields = excludeShapeField
						                   ? GetFieldNamesExcludingShapeField(
							                   (IReadOnlyFeatureClass) table)
						                   : allFields;

					nonCachedTables.Add(new TableFields(table, subFields,
					                                    excludeShapeField));
				}
			}
		}

		private static void AddRecursive(IReadOnlyTable table,
		                                 Dictionary<IReadOnlyTable, double> cachedTables)
		{
			if (! (table is IDataContainerAware transformed))
			{
				return;
			}

			foreach (var baseTable in transformed.InvolvedTables)
			{
				AddRecursive(baseTable, cachedTables);
				if ((baseTable as ITransformedTable)?.NoCaching == true)
				{
					continue;
				}

				if (! (baseTable is IReadOnlyFeatureClass))
				{
					continue;
				}

				if (! cachedTables.TryGetValue(baseTable, out double searchDist))
				{
					searchDist = 0;
				}

				double search = transformed is IHasSearchDistance has ? has.SearchDistance : 0;
				cachedTables[baseTable] = Math.Max(searchDist, search);
			}
		}

		private void AddProcessorTables(IEnumerable<ContainerTest> tests,
		                                Dictionary<IReadOnlyTable, double> cachedSet)
		{
			foreach (var test in tests)
			{
				if (test.IssueFilters != null)
				{
					foreach (IIssueFilter filter in test.IssueFilters)
					{
						foreach (IReadOnlyTable table in filter.InvolvedTables)
						{
							AddRecursive(table, cachedSet);
							if (! cachedSet.TryGetValue(table, out double searchDist))
							{
								searchDist = 0;
							}

							double filterSearch =
								filter is IHasSearchDistance has ? has.SearchDistance : 0;
							cachedSet[table] = Math.Max(searchDist, filterSearch);
						}

						if (filter is IssueFilter f)
						{
							f.DataContainer = this;
						}
					}
				}

				for (int iInvolved = 0; iInvolved < test.InvolvedTables.Count; iInvolved++)
				{
					foreach (IRowFilter filter in test.GetRowFilters(iInvolved))
					{
						foreach (IReadOnlyTable table in filter.InvolvedTables)
						{
							AddRecursive(table, cachedSet);
							if (! cachedSet.TryGetValue(table, out double searchDist))
							{
								searchDist = 0;
							}

							double filterSearch =
								filter is IHasSearchDistance has ? has.SearchDistance : 0;
							cachedSet[table] = Math.Max(searchDist, filterSearch);
						}

						if (filter is RowFilter f)
						{
							f.DataContainer = this;
						}
					}
				}
			}
		}

		private static void GetNeeds(
			IReadOnlyTable table, IEnumerable<ContainerTest> tests,
			out bool queried, out bool geometryUsed)
		{
			queried = false;
			geometryUsed = false;

			foreach (ContainerTest test in tests)
			{
				IList<IReadOnlyTable> tableList = test.InvolvedTables;
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

			if ((table as ITransformedTable)?.NoCaching == true)
			{
				queried = false;
			}
		}

		private static bool IsInLocalDatabaseWorkspace([NotNull] IReadOnlyTable table)
		{
			return table.Workspace.Type == esriWorkspaceType.esriLocalDatabaseWorkspace;
		}

		[NotNull]
		private static string GetFieldNamesExcludingShapeField(
			[NotNull] IReadOnlyFeatureClass featureClass)
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
			public TableFields([NotNull] IReadOnlyTable table, [NotNull] string fields,
			                   bool shapeFieldExcluded)
			{
				Table = table;
				Fields = fields;
				ShapeFieldExcluded = shapeFieldExcluded;
			}

			[NotNull]
			public string Fields { get; }

			[NotNull]
			public IReadOnlyTable Table { get; }

			public bool ShapeFieldExcluded { get; }

			public override string ToString() =>
				$"{Table.Name}; {Fields}; excludeShape:{ShapeFieldExcluded}";
		}

		#endregion

		#endregion

		#region moving to next tile

		private void LoadCachedRows(Tile tile, TileCache tileCache)
		{
			TileCache preloadedCache = _tilesAdmin?.PrepareNextTile(tile);

			int cachedTableIndex = 0;
			tileCache.LoadingTileBox = tile.Box;
			foreach (IReadOnlyTable cachedTable in _cachedSet.Keys)
			{
				using (_container.UseProgressWatch(
					       Step.DataLoading, Step.DataLoaded, cachedTableIndex, _cachedTableCount,
					       cachedTable))
				{
					if (tileCache.IsLoaded(cachedTable, tile))
					{
						continue;
					}

					ICollection<CachedRow> cachedRows;
					if (preloadedCache?.IsLoaded(cachedTable, tile) == true)
					{
						cachedRows = preloadedCache.TransferCachedRows(tileCache, cachedTable);
					}
					else
					{
						cachedRows = LoadCachedTableRows(cachedTable, tile, tileCache);
					}

					PreprocessCache(cachedTable, tile, tileCache, cachedRows);
				}

				cachedTableIndex++;
			}

			tileCache.SetCurrentTileBox(tile.Box);
		}

		private IEnumerable<Tile> EnumTiles()
		{
			IEnvelope previousTileEnvelope = _tileEnum.GetInitialTileEnvelope();
			IEnvelope testRunEnvelope = _tileEnum.GetTestRunEnvelope();
			int totalTileCount = _tileEnum.GetTotalTileCount();
			_container.CompleteTile(TileState.Initial, previousTileEnvelope, testRunEnvelope,
			                        _overlappingFeatures);

			int currentTileIndex = 0;
			foreach (Tile tile in _tileEnum.EnumTiles())
			{
				_container.OnProgressChanged(Step.TileProcessed, currentTileIndex,
				                             totalTileCount,
				                             previousTileEnvelope, testRunEnvelope);

				_overlappingFeatures.SetCurrentTile(tile.Box);

				// TODO: check if tileCache for tile already created (when preloading different tiles)
				// TODO: _tileCache = _tileCache.Clone();

				LoadCachedRows(tile, _tileCache);

				_container.OnProgressChanged(Step.TileProcessing,
				                             currentTileIndex + 1, totalTileCount,
				                             tile.FilterEnvelope, testRunEnvelope);

				_container.BeginTile(tile.FilterEnvelope, testRunEnvelope);

				yield return tile;

				previousTileEnvelope = tile.FilterEnvelope;
				TileState state = currentTileIndex + 1 < totalTileCount
					                  ? TileState.Progressing
					                  : TileState.Final;
				_container.CompleteTile(state, tile.FilterEnvelope,
				                        testRunEnvelope, _overlappingFeatures);

				currentTileIndex++;
			}

			ClearDelegates();
		}

		private IEnumerable<TestRow> EnumRows(Tile tile, TileCache tileCache)
		{
			long cachedRowCount = tileCache.GetTablesRowCount();
			long nonCachedRowCount = GetTileNonCachedTablesRowCount(tile.SpatialFilter);
			int rasterRowCount = GetTileRasterRowCount(tile);

			long tileRowCount = cachedRowCount;
			tileRowCount += nonCachedRowCount;
			tileRowCount += rasterRowCount;
			tileRowCount += GetTileTerrainRowCount(tile);

			long preRowCount = 0;
			foreach (var cachedRow in EnumCachedRows(tile, tileCache, preRowCount, tileRowCount))
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

		#endregion

		#region loading the cache

		private ICollection<CachedRow> LoadCachedTableRows(
			[NotNull] IReadOnlyTable table,
			[NotNull] Tile tile,
			[NotNull] TileCache tileCache)
		{
			IDictionary<BaseRow, CachedRow> cachedRows =
				_overlappingFeatures.GetOverlappingCachedRows(table, tile.Box);
			int previousCachedRowCount = cachedRows.Count;

			tileCache.LoadCachedTableRows(cachedRows, table, tile, this);
			// LoadCachedTableRows(cachedRows, table, tile, tileCache);

			int newlyLoadedRows = cachedRows.Count - previousCachedRowCount;
			_msg.VerboseDebug(() => $"{table.Name}: Added additional {newlyLoadedRows} rows " +
			                        $"to the previous {previousCachedRowCount} rows in {tile}");

			if (_loadedRowCountPerTable != null)
			{
				_loadedRowCountPerTable[table] += newlyLoadedRows;
			}

			return cachedRows.Values;
		}

		private void PreprocessCache([NotNull] IReadOnlyTable table,
		                             [NotNull] Tile tile,
		                             [NotNull] TileCache tileCache,
		                             [NotNull] ICollection<CachedRow> cachedRows)
		{
			UpdateXYOccurance(cachedRows, tile);

			tileCache.IgnoredRowsByTableAndTest[table] =
				GetIgnoredRows(table, cachedRows, tile.SpatialFilter.Geometry);
		}

		private void UpdateXYOccurance(IEnumerable<CachedRow> cachedRows, Tile tile)
		{
			double tileEpsilon = _tileEnum.TileSize / 1000;
			bool notFirstTileColumn = tile.Box.Min.X > _tileEnum.TestRunBox.Min.X + tileEpsilon;
			bool notFirstTileRow = tile.Box.Min.Y > _tileEnum.TestRunBox.Min.Y + tileEpsilon;

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
		/// Workaround : Shapefile-Tables may return wrong feature is querying with esriSpatialRelEnvelopeIntersects
		/// </summary>
		[NotNull]
		private static IEnumerable<IReadOnlyRow> GetRows([NotNull] IReadOnlyTable table,
		                                                 [NotNull] ISpatialFilter filter)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(filter, nameof(filter));

			IWorkspace workspace = table.Workspace;
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

				return table.EnumRows(filter, recycle: false);
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
			[NotNull] IReadOnlyTable table,
			[NotNull] ICollection<CachedRow> cachedRows,
			[NotNull] IGeometry tileGeometry)
		{
			var result = new List<IList<BaseRow>>();

			if (_testSorter.TestsPerTable.TryGetValue(table, out IList<ContainerTest> tableTests))
			{
				foreach (ContainerTest test in tableTests)
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
			}

			return result;
		}

		public TileEnum TileEnum => _tileEnum;
		public double TileSize => _tileEnum.TileSize;

		public OverlappingFeatures OverlappingFeatures => _overlappingFeatures;

		string ITileEnumContext.GetCommonFilterExpression([NotNull] IReadOnlyTable table)
		{
			_commonFilterExpressions.TryGetValue(table, out string result);
			return result;
		}

		UniqueIdProvider ITileEnumContext.GetUniqueIdProvider([NotNull] IReadOnlyTable table)
		{
			_uniqueIdProviders.TryGetValue(table, out UniqueIdProvider result);
			return result;
		}

		[Obsolete("make private")]
		public bool IsDisjointFromExecuteArea([NotNull] IGeometry geometry)
		{
			return _executePolygon != null && _executePolygonRelOp.Disjoint(geometry);
		}

		#endregion
	}
}
