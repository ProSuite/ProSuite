using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.Commons.Logging;
using IPnt = ProSuite.Commons.Geom.IPnt;
using Pnt = ProSuite.Commons.Geom.Pnt;

namespace ProSuite.QA.Container.TestContainer
{
	/// <summary>
	/// The tile data for all tables in a single tile extent.
	/// </summary>
	internal class TileCache
	{
		private readonly IList<IReadOnlyTable> _cachedTables;

		private readonly IBox _testRunBox;
		private readonly IDictionary<IReadOnlyTable, double> _xyToleranceByTable;
		private readonly ITestContainer _container;
		private readonly IDictionary<IReadOnlyTable, IList<ContainerTest>> _testsPerTable;
		private readonly IDictionary<IReadOnlyTable, CachedTableProps> _cachedTableProps;

		private IDictionary<IReadOnlyTable, RowBoxTree> _rowBoxTrees;
		private IDictionary<IReadOnlyTable, IEnvelope> _loadedExtents;

		private double _maximumSearchTolerance;

		private IDictionary<IReadOnlyTable, IDictionary<IReadOnlyTable, double>>
			_searchToleranceFromTo;

		private IReadOnlyTable _cachedTable;
		private CachedRow _cachedRow;
		private IDictionary<IReadOnlyTable, BoxSelection> _currentRowNeighbors;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public TileCache([NotNull] IDictionary<IReadOnlyTable, CachedTableProps> cachedTableProps,
		                 [NotNull] IBox testRunBox,
		                 [NotNull] ITestContainer container,
		                 [NotNull] IDictionary<IReadOnlyTable, IList<ContainerTest>> testsPerTable)
		{
			_cachedTables = new List<IReadOnlyTable>(cachedTableProps.Keys);
			_cachedTableProps = cachedTableProps;
			_testRunBox = testRunBox;
			_container = container;
			_testsPerTable = testsPerTable;

			_rowBoxTrees = new ConcurrentDictionary<IReadOnlyTable, RowBoxTree>();
			_xyToleranceByTable = GetXYTolerancePerTable(_cachedTables);
			_loadedExtents = new ConcurrentDictionary<IReadOnlyTable, IEnvelope>();
			IgnoredRowsByTableAndTest =
				new ConcurrentDictionary<IReadOnlyTable, IReadOnlyList<IList<BaseRow>>>();

			CollectSearchTolerances();
		}

		public TileCache Clone()
		{
			var clone = (TileCache) MemberwiseClone();
			clone._rowBoxTrees = new ConcurrentDictionary<IReadOnlyTable, RowBoxTree>();
			clone._loadedExtents = new ConcurrentDictionary<IReadOnlyTable, IEnvelope>();
			clone.LoadingTileBox = null;
			return clone;
		}

		public IDictionary<IReadOnlyTable, IReadOnlyList<IList<BaseRow>>> IgnoredRowsByTableAndTest
		{
			get;
		}

		public TestRow CurrentTestRow { get; set; }
		public Box CurrentTileBox { get; private set; }
		internal Box LoadingTileBox { get; set; }

		public IEnvelope GetLoadedExtent(IReadOnlyTable table)
		{
			_loadedExtents.TryGetValue(table, out IEnvelope loaded);
			return loaded;
		}

		public Box SetCurrentTileBox(Box tileBox)
		{
			_currentRowNeighbors = null;
			CurrentTileBox = tileBox;
			return CurrentTileBox;
		}

		public bool IsLoaded(IReadOnlyTable table, Tile tile)
		{
			if (! _rowBoxTrees.ContainsKey(table))
			{
				return false;
			}

			IEnvelope tileEnv = tile.FilterEnvelope;
			CachedTableProps props = _cachedTableProps[table];
			if (props.HasGeotransformation != null)
			{
				tileEnv = props.HasGeotransformation.ProjectEx(tileEnv);
			}

			if (! ((IRelationalOperator) tileEnv).Within(_loadedExtents[table]))
			{
				return false;
			}

			return true;
		}

		[NotNull]
		private static IDictionary<IReadOnlyTable, double> GetXYTolerancePerTable(
			[NotNull] ICollection<IReadOnlyTable> tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));

			var result = new ConcurrentDictionary<IReadOnlyTable, double>();

			const double defaultTolerance = 0;

			foreach (IReadOnlyTable table in tables)
			{
				var geoDataset = table as IReadOnlyGeoDataset;
				result[table] = geoDataset == null
					                ? defaultTolerance
					                : GeometryUtils.GetXyTolerance(geoDataset.SpatialReference,
						                defaultTolerance);
			}

			return result;
		}

		private void CollectSearchTolerances()
		{
			_maximumSearchTolerance = 0;
			_searchToleranceFromTo =
				new ConcurrentDictionary<IReadOnlyTable, IDictionary<IReadOnlyTable, double>>();

			foreach (IReadOnlyTable table in _cachedTables)
			{
				var searchToleranceByTable = new ConcurrentDictionary<IReadOnlyTable, double>();
				_searchToleranceFromTo[table] = searchToleranceByTable;

				if (! _testsPerTable.TryGetValue(table, out IList<ContainerTest> tableTests))
				{
					continue;
				}

				foreach (ContainerTest containerTest in tableTests)
				{
					if (Math.Abs(containerTest.SearchDistance) < double.Epsilon)
					{
						continue;
					}

					_maximumSearchTolerance = Math.Max(_maximumSearchTolerance,
					                                   containerTest.SearchDistance);

					foreach (IReadOnlyTable involvedTable in containerTest.InvolvedTables)
					{
						int involvedTableIndex = _cachedTables.IndexOf(involvedTable);

						if (involvedTableIndex < 0)
						{
							continue;
						}

						if (! searchToleranceByTable.TryGetValue(
							    involvedTable, out double currentTolerance))
						{
							currentTolerance = 0;
						}

						searchToleranceByTable[involvedTable] =
							Math.Max(currentTolerance, containerTest.SearchDistance);
					}
				}
			}
		}

		private double GetXYTolerance(IReadOnlyTable table)
		{
			return _xyToleranceByTable[table];
		}

		[CanBeNull]
		public IList<IReadOnlyRow> Search([NotNull] IReadOnlyTable table,
		                                  [NotNull] ITableFilter filter,
		                                  [NotNull] QueryFilterHelper filterHelper)
		{
			var spatialFilter = (IFeatureClassFilter) filter;
			IGeometry filterGeometry = spatialFilter.FilterGeometry;

			IList<IReadOnlyRow> result = new List<IReadOnlyRow>();

			// TODO explain network queries

			// filterHelper.PointSearchOnlyWithinTile
			if (filterHelper.ForNetwork)
			{
				if (filterGeometry is IPoint filterPoint)
				{
					// search only if the point is within the tile box
					// (or left/below of test run box)

					filterPoint.QueryCoords(out double x, out double y);

					Pnt tileMin = CurrentTileBox.Min;
					Pnt tileMax = CurrentTileBox.Max;
					IPnt testRunMin = _testRunBox.Min;

					if (x <= tileMin.X && x > testRunMin.X || x > tileMax.X ||
					    y <= tileMin.Y && y > testRunMin.Y || y > tileMax.Y)
					{
						// outside of tile box, return empty list
						return result;
					}
				}
			}

			List<BoxTree<CachedRow>.TileEntry> searchList =
				SearchList(filterGeometry, table);
			if (searchList == null || searchList.Count == 0)
			{
				return result;
			}

			IGeometryEngine engine = _container.GeometryEngine;

			engine.SetSourceGeometry(filterGeometry);

			IList<BaseRow> ignoredRows = null;
			if (_testsPerTable.TryGetValue(table, out IList<ContainerTest> tests))
			{
				if (filterHelper.ContainerTest != null)
				{
					int indexTest = tests.IndexOf(filterHelper.ContainerTest);
					ignoredRows = IgnoredRowsByTableAndTest[table][indexTest];
				}
			}

			foreach (BoxTree<CachedRow>.TileEntry entry in searchList)
			{
				CachedRow cachedRow = Assert.NotNull(entry.Value, "cachedRow");

				if (ignoredRows != null && ignoredRows.Contains(entry.Value))
				{
					continue;
				}

				IReadOnlyFeature targetFeature = cachedRow.Feature;

				if (targetFeature.OID < filterHelper.MinimumOID)
				{
					continue;
				}

				engine.SetTargetGeometry(cachedRow.Geometry);

				// Remark: if most of the rows fullfill helper.Check, 
				// it is better to check the geometric relation first
				var matchesConstraint = false;
				if (filterHelper.AttributeFirst)
				{
					if (! filterHelper.MatchesConstraint(targetFeature))
					{
						continue;
					}

					matchesConstraint = true;
				}

				if (engine.EvaluateRelation(spatialFilter))
				{
					if (matchesConstraint || filterHelper.MatchesConstraint(targetFeature))
					{
						result.Add(targetFeature);
					}
				}
			}

			return result;
		}

		[CanBeNull]
		private List<BoxTree<CachedRow>.TileEntry> SearchList(
			[NotNull] IGeometry searchGeometry, IReadOnlyTable table)
		{
			Assert.ArgumentNotNull(searchGeometry, nameof(searchGeometry));

			IBox searchGeometryBox = ProxyUtils.CreateBox(searchGeometry,
			                                              GetXYTolerance(table));

			BoxTree<CachedRow> boxTree = _rowBoxTrees[table];

			_currentRowNeighbors = _currentRowNeighbors ??
			                       new ConcurrentDictionary<IReadOnlyTable, BoxSelection>();

			if (! _currentRowNeighbors.TryGetValue(table, out BoxSelection currentRowBoxSelection))
			{
				currentRowBoxSelection = CreateCurrentRowToleranceSelection(table);
				_currentRowNeighbors[table] = currentRowBoxSelection;
			}

			IBox searchBox = null;
			var isWithin = false;
			if (currentRowBoxSelection != null)
			{
				isWithin = currentRowBoxSelection.Box.Contains(searchGeometryBox);
			}

			if (! isWithin)
			{
				searchBox = searchGeometryBox;
			}
			else if (currentRowBoxSelection.Selection == null)
			{
				searchBox = currentRowBoxSelection.Box;
			}

			List<BoxTree<CachedRow>.TileEntry> tileEntries;
			if (searchBox != null)
			{
				tileEntries = new List<BoxTree<CachedRow>.TileEntry>();

				foreach (
					BoxTree<CachedRow>.TileEntry tileEntry in boxTree.Search(searchBox))
				{
					tileEntries.Add(tileEntry);
				}

				if (isWithin)
				{
					currentRowBoxSelection.Selection = tileEntries;
				}
			}
			else
			{
				tileEntries = currentRowBoxSelection.Selection;
			}

			if (! isWithin || searchGeometryBox.Contains(currentRowBoxSelection.Box))
			{
				return tileEntries;
			}

			// drop non intersection lines
			Assert.NotNull(tileEntries, "tileEntries");

			var reducedList
				= new List<BoxTree<CachedRow>.TileEntry>(tileEntries.Count);

			foreach (BoxTree<CachedRow>.TileEntry tileEntry in tileEntries)
			{
				if (tileEntry.Box.Intersects(searchGeometryBox))
				{
					reducedList.Add(tileEntry);
				}
			}

			return reducedList;
		}

		public IEnumerable<BoxTree<CachedRow>.TileEntry> EnumEntries(
			IReadOnlyTable cachedTable, IBox box)
		{
			_cachedTable = cachedTable;
			RowBoxTree rowBoxTree = _rowBoxTrees[cachedTable];
			foreach (BoxTree<CachedRow>.TileEntry entry in rowBoxTree.Search(box))
			{
				_cachedRow = entry.Value;
				yield return entry;
			}

			_cachedRow = null;
			_cachedTable = null;
		}

		/// <summary>
		/// Gets the total number of cached rows for all tables of the current tile.
		/// </summary>
		/// <returns></returns>
		public int GetCachedRowCount()
		{
			var result = 0;

			foreach (RowBoxTree rowBoxTree in _rowBoxTrees.Values)
			{
				result += rowBoxTree.Count;
			}

			return result;
		}

		/// <summary>
		/// Gets the number of cached rows for the specified table in the current tile.
		/// </summary>
		/// <returns></returns>
		public int GetCachedRowCount(IReadOnlyTable forTable)
		{
			return ! _rowBoxTrees.TryGetValue(forTable, out RowBoxTree rowBoxTree)
				       ? 0
				       : rowBoxTree.Count;
		}

		[CanBeNull]
		private BoxSelection CreateCurrentRowToleranceSelection(IReadOnlyTable table)
		{
			IBox toleranceBox = GetCurrentRowToleranceBox(table);
			if (toleranceBox == null)
			{
				return null;
			}

			var boxSelection = new BoxSelection();
			boxSelection.Box = toleranceBox;

			return boxSelection;
		}

		[CanBeNull]
		private IBox GetCurrentRowToleranceBox(IReadOnlyTable table)
		{
			if (CurrentTestRow == null)
			{
				return null;
			}

			if (_cachedRow == null)
			{
				return ProxyUtils.CreateBox(CurrentTestRow.DataReference.Extent);
			}

			IBox currentBox = _cachedRow.Extent;

			double searchTolerance = GetSearchTolerance(_cachedTable, table);

			IBox toleranceBox = currentBox.Clone();

			toleranceBox.Min.X -= searchTolerance;
			toleranceBox.Min.Y -= searchTolerance;
			toleranceBox.Max.X += searchTolerance;
			toleranceBox.Max.Y += searchTolerance;

			return toleranceBox;
		}

		private double GetSearchTolerance(IReadOnlyTable fromTable, IReadOnlyTable table)
		{
			if (! _searchToleranceFromTo[fromTable].TryGetValue(table, out double tolerance))
			{
				tolerance = 0;
			}

			return Math.Max(GetXYTolerance(table), tolerance);
		}

		private void CreateBoxTree(IReadOnlyTable table,
		                           [NotNull] IEnumerable<CachedRow> cachedRows,
		                           [CanBeNull] IBox allBox, IEnvelope loadedEnvelope)
		{
			_rowBoxTrees[table] = CreateBoxTree(cachedRows, allBox);
			_loadedExtents[table] = loadedEnvelope;
		}

		[NotNull]
		private static RowBoxTree CreateBoxTree(
			[NotNull] IEnumerable<CachedRow> cachedRows,
			[CanBeNull] IBox allBox)
		{
			var rowBoxTree = new RowBoxTree();

			if (allBox != null)
			{
				rowBoxTree.InitSize(new IGmtry[] { allBox });
			}

			foreach (CachedRow cachedRow in cachedRows)
			{
				rowBoxTree.Add(cachedRow);
			}

			return rowBoxTree;
		}

		internal IList<CachedRow> TransferCachedRows(
			TileCache target, IReadOnlyTable table)
		{
			if (! _rowBoxTrees.TryGetValue(table, out var sourceTree))
			{
				return null;
			}

			if (! target._rowBoxTrees.ContainsKey(table))
			{
				return null;
			}

			target._rowBoxTrees[table] = sourceTree;
			target._loadedExtents[table] = _loadedExtents[table];

			IList<CachedRow> result = new List<CachedRow>();

			foreach (BoxTree<CachedRow>.TileEntry entry in target._rowBoxTrees[table].Search(null))
			{
				if (! entry.Value.HasFeatureCached())
				{
					// TODO: why can this be
					return null;
				}

				result.Add(entry.Value);
			}

			return result;
		}

		public void LoadCachedTableRows(
			[NotNull] IDictionary<BaseRow, CachedRow> cachedRows,
			[NotNull] CachedTableProps tableProps,
			[NotNull] Tile tile,
			[NotNull] ITileEnumContext context)
		{
			IBox allBox = null;

			IReadOnlyTable table = tableProps.Table;
			// avoid rereading overlapping large features (with a max extent > tile size)
			// TODO: use more detailed info ignore / improve notInExpression
			bool isQueryTable = table.FullName is IQueryName2;
			string notInExpression =
				isQueryTable
					? string.Empty
					: GetFilterOldLargeRows(cachedRows.Values, context.TileSize, ref allBox);

			IFeatureClassFilter loadSpatialFilter =
				GetLoadSpatialFilter(tableProps, tile.SpatialFilter, context, notInExpression);

			IEnvelope loadExtent =
				GeometryFactory.Clone((IEnvelope) loadSpatialFilter.FilterGeometry);

			AddRowsToCache(cachedRows, tableProps, loadSpatialFilter, context, ref allBox);

			CreateBoxTree(table, cachedRows.Values, allBox, loadExtent);
		}

		[NotNull]
		private IFeatureClassFilter GetLoadSpatialFilter(
			[NotNull] CachedTableProps tableProps,
			[NotNull] IFeatureClassFilter tileSpatialFilter,
			[NotNull] ITileEnumContext context,
			[CanBeNull] string notInExpression)
		{
			Assert.ArgumentNotNull(tableProps, nameof(tableProps));
			Assert.ArgumentNotNull(tileSpatialFilter, nameof(tileSpatialFilter));

			IReadOnlyTable table = tableProps.Table;

			string whereClause = _container.FilterExpressionsUseDbSyntax
				                     ? context.GetCommonFilterExpression(table)
				                     : string.Empty;

			if (! string.IsNullOrWhiteSpace(notInExpression))
			{
				whereClause = string.IsNullOrEmpty(whereClause)
					              ? notInExpression
					              : whereClause + " AND " + notInExpression;
			}

			IEnvelope filterGeometry =
				(IEnvelope) ((IClone) tileSpatialFilter.FilterGeometry).Clone();
			esriSpatialRelEnum spatialRelationship = tileSpatialFilter.SpatialRelationship;

			IWorkspace workspace = table.Workspace;

			// Workaround : Shapefile-Tables may return wrong feature when querying with esriSpatialRelEnvelopeIntersects
			if (spatialRelationship == esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects &&
			    workspace.Type == esriWorkspaceType.esriFileSystemWorkspace)
			{
				spatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;
			}

			if (tableProps.HasGeotransformation != null)
			{
				filterGeometry =
					tableProps.HasGeotransformation.ProjectEx(filterGeometry);
			}

			double searchTolerance = context.OverlappingFeatures.GetSearchTolerance(table);
			if (searchTolerance > 0)
			{
				filterGeometry.Expand(searchTolerance, searchTolerance, false);
			}

			var result = new AoFeatureClassFilter(filterGeometry, spatialRelationship)
			             {
				             SubFields = tileSpatialFilter.SubFields,
				             WhereClause = whereClause,
				             TileExtent = tileSpatialFilter.FilterGeometry.Envelope
			             };

			// Exclude blob fields for better performance and memory behaviour:
			if (tableProps.AdaptSubFields(result.SubFields, out string newSubFields))
			{
				result.SubFields = newSubFields;
			}

			return result;
		}

		private void AddRowsToCache([NotNull] IDictionary<BaseRow, CachedRow> cachedRows,
		                            [NotNull] CachedTableProps tableProps,
		                            [NotNull] ITableFilter filter,
		                            [NotNull] ITileEnumContext context,
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

			IReadOnlyTable table = tableProps.Table;
			IUniqueIdProvider uniqueIdProvider = context.GetUniqueIdProvider(table);
			// get data from database
			try
			{
				IFeatureClassFilter spatialFilter = (IFeatureClassFilter) filter;
				ISpatialReference filterSr = spatialFilter.FilterGeometry.SpatialReference;
				(table as ITransformedTable)?.SetKnownTransformedRows(
					cachedRows.Values.Select(x => x.Feature as IReadOnlyRow));
				foreach (IReadOnlyRow row in table.EnumRows(filter, recycle: false))
				{
					var feature = (IReadOnlyFeature) row;
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

					// Do not optimize for execute area when differing spatial references
					if (tableProps.HasGeotransformation == null)
					{
						bool disjoint = context.IsDisjointFromExecuteArea(feature.Shape);

						cachedRow.DisjointFromExecuteArea = disjoint;
					}

					if (allBox == null)
					{
						allBox = cachedRowExtent.Clone();
					}
					else
					{
						allBox.Include(cachedRowExtent);
					}
				}
			}
			finally
			{
				(table as ITransformedTable)?.SetKnownTransformedRows(null);
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
					IReadOnlyFeature feature = cachedRow.Feature;

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

		#region Nested type: RowBoxTree

		private class RowBoxTree : BoxTree<CachedRow>
		{
			private const int _dimension = 2;
			private const int _maximumElementCountPerTile = 64;
			private const bool _dynamic = true;

			public RowBoxTree() : base(_dimension, _maximumElementCountPerTile, _dynamic) { }

			[NotNull]
			public CachedRow GetCachedRow(int index)
			{
				CachedRow cachedRow = base[index].Value;

				Assert.NotNull(cachedRow, "TileEntry value is null");

				return cachedRow;
			}

			public void Add([NotNull] CachedRow row)
			{
				Add(row.Extent, row);
			}
		}

		#endregion

		#region Nested type: BoxSelection

		private class BoxSelection
		{
			public IBox Box;
			public List<BoxTree<CachedRow>.TileEntry> Selection;
		}

		#endregion
	}
}
