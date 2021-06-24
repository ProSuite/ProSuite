using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.TestContainer
{
	public class OverlappingFeatures
	{
		[NotNull] private static readonly BaseRowComparer _comparer =
			new BaseRowComparer();

		[NotNull] private readonly IDictionary<ITable, IDictionary<BaseRow, TestedRow>>
			_overlappingRows =
				new Dictionary<ITable, IDictionary<BaseRow, TestedRow>>();

		[NotNull] private readonly Dictionary<ITable, double> _searchTolerance =
			new Dictionary<ITable, double>();

		[CanBeNull] private IBox _currentTileBox;

		[NotNull] private readonly List<CachedRow> _currentUncachableRows =
			new List<CachedRow>();

		private int _currentCachedPointCount;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="OverlappingFeatures"/> class.
		/// </summary>
		public OverlappingFeatures() : this(-1) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="OverlappingFeatures"/> class.
		/// </summary>
		/// <param name="maxCachedPointCount">The maximum number of cached vertices.</param>
		public OverlappingFeatures(int maxCachedPointCount)
		{
			MaxCachedPointCount = maxCachedPointCount;
		}

		#endregion

		public void RegisterTestedFeature([NotNull] BaseRow row,
		                                  [CanBeNull] IList<ContainerTest> reducedTests)
		{
			double searchTolerance = GetSearchTolerance(row.Table);

			RegisterTestedFeature(row, searchTolerance, reducedTests);
		}

		public int MaxCachedPointCount { get; }

		public int CurrentCachedPointCount => MaxCachedPointCount < 0
			                                      ? -1
			                                      : _currentCachedPointCount;

		public bool WasAlreadyTested([NotNull] IFeature feature,
		                             [NotNull] ContainerTest containerTest)
		{
			TestedRow testedRow = GetTestedRow(feature);

			return testedRow != null && testedRow.WasTestedFor(containerTest);
		}

		public void AdaptSearchTolerance([NotNull] ITable table, double searchDistance)
		{
			double existingDistance;
			if (_searchTolerance.TryGetValue(table, out existingDistance))
			{
				_searchTolerance[table] = Math.Max(existingDistance, searchDistance);
			}
			else
			{
				_searchTolerance.Add(table, searchDistance);
			}
		}

		public double GetSearchTolerance([CanBeNull] ITable table)
		{
			const double defaultTolerance = 0;

			if (table == null)
			{
				return defaultTolerance;
			}

			double tolerance;
			return ! _searchTolerance.TryGetValue(table, out tolerance)
				       ? defaultTolerance
				       : tolerance;
		}

		/// <summary>
		/// removes all TestedRows that won't be encountered again 
		/// and find all rows that are not needed in  currentTileBox
		/// </summary>
		/// <param name="currentTileBox"></param>
		public void SetCurrentTile([NotNull] IBox currentTileBox)
		{
			_currentUncachableRows.Clear();
			_currentCachedPointCount = 0;

			foreach (
				KeyValuePair<ITable, IDictionary<BaseRow, TestedRow>> pairRows in
				_overlappingRows)
			{
				ITable table = pairRows.Key;
				IDictionary<BaseRow, TestedRow> testedRows = pairRows.Value;

				double searchTolerance = GetSearchTolerance(table);
				IBox searchBox = GetSearchBox(currentTileBox, searchTolerance);
				searchBox.Max.Y -= searchTolerance;

				RemoveObsoleteRows(testedRows, searchBox);
			}

			_currentTileBox = currentTileBox;
		}

		#region Non-public

		private void RemoveObsoleteRows(IDictionary<BaseRow, TestedRow> testedRows,
		                                [NotNull] IBox searchBox)
		{
			List<BaseRow> toBeRemoved = null;
			foreach (KeyValuePair<BaseRow, TestedRow> pair in testedRows)
			{
				BaseRow baseRow = pair.Key;
				TestedRow testedRow = pair.Value;

				if (! testedRow.HasNoRemainingOccurrence(searchBox))
				{
					ValidateCache(baseRow, searchBox);

					continue;
				}

				if (toBeRemoved == null)
				{
					toBeRemoved = new List<BaseRow>();
				}

				toBeRemoved.Add(baseRow);
			}

			if (toBeRemoved != null)
			{
				foreach (BaseRow oidToRemove in toBeRemoved)
				{
					testedRows.Remove(oidToRemove);

					oidToRemove.UniqueId?.Drop();
				}
			}
		}

		internal void RegisterTestedFeature([NotNull] BaseRow row,
		                                    double searchTolerance,
		                                    [CanBeNull] IList<ContainerTest> reducedTests)
		{
			ITable table = row.Table;
			if (table == null)
			{
				return;
			}

			if (_currentTileBox != null)
			{
				if (row.Extent.Max.Y + searchTolerance <= _currentTileBox.Max.Y &&
				    row.Extent.Max.X + searchTolerance <= _currentTileBox.Max.X)
				{
					// the feature is not needed in the untested tiles
					return;
				}
			}

			Register(table, _overlappingRows, row, reducedTests);
		}

		[NotNull]
		internal IDictionary<BaseRow, CachedRow> GetOverlappingCachedRows(
			[NotNull] ITable table,
			[NotNull] IBox currentTileBox)
		{
			return GetOverlappingCachedRows(table, currentTileBox, GetSearchTolerance(table));
		}

		[NotNull]
		private IDictionary<BaseRow, CachedRow> GetOverlappingCachedRows(
			[NotNull] ITable table,
			[NotNull] IBox currentTileBox,
			double searchTolerance)
		{
			IDictionary<BaseRow, CachedRow> result =
				LargeDictionaryFactory.CreateDictionary<BaseRow, CachedRow>(
					equalityComparer: _comparer);

			IDictionary<BaseRow, TestedRow> allRows;
			if (! _overlappingRows.TryGetValue(table, out allRows))
			{
				return result;
			}

			IBox searchBox = GetSearchBox(currentTileBox, searchTolerance);

			foreach (KeyValuePair<BaseRow, TestedRow> pair in allRows)
			{
				var cachedRow = (CachedRow) pair.Value.BaseRow;

				if (cachedRow.Extent.Intersects(searchBox))
				{
					result.Add(cachedRow, cachedRow);
				}
			}

			return result;
		}

		private static void Register(
			[NotNull] ITable table,
			[NotNull] IDictionary<ITable, IDictionary<BaseRow, TestedRow>> overlapping,
			[NotNull] BaseRow cachedRow,
			[CanBeNull] IList<ContainerTest> reducedTests)
		{
			IDictionary<BaseRow, TestedRow> testedRows;

			if (! overlapping.TryGetValue(table, out testedRows))
			{
				//testedRows = new Dictionary<BaseRow, TestedRow>(_comparer);
				testedRows = LargeDictionaryFactory.CreateDictionary<BaseRow, TestedRow>(
					equalityComparer: _comparer);
				overlapping.Add(table, testedRows);
			}

			TestedRow testedRow;
			if (testedRows.TryGetValue(cachedRow, out testedRow))
			{
				testedRow.RegisterTested(reducedTests);
			}
			else
			{
				testedRows.Add(cachedRow, new TestedRow(cachedRow, reducedTests));
			}
		}

		private void ValidateCache([NotNull] BaseRow baseRow,
		                           [NotNull] IBox currentSearchBox)
		{
			if (MaxCachedPointCount < 0)
			{
				return;
			}

			var cachedRow = baseRow as CachedRow;
			if (cachedRow == null)
			{
				return;
			}

			if (cachedRow.Extent.Intersects(currentSearchBox))
			{
				// row is needed in currentSearchBox, 
				// if we would uncache it now it will be cached again during the next loading of features
				return;
			}

			_currentUncachableRows.Add(cachedRow);
			int pointCount = cachedRow.CachedPointCount;
			if (_currentCachedPointCount + pointCount < MaxCachedPointCount)
			{
				_currentCachedPointCount += pointCount;
			}
			else
			{
				cachedRow.ReleaseFeature();
			}
		}

		[NotNull]
		private static IBox GetSearchBox([NotNull] IBox currentTileBox,
		                                 double searchTolerance)
		{
			IBox searchBox = currentTileBox.Clone();

			searchBox.Min.X -= searchTolerance;
			searchBox.Min.Y -= searchTolerance;

			return searchBox;
		}

		[CanBeNull]
		private TestedRow GetTestedRow([NotNull] IFeature feature)
		{
			ITable table = feature.Table;
			if (table == null)
			{
				return null;
			}

			IDictionary<BaseRow, TestedRow> testedRows;
			if (! _overlappingRows.TryGetValue(table, out testedRows))
			{
				return null;
			}

			// TODO revise: no UniqueIdProvider passed, but rows in testedRows may use UniqueIdProvider
			var baseRow = new CachedRow(feature);
			TestedRow testedRow;
			return testedRows.TryGetValue(baseRow, out testedRow)
				       ? testedRow
				       : null;
		}

		#endregion

		#region Nested type: TestedRow

		private class TestedRow
		{
			//private int _occurrence;
			private IList<ContainerTest> _reducedTests;

			/// <summary>
			/// Initializes a new instance of the <see cref="TestedRow"/> class.
			/// </summary>
			/// <param name="cachedRow">The cached row.</param>
			/// <param name="reducedTests">The reduced tests.</param>
			public TestedRow([NotNull] BaseRow cachedRow,
			                 [CanBeNull] IList<ContainerTest> reducedTests)
			{
				_reducedTests = reducedTests;

				BaseRow = cachedRow;
				//_occurrence = 1;
			}

			public BaseRow BaseRow { get; }

			//public int Occurrence
			//{
			//    get { return _occurrence; }
			//}

			public bool HasNoRemainingOccurrence([NotNull] IBox tileBox)
			{
				if (BaseRow.Extent.Max.Y <= tileBox.Min.Y)
				{
					// the feature is fully below the current scan line
					return true;
				}

				if (BaseRow.Extent.Max.X <= tileBox.Min.X &&
				    BaseRow.Extent.Max.Y <= tileBox.Max.Y)
				{
					// the feature is left of the tile, and below max y
					return true;
				}

				return false;
			}

			public void RegisterTested([CanBeNull] IEnumerable<ContainerTest> reducedTests)
			{
				//_occurrence++;

				if (_reducedTests == null)
				{
					// the earlier occurrence had all tests applied, don't change
				}
				else
				{
					if (reducedTests == null)
					{
						// no reduced tests, all tests applied
						_reducedTests = null;
					}
					else
					{
						// the previous occurrences had a reduced list, and the current occurrence
						// also --> merge the two reduced lists
						var union = new List<ContainerTest>(_reducedTests);

						foreach (ContainerTest containerTest in reducedTests)
						{
							if (! union.Contains(containerTest))
							{
								union.Add(containerTest);
							}
						}

						_reducedTests = union;
					}
				}
			}

			public bool WasTestedFor([NotNull] ContainerTest containerTest)
			{
				return _reducedTests == null || _reducedTests.Contains(containerTest);
			}
		}

		#endregion
	}
}
