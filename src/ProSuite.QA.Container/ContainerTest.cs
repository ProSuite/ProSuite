using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Base class for tests running in the container
	/// </summary>
	public abstract partial class ContainerTest : TestBase, IRelatedTablesProvider, IEditProcessorTest
	{
		private readonly List<QueryFilterHelper> _filterHelpers;

		private IList<ISpatialFilter> _defaultFilters;
		private IList<QueryFilterHelper> _defaultFilterHelpers;

		private Dictionary<ITable, RelatedTables> _dictRelated;

		private IEnvelope _lastCompleteTileBox;
		private bool _completeTileInitialized;
		private double _allMaxY;
		private double _currentMaxX;
		private double _currentMaxY;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ContainerTest"/> class.
		/// </summary>
		/// <param name="tables">The tables.</param>
		protected ContainerTest([NotNull] IEnumerable<ITable> tables)
			: base(tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));

			int tableCount = InvolvedTables.Count;

			// initialise default values in list!
			_filterHelpers = new List<QueryFilterHelper>(new QueryFilterHelper[tableCount]);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ContainerTest"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		protected ContainerTest(ITable table) : this(new[] {table}) { }

		#endregion

		protected internal double TerrainTolerance { get; set; }

		public IList<RasterReference> InvolvedRasters { get; protected set; }

		public IList<TerrainReference> InvolvedTerrains { get; protected set; }

		private List<IPreProcessor> _preProcessors;
		private Dictionary<int, List<IPreProcessor>> _preProssesorDict;
		public IReadOnlyList<IPreProcessor> PreProcessors => _preProcessors;
		void IEditProcessorTest.AddPreProcessor(IPreProcessor proc)
		{
			_preProcessors = _preProcessors ?? new List<IPreProcessor>();
			_preProssesorDict = null;
			_preProcessors.Add(proc);
		}


		private Dictionary<int, List<IPreProcessor>> PreProssesorDict => _preProssesorDict ??
			(_preProssesorDict = GetPreProcessorDict(_preProcessors));

		[NotNull]
		private Dictionary<int, List<IPreProcessor>> GetPreProcessorDict(
			[CanBeNull] IList<IPreProcessor> preProcessors)
		{
			Dictionary<int, List<IPreProcessor>> dict = new Dictionary<int, List<IPreProcessor>>();
			if (preProcessors == null)
			{
				return dict;
			}

			foreach (IPreProcessor proc in preProcessors)
			{
				if (! dict.TryGetValue(proc.TableIndex, out List<IPreProcessor> procs))
				{
					procs = new List<IPreProcessor>();
					dict.Add(proc.TableIndex, procs);
				}

				procs.Add(proc);
			}

			return dict;
		}

		private List<IPostProcessor> _postProcessors;
		public IReadOnlyList<IPostProcessor> PostProcessors => _postProcessors;

		void IEditProcessorTest.AddPostProcessor(IPostProcessor proc)
		{
			_postProcessors = _postProcessors ?? new List<IPostProcessor>();
			_postProcessors.Add(proc);
		}

		public IEnumerable<IGeoDataset> GetInvolvedGeoDatasets()
		{
			IGeoDataset geoDataset;
			foreach (ITable table in InvolvedTables)
			{
				geoDataset = table as IGeoDataset;
				if (geoDataset != null)
				{
					yield return geoDataset;
				}
			}

			if (InvolvedTerrains != null)
			{
				foreach (TerrainReference terrain in InvolvedTerrains)
				{
					geoDataset = terrain.Dataset;
					yield return geoDataset;
				}
			}

			if (InvolvedRasters != null)
			{
				foreach (RasterReference raster in InvolvedRasters)
				{
					geoDataset = raster.RasterDataset as IGeoDataset;
					if (geoDataset != null)
					{
						yield return geoDataset;
					}
				}
			}
		}

		/// <summary>
		/// Gets the distance, within related objects are searched
		/// </summary>
		public double SearchDistance { get; protected set; }

		/// <summary>
		/// gets or sets whether all involved rows are tested and hence row combinations
		/// that occure in undirected order can be ignored
		/// </summary>
		[PublicAPI]
		protected internal bool IgnoreUndirected { get; set; }

		[CanBeNull]
		protected UniqueIdProvider GetUniqueIdProvider(int tableIndex)
		{
			return DataContainer?.GetUniqueIdProvider(InvolvedTables[tableIndex]);
		}

		internal ISearchable DataContainer { get; set; }

		/// <summary>
		/// Disables recycling of rows in Execute()
		/// </summary>
		[PublicAPI]
		protected bool KeepRows { get; set; }

		protected override void OnQaError(QaErrorEventArgs args)
		{
			if (_postProcessors != null)
			{
				foreach (IPostProcessor postProcessor in _postProcessors)
				{
					postProcessor.PostProcessError(args);
					if (args.Cancel)
					{
						return;
					}
				}
			}

			base.OnQaError(args);
		}

		#region ITest Members

		// TODO currently this seems to be called on container tests only when they
		//      don't have any involved feature class nor terrains (only tables without geometry)
		//      (or apparently from unit tests)
		public override int Execute()
		{
			if (AreaOfInterest != null)
			{
				return Execute(AreaOfInterest);
			}

			IEnvelope testRunEnvelope = TestUtils.GetFullExtent(GetInvolvedGeoDatasets());

			var errorCount = 0;
			errorCount += CompleteTile(new TileInfo(TileState.Initial, null, testRunEnvelope));

			BeginTile(new BeginTileParameters(testRunEnvelope, testRunEnvelope));

			var tableIndex = 0;
			foreach (ITable table in InvolvedTables)
			{
				if (! GetQueriedOnly(tableIndex))
				{
					IQueryFilter queryFilter = new QueryFilterClass();
					ConfigureQueryFilter(tableIndex, queryFilter);

					errorCount += Execute(table, tableIndex, queryFilter);
				}

				tableIndex++;
			}

			// This method will only be called from non-cached tests, and terrain tests must be cached:
			Assert.Null(InvolvedTerrains,
			            "Unexpected Execute() call for container test with terrain");

			errorCount += CompleteTile(new TileInfo(TileState.Final,
			                                        testRunEnvelope,
			                                        testRunEnvelope));

			return errorCount;
		}

		// TODO currently this seems to be called on container tests only when they
		//      don't have any involved feature class (only tables without geometry)
		public override int Execute(IEnvelope boundingBox)
		{
			Assert.ArgumentNotNull(boundingBox, nameof(boundingBox));
			Assert.ArgumentCondition(! boundingBox.IsEmpty, "bounding box is empty");

			if (AreaOfInterest != null)
			{
				// TODO revise - ok to alter AreaOfInterest?
				// TODO revise - spatial index on geometry?
				((ITopologicalOperator) AreaOfInterest).Clip(boundingBox);
				return Execute(AreaOfInterest);
			}

			var errorCount = 0;
			errorCount += CompleteTile(new TileInfo(TileState.Initial, null, boundingBox));

			BeginTile(new BeginTileParameters(boundingBox, boundingBox));

			var tableIndex = 0;
			// TODO revise - run over all tables, even if equals --> multiple runs for same row in same involved table in Execute(row)
			foreach (ITable table in InvolvedTables)
			{
				IQueryFilter filter = TestUtils.CreateFilter(boundingBox, AreaOfInterest,
				                                             GetConstraint(tableIndex),
				                                             table, null);
				errorCount += Execute(table, tableIndex, filter);

				tableIndex++;
			}

			// This method will only be called from non-cached tests, and terrain tests must be cached:
			Assert.Null(InvolvedTerrains, "Unexpected method call for ContainerTest with terrain");

			errorCount += CompleteTile(new TileInfo(TileState.Final, boundingBox, boundingBox));

			return errorCount;
		}

		// TODO currently this seems to be called on container tests only when they 
		//      don't have any involved feature class (only tables without geometry)
		//      (or apparently from unit tests)
		public override int Execute(IPolygon area)
		{
			Assert.ArgumentNotNull(area, nameof(area));
			Assert.ArgumentCondition(! area.IsEmpty, "area is empty");

			IEnvelope boundingBox = area.Envelope;
			var errorCount = 0;
			errorCount += CompleteTile(new TileInfo(TileState.Initial, null, boundingBox));
			BeginTile(new BeginTileParameters(boundingBox, boundingBox));

			var tableIndex = 0;
			foreach (ITable table in InvolvedTables)
			{
				IQueryFilter filter = TestUtils.CreateFilter(area, AreaOfInterest,
				                                             GetConstraint(tableIndex),
				                                             table, null);
				errorCount += Execute(table, tableIndex, filter);

				tableIndex++;
			}

			// This method will only be called from non-cached tests, and terrain tests must be cached:
			Assert.Null(InvolvedTerrains, "Unexpected method call for ContainerTest with terrain");

			errorCount += CompleteTile(new TileInfo(TileState.Final, boundingBox, boundingBox));

			return errorCount;
		}

		public override int Execute(IEnumerable<IRow> selectedRows)
		{
			Assert.ArgumentNotNull(selectedRows, nameof(selectedRows));

			var errorCount = 0;

			int tableCount = InvolvedTables.Count;
			var helpers = new QueryFilterHelper[tableCount];

			for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
			{
				ITable table = InvolvedTables[tableIndex];
				helpers[tableIndex] = new QueryFilterHelper(table,
				                                            GetConstraint(tableIndex),
				                                            GetSqlCaseSensitivity(tableIndex));
			}

			foreach (IRow row in selectedRows)
			{
				if (CancelTestingRow(row))
				{
					continue;
				}

				var feature = row as IFeature;
				if (feature != null && AreaOfInterest != null &&
				    ((IRelationalOperator) AreaOfInterest).Disjoint(feature.Shape))
				{
					continue;
				}

				var occurrence = 0;
				ITable table = row.Table;
				for (int tableIndex = GetTableIndex(table, occurrence);
				     tableIndex >= 0;
				     tableIndex = GetTableIndex(table, occurrence))
				{
					occurrence++;

					if (helpers[tableIndex].MatchesConstraint(row) == false)
					{
						continue;
					}

					errorCount += ExecuteCore(row, tableIndex);
				}
			}

			return errorCount;
		}

		public override int Execute(IRow row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			if (CancelTestingRow(row))
			{
				return 0;
			}

			if (row.Table == null)
			{
				return ExecuteCore(row, -1);
			}

			var errorCount = 0;
			var occurrence = 0;
			int tableIndex = GetTableIndex(row.Table, occurrence);
			while (tableIndex >= 0)
			{
				errorCount += ExecuteCore(row, tableIndex);

				occurrence++;

				tableIndex = GetTableIndex(row.Table, occurrence);
			}

			return errorCount;
		}

		#endregion

		public void AddRelatedTables([NotNull] ITable joined,
		                             [NotNull] IList<ITable> related)
		{
			Assert.ArgumentNotNull(joined, nameof(joined));
			Assert.ArgumentNotNull(related, nameof(related));

			if (_dictRelated == null)
			{
				_dictRelated = new Dictionary<ITable, RelatedTables>();
			}

			RelatedTables relatedTables = RelatedTables.Create(related, joined);
			_dictRelated.Add(joined, relatedTables);
		}

		public RelatedTables GetRelatedTables(IRow row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			return _dictRelated?[row.Table];
		}

		/// <summary>
		/// Get involved rows related to IRow
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		protected IList<InvolvedRow> GetInvolvedRows([NotNull] IRow row)
		{
			RelatedTables related = GetRelatedTables(row);

			return related?.GetInvolvedRows(row) ?? new[] {new InvolvedRow(row)};
		}

		internal int GetTableIndex([CanBeNull] ITable table, int occurrence)
		{
			// TODO there are calls with null from TestContainer.ExecuteCore()
			return GetIndex(table, InvolvedTables, occurrence);
		}

		internal int GetRasterIndex([CanBeNull] RasterReference raster, int occurrence)
		{
			// TODO there are calls with null from TestContainer.ExecuteCore()
			return GetIndex(raster, InvolvedRasters, occurrence, (x, y) => x.EqualsCore(y));
		}

		internal int GetTerrainIndex([CanBeNull] TerrainReference terrain, int occurrence)
		{
			// TODO there are calls with null from TestContainer.ExecuteCore()
			return GetIndex(terrain, InvolvedTerrains, occurrence, (x, y) => x.EqualsCore(y));
		}

		private static int GetIndex<T>([CanBeNull] T element, IList<T> list,
		                               int occurrence, Func<T, T, bool> equalsFunc = null)
			where T : class
		{
			// TODO there are calls with null from TestContainer.ExecuteCore()
			int elementIndex = -1;
			int elementCount = list.Count;

			while (occurrence >= 0)
			{
				occurrence--;
				int iStart = elementIndex + 1;
				elementIndex = -1;

				for (int i = iStart; i < elementCount; i++)
				{
					if (equalsFunc?.Invoke(list[i], element) ?? list[i] == element)
					{
						elementIndex = i;
						break;
					}
				}
			}

			return elementIndex;
		}

		/// <summary>
		/// Gets indexes at which a given table occurs as involved table in this test.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns></returns>
		[NotNull]
		internal IEnumerable<int> GetTableIndexes([NotNull] ITable table)
		{
			int tableCount = InvolvedTables.Count;

			for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
			{
				if (InvolvedTables[tableIndex] == table)
				{
					yield return tableIndex;
				}
			}
		}

		protected int GetTableIndex([NotNull] IRow row)
		{
			int tableCount = InvolvedTables.Count;

			for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
			{
				ITable table = InvolvedTables[tableIndex];

				if (table != row.Table)
				{
					continue;
				}

				if (GetConstraint(tableIndex) == null)
				{
					return tableIndex;
				}

				if (_filterHelpers[tableIndex].MatchesConstraint(row))
				{
					return tableIndex;
				}
			}

			return -1;
		}

		internal bool IsOutsideAreaOfInterest([NotNull] IRow row)
		{
			if (AreaOfInterest == null)
			{
				// no restrictions
				return false;
			}

			var feature = row as IFeature;

			if (feature == null)
			{
				return false;
			}

			IGeometry shape = feature.Shape;

			return ((IRelationalOperator) AreaOfInterest).Disjoint(shape);
		}

		/// <summary>
		/// Indicates if the corresponding table is spatially queried 
		/// by another table or itself in this test
		/// </summary>
		public virtual bool IsQueriedTable(int tableIndex)
		{
			return true;
		}

		/// <summary>
		/// Indicates if the corresponding table is spatially queried 
		/// by another table or itself in this test
		/// </summary>
		public virtual bool IsGeometryUsedTable(int tableIndex)
		{
			return true;
		}

		/// <summary>
		/// Indicates if rows from the indicated table need to be tested for each individual tile
		/// they intersect.
		/// </summary>
		/// <param name="tableIndex">Index of the table.</param>
		/// <returns><c>true</c> if rows need to be tested for each tile they intersect. <c>false</c> if testing the
		/// row in the first intersected file is sufficient.</returns>
		public virtual bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			// If the geometry is NOT used, then there's no point in retesting a feature
			// per tile. If the geometry is used, then, by default, retest for each tile

			return IsGeometryUsedTable(tableIndex);
		}

		public virtual bool ValidateParameters(out string error)
		{
			// consider using Notification from Commons.Validation

			error = null;
			return true;
		}

		/// <summary>
		/// Checks if two errors describe the same error
		/// </summary>
		/// <param name="error0"></param>
		/// <param name="error1"></param>
		/// <returns></returns>
		public int Compare([NotNull] QaError error0, [NotNull] QaError error1)
		{
			if (! (error0.Test == this && error1.Test == this))
			{
				throw new InvalidOperationException(
					"Only errors created by this test can be compared");
			}

			return TestUtils.CompareQaErrors(error0, error1,
			                                 compareIndividualInvolvedRows: true);
		}

		internal void BeginTile([NotNull] BeginTileParameters parameters)
		{
			Assert.ArgumentNotNull(parameters, nameof(parameters));

			BeginTileCore(parameters);
		}

		/// <summary>
		/// Informs the test, that all objects lying completely lower and 
		/// more left than the upper right edge of region are processed
		/// </summary>
		internal int CompleteTile([NotNull] TileInfo tileInfo)
		{
			_lastCompleteTileBox = tileInfo.CurrentEnvelope;
			_completeTileInitialized = false;

			int errorCount = CompleteTileCore(tileInfo);
			return errorCount;
		}

		protected virtual void BeginTileCore([NotNull] BeginTileParameters parameters) { }

		protected virtual int CompleteTileCore([NotNull] TileInfo tileInfo)
		{
			return 0;
		}

		/// <summary>
		/// Checks if the specified row fulfils the test constraints
		/// </summary>
		/// <returns></returns>
		internal bool CheckConstraint(IRow row, int tableIndex)
		{
			if (row.Table == null)
			{
				// Mock Row (TerrainRow or RasterRow
				Assert.True(row is IMockRow, "table is null for regular row");

				return true;
			}

			if (row.Table != InvolvedTables[tableIndex])
			{
				throw new InvalidProgramException("row does not correspond to table index");
			}

			QueryFilterHelper filterHelper = _filterHelpers[tableIndex];

			return filterHelper == null || filterHelper.MatchesConstraint(row);
		}

		protected override ISpatialReference GetSpatialReference()
		{
			return TestUtils.GetUniqueSpatialReference(
				this,
				requireEqualVerticalCoordinateSystems: false);
		}

		/// <summary>
		/// Gets the rows in table conforming to filter. 
		/// If the tests belongs to a test container, the data are searched
		/// in the test container cache.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="queryFilter"></param>
		/// <param name="filterHelper">helper corresponding to filter</param>
		/// <param name="cacheGeometry">geometry of feature, from which queryFilter.Geometry was derived</param>
		/// <returns></returns>
		[NotNull]
		protected IEnumerable<IRow> Search([NotNull] ITable table,
		                                   [NotNull] IQueryFilter queryFilter,
		                                   [NotNull] QueryFilterHelper filterHelper,
		                                   [CanBeNull] IGeometry cacheGeometry = null)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));
			Assert.ArgumentNotNull(filterHelper, nameof(filterHelper));

			if (filterHelper.ContainerTest == null)
			{
				filterHelper.ContainerTest = this;
			}

			if (DataContainer != null)
			{
				IEnumerable<IRow> rows = DataContainer.Search(table, queryFilter,
				                                              filterHelper, cacheGeometry);

				if (rows != null)
				{
					return rows;
				}
			}

			// this could be controlled by a flag on the filterHelper or a parameter
			// on the Search() method: AllowRecycling
			const bool recycle = false;
			var cursor = new EnumCursor(table, queryFilter, recycle);

			// TestUtils.AddGarbageCollectionRequest();

			return cursor;
		}

		private void EnsureDefaultFilters()
		{
			if (_defaultFilters == null)
			{
				CopyFilters(out _defaultFilters, out _defaultFilterHelpers);
			}
		}

		/// <summary>
		/// Gets the rows in involvedTable[involvedTableIndex] conforming to geometry, spatialRelation and additional where. 
		/// If the tests belongs to a test container, the data are searched
		/// in the test container cache.
		/// </summary>
		/// <param name="involvedTableIndex">index of the involved table</param>
		/// <param name="geometry"></param>
		/// <param name="spatialRelation"></param>
		/// <param name="where">additional where clause ('involvedTableConstraint' AND 'where')</param>
		/// <returns></returns>
		[NotNull]
		protected IEnumerable<IRow> Search(
			int involvedTableIndex,
			[NotNull] IGeometry geometry,
			esriSpatialRelEnum spatialRelation = esriSpatialRelEnum.esriSpatialRelIntersects,
			[CanBeNull] string where = null)
		{
			EnsureDefaultFilters();

			ITable table = InvolvedTables[involvedTableIndex];
			ISpatialFilter filter = _defaultFilters[involvedTableIndex];
			QueryFilterHelper defaultHelper = _defaultFilterHelpers[involvedTableIndex];

			QueryFilterHelper whereHelper = (string.IsNullOrWhiteSpace(where))
				                                ? new QueryFilterHelper(
					                                table, where, GetSqlCaseSensitivity(table))
				                                : null;

			filter.Geometry = geometry;
			filter.SpatialRel = spatialRelation;
			foreach (var row in Search(table, filter, defaultHelper))
			{
				if (whereHelper == null || whereHelper.MatchesConstraint(row))
				{
					yield return row;
				}
			}
		}

		protected abstract int ExecuteCore([NotNull] IRow row, int tableIndex);

		protected virtual int ExecuteCore([NotNull] ISurfaceRow row, int surfaceIndex)
		{
			return 0;
		}

		internal int Execute([NotNull] IRow row, int tableIndex, Guid? recycledUnique = null)
		{
			if (CancelTestingRow(row, recycledUnique))
			{
				return 0;
			}

			if (! (row is TerrainRow || row.Table == InvolvedTables[tableIndex]))
			{
				throw new InvalidOperationException(
					"row does not correspond to table index");
			}

			return ExecuteCore(row, tableIndex);
		}

		internal int Execute([NotNull] RasterRow rasterRow, int rasterIndex)
		{
			if (! rasterRow.RasterReference.EqualsCore(InvolvedRasters[rasterIndex]))
			{
				throw new InvalidOperationException(
					"row does not correspond to raster index");
			}

			return ExecuteCore(rasterRow, rasterIndex);
		}

		internal int Execute([NotNull] TerrainRow terrainRow, int terrainIndex)
		{
			if (! InvolvedTerrains.Contains(terrainRow.TerrainReference))
			{
				throw new InvalidOperationException(
					"terrain does not correspond to test terrain");
			}

			return ExecuteCore(terrainRow, terrainIndex);
		}

		protected sealed override void AddInvolvedTableCore(ITable table, string constraint,
		                                                    bool sqlCaseSensitivity)
		{
			_filterHelpers.Add(new QueryFilterHelper(table, constraint, sqlCaseSensitivity));
		}

		protected override void SetConstraintCore([NotNull] ITable table, int tableIndex,
		                                          [CanBeNull] string constraint)
		{
			_filterHelpers[tableIndex] = new QueryFilterHelper(table, constraint,
			                                                   GetSqlCaseSensitivity(tableIndex));
		}

		[NotNull]
		public IReadOnlyList<IPreProcessor> GetPreProcessors(int involvedTableIndex)
		{
			if (! PreProssesorDict.TryGetValue(involvedTableIndex,
			                                   out List<IPreProcessor> preProcessors))
			{
				preProcessors = new List<IPreProcessor>();
			}

			return preProcessors;
		}

		private int Execute([NotNull] ITable table, int tableIndex,
		                    [CanBeNull] IQueryFilter queryFilter)
		{
			var cursor = new EnumCursor(table, queryFilter, ! KeepRows);
			var errorCount = 0;
			IReadOnlyList<IPreProcessor> preProcessors = GetPreProcessors(tableIndex);

			foreach (IRow row in cursor)
			{
				if (row is IFeature feature)
				{
					// TODO revise

					// workaround that all spatial searches work properly:
					// explicitly calculate IsSimple --> 
					// state of shape seems to be consistent afterward
					var shapeTopoOp = feature.Shape as ITopologicalOperator2;
					// always, when a shape comes from DB, though it needs not to be true:
					//Debug.Assert(shp.IsKnownSimple && shp.IsSimple);

					if (shapeTopoOp != null)
					{
						shapeTopoOp.IsKnownSimple_2 = false;
						bool simple = shapeTopoOp.IsSimple;
					}
				}

				bool cancel = false;
				foreach (var preprocessor in preProcessors)
				{
					if (! preprocessor.VerifyExecute(row))
					{
						cancel = true;
						break;
					}
				}

				if (! cancel)
				{
					errorCount += ExecuteCore(row, tableIndex);
				}
			}

			return errorCount;
		}

		private class TestProgress : ITestProgress
		{
			public void OnProgressChanged(Step step, int current, int total,
			                              IEnvelope currentEnvelope, IEnvelope allBox) { }

			public void OnProgressChanged(Step step, int current, int total, object tag) { }

			public IDisposable UseProgressWatch(
				Step startStep, Step endStep, int current, int total, object tag)
			{
				return new Disposable();
			}

			private class Disposable : IDisposable
			{
				public void Dispose() { }
			}
		}
	}
}
