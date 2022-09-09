using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.QA.Container.Geometry;
using IPnt = ProSuite.Commons.Geom.IPnt;
using Pnt = ProSuite.Commons.Geom.Pnt;

namespace ProSuite.QA.Container.TestContainer
{
	internal class TileCache
	{
		private readonly IList<IReadOnlyTable> _cachedTables;
		private readonly IDictionary<IReadOnlyTable, RowBoxTree> _rowBoxTrees;
		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();
		private readonly IBox _testRunBox;
		private readonly IDictionary<IReadOnlyTable, double> _xyToleranceByTable;
		private readonly ITestContainer _container;
		private readonly IDictionary<IReadOnlyTable, IList<ContainerTest>> _testsPerTable;
		private readonly IDictionary<IReadOnlyTable, IEnvelope> _loadedExtents;

		private double _maximumSearchTolerance;

		private IDictionary<IReadOnlyTable, IDictionary<IReadOnlyTable, double>>
			_searchToleranceFromTo;

		private IReadOnlyTable _cachedTable;
		private CachedRow _cachedRow;

		private IDictionary<IReadOnlyTable, BoxSelection> _currentRowNeighbors;

		public TileCache([NotNull] IList<IReadOnlyTable> cachedTables, [NotNull] IBox testRunBox,
		                 [NotNull] ITestContainer container,
		                 [NotNull] IDictionary<IReadOnlyTable, IList<ContainerTest>> testsPerTable)
		{
			_cachedTables = cachedTables;
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

		public IDictionary<IReadOnlyTable, IReadOnlyList<IList<BaseRow>>> IgnoredRowsByTableAndTest { get; }

		public TestRow CurrentTestRow { get; set; }
		public Box CurrentTileBox { get; private set; }
		public IEnvelope GetLoadedExtent(IReadOnlyTable table) => _loadedExtents[table];
		public Box SetCurrentTileBox(Box tileBox)
		{
			_currentRowNeighbors = null;
			CurrentTileBox = tileBox;
			return CurrentTileBox;
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

						searchToleranceByTable[involvedTable] =
							Math.Max(searchToleranceByTable[involvedTable],
							         containerTest.SearchDistance);
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
		                          [NotNull] IQueryFilter queryFilter,
		                          [NotNull] QueryFilterHelper filterHelper,
		                          [CanBeNull] IGeometry cacheGeometry)
		{
			var spatialFilter = (ISpatialFilter) queryFilter;
			IGeometry filterGeometry = spatialFilter.Geometry;

			IList<IReadOnlyRow> result = new List<IReadOnlyRow>();

			// TODO explain network queries
			bool repeatCachedRows = filterHelper.RepeatCachedRows ?? filterHelper.ForNetwork;

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

			var cacheGeometryOverlapsLeftTile = false;
			var cacheGeometryOverlapsBottomTile = false;

			if (! repeatCachedRows)
			{
				if (cacheGeometry != null)
				{
					cacheGeometry.QueryEnvelope(_envelopeTemplate);
				}
				else
				{
					filterGeometry.QueryEnvelope(_envelopeTemplate);
				}

				_envelopeTemplate.QueryCoords(out double xmin, out double ymin, out _, out _);

				cacheGeometryOverlapsLeftTile = xmin < CurrentTileBox?.Min.X &&
				                                xmin > _testRunBox.Min.X;

				// https://issuetracker02.eggits.net/browse/COM-85
				// observed (CtLu): 
				// - filter geometry ymin = 220532.967
				// - filter geometry ymax = 220557.78500
				// - tile ymin            = 220557.78534
				// --> filter geometry is completely outside of tile boundaries!!!
				// --> causes incorrect error in QaContainsOther
				cacheGeometryOverlapsBottomTile = ymin < CurrentTileBox?.Min.Y &&
				                                  ymin > _testRunBox.Min.Y;
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

				// This causes problems for QaIsCoveredByOther. However 
				// IsCoveredByOther is not a network test, but still requires cached features
				// to be returned repeatedly
				if (cacheGeometryOverlapsLeftTile && ! cachedRow.IsFirstOccurrenceX)
				{
					// only if *not for network*:
					// the filter geometry overlaps the left border of the tile, but 
					// not the left border of the test run box AND the cached row
					// was already returned previously --> skip it
					continue;
				}

				if (cacheGeometryOverlapsBottomTile && ! cachedRow.IsFirstOccurrenceY)
				{
					// only if *not for network*:
					// the filter geometry overlaps the bottom border of the tile, but 
					// not the bottom border of the test run box AND the cached row
					// was already returned previously --> skip it
					continue;
				}

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

			IBox searchGeometryBox = QaGeometryUtils.CreateBox(searchGeometry,
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

		public IEnumerable<BoxTree<CachedRow>.TileEntry> EnumEntries(IReadOnlyTable cachedTable, IBox box)
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
		/// Gets the number of cached rows for the current tile
		/// </summary>
		/// <returns></returns>
		public int GetTablesRowCount()
		{
			var result = 0;

			foreach (RowBoxTree rowBoxTree in _rowBoxTrees.Values)
			{
				result += rowBoxTree.Count;
			}

			return result;
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
				return QaGeometryUtils.CreateBox(CurrentTestRow.DataReference.Extent);
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
			return Math.Max(GetXYTolerance(table),
			                _searchToleranceFromTo[fromTable][table]);
		}

		public void CreateBoxTree(IReadOnlyTable table, [NotNull] IEnumerable<CachedRow> cachedRows,
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
				rowBoxTree.InitSize(new IGmtry[] {allBox});
			}

			foreach (CachedRow cachedRow in cachedRows)
			{
				rowBoxTree.Add(cachedRow);
			}

			return rowBoxTree;
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
