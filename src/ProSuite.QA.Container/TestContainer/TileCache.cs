using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Geometry.SpatialIndex;
using ProSuite.QA.Container.Geometry;
using IPnt = ProSuite.Commons.Geometry.IPnt;
using Pnt = ProSuite.Commons.Geometry.Pnt;

namespace ProSuite.QA.Container.TestContainer
{
	internal class TileCache
	{
		private readonly IList<ITable> _cachedTables;
		private readonly RowBoxTree[] _rowBoxTrees;
		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();
		private readonly IBox _testRunBox;
		private readonly double[] _xyToleranceByTableIndex;
		private readonly ITestContainer _container;
		private readonly int _cachedTableCount;
		private readonly IDictionary<ITable, IList<ContainerTest>> _testsPerTable;

		private double _maximumSearchTolerance;
		private double[][] _searchToleranceFromTo;

		private int _cachedTableIndex;
		private CachedRow _cachedRow;

		private BoxSelection[] _currentRowNeighbors;

		public TileCache([NotNull] IList<ITable> cachedTables, [NotNull] IBox testRunBox,
		                 [NotNull] ITestContainer container,
		                 [NotNull] IDictionary<ITable, IList<ContainerTest>> testsPerTable)
		{
			_cachedTables = cachedTables;
			_testRunBox = testRunBox;
			_container = container;
			_testsPerTable = testsPerTable;

			_cachedTableCount = cachedTables.Count;
			_rowBoxTrees = new RowBoxTree[_cachedTables.Count];
			_xyToleranceByTableIndex = GetXYTolerancePerTable(_testsPerTable.Keys);
			IgnoredRowsByTableAndTest = new List<IList<BaseRow>>[_cachedTableCount];
			OverlappingFeatures = new OverlappingFeatures(_container.MaxCachedPointCount);

			CollectSearchTolerances();
		}

		public OverlappingFeatures OverlappingFeatures { get; }

		public List<IList<BaseRow>>[] IgnoredRowsByTableAndTest { get; }

		public TestRow CurrentTestRow { get; set; }
		public Box CurrentTileBox { get; set; }

		[NotNull]
		private static double[] GetXYTolerancePerTable(
			[NotNull] ICollection<ITable> tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));

			var result = new double[tables.Count];

			const double defaultTolerance = 0;

			var tableIndex = 0;
			foreach (ITable table in tables)
			{
				var geoDataset = table as IGeoDataset;
				result[tableIndex] = geoDataset == null
					                     ? defaultTolerance
					                     : GeometryUtils.GetXyTolerance(geoDataset,
					                                                    defaultTolerance);

				tableIndex++;
			}

			return result;
		}

		private void CollectSearchTolerances()
		{
			_maximumSearchTolerance = 0;
			_searchToleranceFromTo = new double[_cachedTableCount][];

			for (var queriedTableIndex = 0;
			     queriedTableIndex < _cachedTables.Count;
			     queriedTableIndex++)
			{
				var searchToleranceByTable = new double[_cachedTableCount];

				_searchToleranceFromTo[queriedTableIndex] = searchToleranceByTable;

				ITable table = GetCachedTable(queriedTableIndex);
				IList<ContainerTest> pTestList = _testsPerTable[table];

				foreach (ContainerTest containerTest in pTestList)
				{
					if (Math.Abs(containerTest.SearchDistance) < double.Epsilon)
					{
						continue;
					}

					_maximumSearchTolerance = Math.Max(_maximumSearchTolerance,
					                                   containerTest.SearchDistance);

					foreach (ITable involvedTable in containerTest.InvolvedTables)
					{
						int involvedTableIndex = _cachedTables.IndexOf(involvedTable);

						if (involvedTableIndex < 0)
						{
							continue;
						}

						searchToleranceByTable[involvedTableIndex] =
							Math.Max(searchToleranceByTable[involvedTableIndex],
							         containerTest.SearchDistance);

						OverlappingFeatures.AdaptSearchTolerance(
							involvedTable, containerTest.SearchDistance);
					}
				}
			}
		}

		[NotNull]
		private ITable GetCachedTable(int tableIndex)
		{
			return _cachedTables[tableIndex];
		}

		private double GetXYTolerance(int tableIndex)
		{
			return _xyToleranceByTableIndex[tableIndex];
		}

		[CanBeNull]
		public IList<IRow> Search([NotNull] ITable table,
		                          int tableIndex,
		                          [NotNull] IQueryFilter queryFilter,
		                          [NotNull] QueryFilterHelper filterHelper,
		                          [CanBeNull] IGeometry cacheGeometry)
		{
			var spatialFilter = (ISpatialFilter) queryFilter;
			IGeometry filterGeometry = spatialFilter.Geometry;

			IList<IRow> result = new List<IRow>();

			// TODO explain network queries
			bool repeatCachedRows = filterHelper.ForNetwork;

			// filterHelper.PointSearchOnlyWithinTile
			if (filterHelper.ForNetwork)
			{
				var filterPoint = filterGeometry as IPoint;
				if (filterPoint != null)
				{
					// search only if the point is within the tile box
					// (or left/below of test run box)

					double x;
					double y;
					filterPoint.QueryCoords(out x, out y);

					Pnt tileMin = CurrentTileBox.Min;
					Pnt tileMax = CurrentTileBox.Max;
					IPnt testRunMin = _testRunBox.Min;

					if ((x <= tileMin.X && x > testRunMin.X) || x > tileMax.X ||
					    (y <= tileMin.Y && y > testRunMin.Y) || y > tileMax.Y)
					{
						// outside of tile box, return empty list
						return result;
					}
				}
			}

			List<BoxTree<CachedRow>.TileEntry> searchList = SearchList(filterGeometry,
			                                                           tableIndex);
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

				double xmin;
				double ymin;
				double xmax;
				double ymax;
				_envelopeTemplate.QueryCoords(out xmin, out ymin, out xmax, out ymax);

				cacheGeometryOverlapsLeftTile = xmin < CurrentTileBox.Min.X &&
				                                xmin > _testRunBox.Min.X;

				// https://issuetracker02.eggits.net/browse/COM-85
				// observed (CtLu): 
				// - filter geometry ymin = 220532.967
				// - filter geometry ymax = 220557.78500
				// - tile ymin            = 220557.78534
				// --> filter geometry is completely outside of tile boundaries!!!
				// --> causes incorrect error in QaContainsOther
				cacheGeometryOverlapsBottomTile = ymin < CurrentTileBox.Min.Y &&
				                                  ymin > _testRunBox.Min.Y;
			}

			IGeometryEngine engine = _container.GeometryEngine;

			engine.SetSourceGeometry(filterGeometry);

			IList<ContainerTest> tests = _testsPerTable[table];
			int indexTest = tests.IndexOf(filterHelper.ContainerTest);

			IList<BaseRow> ignoredRows = IgnoredRowsByTableAndTest[tableIndex][indexTest];

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

				IFeature targetFeature = cachedRow.Feature;

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
			[NotNull] IGeometry searchGeometry, int tableIndex)
		{
			Assert.ArgumentNotNull(searchGeometry, nameof(searchGeometry));

			IBox searchGeometryBox = QaGeometryUtils.CreateBox(searchGeometry,
			                                                   GetXYTolerance(tableIndex));

			BoxTree<CachedRow> boxTree = _rowBoxTrees[tableIndex];

			if (_currentRowNeighbors == null)
			{
				_currentRowNeighbors = new BoxSelection[_cachedTableCount];
			}

			BoxSelection currentRowBoxSelection = _currentRowNeighbors[tableIndex];
			if (currentRowBoxSelection == null)
			{
				currentRowBoxSelection = CreateCurrentRowToleranceSelection(tableIndex);

				_currentRowNeighbors[tableIndex] = currentRowBoxSelection;
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

		public IEnumerable<BoxTree<CachedRow>.TileEntry> EnumEntries(int cachedTableIndex, IBox box)
		{
			_cachedTableIndex = cachedTableIndex;
			RowBoxTree rowBoxTree = _rowBoxTrees[cachedTableIndex];
			foreach (BoxTree<CachedRow>.TileEntry entry in rowBoxTree.Search(box))
			{
				_cachedRow = entry.Value;
				yield return entry;
			}

			_cachedRow = null;
			_cachedTableIndex = 0;
		}

		/// <summary>
		/// Gets the number of cached rows for the current tile
		/// </summary>
		/// <returns></returns>
		public int GetTablesRowCount()
		{
			var result = 0;

			foreach (RowBoxTree rowBoxTree in _rowBoxTrees)
			{
				result += rowBoxTree.Count;
			}

			return result;
		}

		[CanBeNull]
		private BoxSelection CreateCurrentRowToleranceSelection(int tableIndex)
		{
			IBox toleranceBox = GetCurrentRowToleranceBox(tableIndex);
			if (toleranceBox == null)
			{
				return null;
			}

			var boxSelection = new BoxSelection();
			boxSelection.Box = toleranceBox;

			return boxSelection;
		}

		[CanBeNull]
		private IBox GetCurrentRowToleranceBox(int tableIndex)
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

			double searchTolerance = GetSearchTolerance(_cachedTableIndex, tableIndex);

			IBox toleranceBox = currentBox.Clone();

			toleranceBox.Min.X -= searchTolerance;
			toleranceBox.Min.Y -= searchTolerance;
			toleranceBox.Max.X += searchTolerance;
			toleranceBox.Max.Y += searchTolerance;

			return toleranceBox;
		}

		private double GetSearchTolerance(int fromTableIndex, int tableIndex)
		{
			return Math.Max(GetXYTolerance(tableIndex),
			                _searchToleranceFromTo[fromTableIndex][tableIndex]);
		}

		public void CreateBoxTree(int tableIndex, [NotNull] IEnumerable<CachedRow> cachedRows,
		                          [CanBeNull] IBox allBox)
		{
			_rowBoxTrees[tableIndex] = CreateBoxTree(cachedRows, allBox);
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
