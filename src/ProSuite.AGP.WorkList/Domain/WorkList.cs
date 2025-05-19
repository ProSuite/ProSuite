using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Domain
{

	// Note: SelectionItemRepository ensures only items of selected rows are returned.
	//		 For DbStatusWorkItemRepository make sure not entire database is searched.
	//		 filter by AOI.
	// Note: Check that work list is not empty. Otherwise, GetWorkItemsForInnermostContextCore
	//		 will search forever.
	public abstract class WorkList : NotifyPropertyChangedBase, IWorkList, IEquatable<WorkList>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private static readonly object _obj = new();
		private static readonly int _initialCapacity = 1000;

		private readonly List<IWorkItem> _items = new(_initialCapacity);
		private readonly Dictionary<GdbRowIdentity, IWorkItem> _rowMap = new(_initialCapacity);

		private WorkItemVisibility? _visibility;
		[NotNull] private string _displayName;
		[CanBeNull] private EditEventsRowCacheSynchronizer _rowCacheSynchronizer;
		private bool _itemsGeometryDraftMode = true;
		private Envelope _extent;

		protected WorkList([NotNull] IWorkItemRepository repository,
		                   [NotNull] Geometry areaOfInterest,
		                   [NotNull] string name,
		                   [NotNull] string displayName)
		{
			Repository = repository ?? throw new ArgumentNullException(nameof(repository));
			AreaOfInterest = areaOfInterest ?? throw new ArgumentNullException(nameof(areaOfInterest));
			Name = name ?? throw new ArgumentNullException(nameof(name));

			_displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));

			CurrentIndex = repository.GetCurrentIndex();
		}

		public event EventHandler<WorkListChangedEventArgs> WorkListChanged;

		[NotNull]
		public IWorkItemRepository Repository { get; }

		[NotNull]
		public string Name { get; set; }

		[NotNull]
		public string DisplayName
		{
			get => _displayName;
			private set => SetProperty(ref _displayName, value);
		}

		[CanBeNull]
		public IWorkItem Current
		{
			get
			{
				IWorkItem current = GetItem(CurrentIndex);
				if (current == null)
				{
					return null;
				}

				// Ensure current item is always visited
				current.Visited = true;
				return current;
			}
		}

		public int CurrentIndex { get; set; }

		public WorkItemVisibility? Visibility
		{
			get => _visibility;
			set
			{
				// invalidate those items with invalid (old) status
				if (_visibility != value)
				{
					OnWorkListChanged();
				}

				_visibility = value;
			}
		}

		public long? TotalCount { get; set; }
		protected Geometry AreaOfInterest { get; }

		public virtual bool CanSetStatus()
		{
			return HasCurrentItem();
		}

		public async Task SetStatusAsync(IWorkItem item, WorkItemStatus status)
		{
			await Repository.SetStatusAsync(item, status);

			// If an item visibility changes to 'Done' the item might not be part
			// of the work list anymore depending on the work list's Visibility,
			// respectively GetItems() does not return the Done-item anymore.
			// Therefor use the item's Extent to invalidate the work list layer.
			OnWorkListChanged();
		}

		public bool IsValid(out string message)
		{
			if (Repository.SourceClasses.Count == 0)
			{
				message = "None of the referenced tables could be loaded";
				return false;
			}

			message = null;
			return true;
		}

		public void SetVisited(IList<IWorkItem> items, bool visited)
		{
			var oids = new List<long>(items.Count);

			foreach (IWorkItem item in items)
			{
				item.Visited = visited;
				oids.Add(item.OID);
			}

			OnWorkListChanged(null, oids);
		}

		public void Commit()
		{
			Repository.Commit();
		}

		[CanBeNull]
		public IAttributeReader GetAttributeReader(long forSourceClassId)
		{
			return Repository.SourceClasses
			                 .FirstOrDefault(sc => sc.GetUniqueTableId() == forSourceClassId)
			                 ?.AttributeReader;
		}

		public void Rename(string name)
		{
			DisplayName = name;
			Repository.WorkItemStateRepository.Rename(name);
		}

		[NotNull]
		public Envelope GetExtent()
		{
			if (_extent == null || _extent.IsEmpty)
			{
				return AreaOfInterest.Extent;
			}

			return _extent;
		}

		#region item geometry

		public void SetItemsGeometryDraftMode(bool enable)
		{
			_itemsGeometryDraftMode = enable;
		}

		public Geometry GetItemGeometry(IWorkItem item)
		{
			try
			{
				if (_itemsGeometryDraftMode)
				{
					// it's not a table row and not a point
					if (item.HasExtent && UseItemGeometry(item))
					{
						Assert.NotNull(item.Extent);
						return PolygonBuilderEx.CreatePolygon(item.Extent, item.Extent.SpatialReference);
					}
				}
				else
				{
					// it's not a table row and not a point
					if (item.HasFeatureGeometry && UseItemGeometry(item))
					{
						Assert.NotNull(item.Geometry);

						switch (item.GeometryType)
						{
							case GeometryType.Polyline:
								var polyline = (Polyline) item.Geometry;
								return PolygonBuilderEx.CreatePolygon(polyline, item.Geometry.SpatialReference);
							case GeometryType.Polygon:
								return (Polygon) item.Geometry;

							default:
								throw new ArgumentOutOfRangeException();
						}
					}
				}

				// it's a point
				if (item.HasExtent)
				{
					Assert.NotNull(item.Extent);
					return PolygonBuilderEx.CreatePolygon(item.Extent,
					                                      item.Extent.SpatialReference);
				}

				return null;
			}
			catch (Exception ex)
			{
				Gateway.LogError(ex, _msg);
			}

			return null;
		}

		private static bool UseItemGeometry([NotNull] IWorkItem item)
		{
			GeometryType? geometryType = item.GeometryType;
			return UseItemGeometry(geometryType);
		}

		private static bool UseItemGeometry(Geometry geometry)
		{
			GeometryType geometryType = geometry.GeometryType;
			return UseItemGeometry(geometryType);
		}

		private static bool UseItemGeometry(GeometryType? geometryType)
		{
			switch (geometryType)
			{
				case GeometryType.Polyline:
				case GeometryType.Polygon:
					return true;

				default:
					return false;
			}
		}

		#endregion

		#region GetItems

		public Row GetCurrentItemSourceRow()
		{
			if (Current == null)
			{
				return null;
			}

			ITableReference tableReference = Current.GdbRowProxy.Table;

			ISourceClass sourceClass =
				Repository.SourceClasses.FirstOrDefault(s => s.Uses(tableReference));

			if (sourceClass == null)
			{
				return null;
			}

			return Repository.GetSourceRow(sourceClass, Current.ObjectID);
		}

		public void UpdateExistingItemGeometries(QueryFilter filter)
		{
			// Don't update items already having feature geometry
			Predicate<IWorkItem> exclusion = item => item.HasFeatureGeometry;

			Stopwatch watch = Stopwatch.StartNew();

			int count = 0;
			foreach ((IWorkItem item, Geometry geometry) in Repository.GetItems(filter))
			{
				if (! UseItemGeometry(geometry))
				{
					continue;
				}

				count += UpdateExistingItemGeometry(item, geometry, exclusion);
			}

			// Avoid flooding the log.
			if (count > 0)
			{
				_msg.DebugStopTiming(watch, $"{count} item geometries updated.");
			}
			else
			{
				// Just to keep everything tidy and clean.
				watch.Stop();
			}
		}

		public IEnumerable<IWorkItem> Search([NotNull] QueryFilter filter)
		{
			// Don't query database here. IWorkItem.OID is the item id. IWorkItem.ObjectID is the
			// ObjectID of its source row. QueryFilter.ObjectIDs are the IWorkItem.OID and not
			// the IWorkItem.ObjectID We'd have to make a lookup: IWorkItem.OID > Table, ObjectID
			// to query database.

			Assert.True(_items.Count > 0, "no work items");

			if (filter.ObjectIDs.Count == 0)
			{
				return _items.AsEnumerable();
			}

			List<long> oids = filter.ObjectIDs.OrderBy(oid => oid).ToList();
			return _items.Where(item => oids.BinarySearch(item.OID) >= 0);
		}

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter = null)
		{
			filter ??= new QueryFilter();
			return GetItems(filter, GetStatus(Visibility), excludeGeometry: false);
		}

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter,
		                                       WorkItemStatus? itemStatus,
		                                       bool excludeGeometry = false)
		{
			double xmin = double.MaxValue, ymin = double.MaxValue, zmin = double.MaxValue;
			double xmax = double.MinValue, ymax = double.MinValue, zmax = double.MinValue;

			int newItemsCount = 0;

			foreach ((IWorkItem item, Geometry geometry) in Repository.GetItems(filter, itemStatus, excludeGeometry))
			{
				bool exists;
				IWorkItem cachedItem;

				lock (_obj)
				{
					exists = _rowMap.TryGetValue(item.GdbRowProxy, out cachedItem);
				}

				if (exists)
				{
					Assert.True(cachedItem.OID > 0, "item is not initialized");

					yield return cachedItem;
				}
				else
				{
					Assert.True(TryAddItem(item), $"Could not add {item}");

					// it's an unknown item > refresh it's state (status, visited) either
					// from DB (DbStatusWorkItem) or from definition file (SelectionItem).
					Repository.Refresh(item);

					newItemsCount++;

					if (geometry?.Extent is { IsEmpty: false })
					{
						// Set extent here whereas item feature geometry is set in UpdateItemFeatureGeometry()
						item.SetExtent(geometry.Extent);

						ComputeExtent(geometry.Extent,
						              ref xmin, ref ymin, ref zmin,
						              ref xmax, ref ymax, ref zmax);
					}

					yield return item;
				}
			}

			if (newItemsCount == 0)
			{
				yield break;
			}

			_msg.Debug($"Added {newItemsCount} new items to work list.");

			Assert.True(xmin > double.MinValue, "Cannot get coordinate");
			Assert.True(ymin > double.MinValue, "Cannot get coordinate");
			Assert.True(xmax < double.MaxValue, "Cannot get coordinate");
			Assert.True(ymax < double.MaxValue, "Cannot get coordinate");

			Envelope newExtent = EnvelopeBuilderEx.CreateEnvelope(new Coordinate3D(xmin, ymin, zmin),
																  new Coordinate3D(xmax, ymax, zmax),
																  AreaOfInterest.SpatialReference);

			// Spatial referenece of work list is defined by WorkItemTable.GetExtent().
			_extent = _extent == null ? newExtent : _extent.Union(newExtent);
		}

		private IEnumerable<IWorkItem> GetItems([NotNull] QueryFilter filter,
		                                        CurrentSearchOption currentSearch,
		                                        VisitedSearchOption visitedSearch)
		{
			IEnumerable<IWorkItem> query = GetItems(filter, GetStatus(Visibility));

			if (currentSearch == CurrentSearchOption.ExcludeCurrent)
			{
				query = query.Where(item => ! Equals(item, Current));
			}

			if (visitedSearch == VisitedSearchOption.ExcludeVisited)
			{
				query = query.Where(item => ! item.Visited);
			}

			return query;
		}

		private List<IWorkItem> GetItems(SpatialQueryFilter filter,
		                                 CurrentSearchOption currentSearch,
		                                 VisitedSearchOption visitedSearch, int maxTrials)
		{
			List<IWorkItem> result = GetItems(filter, currentSearch, visitedSearch).ToList();

			Envelope filterExtent = filter.FilterGeometry.Extent;

			while (result.Count == 0)
			{
				filter = GdbQueryUtils.CreateSpatialFilter(filterExtent);

				result = GetItems(filter, currentSearch, visitedSearch).ToList();

				if (maxTrials == 0)
				{
					_msg.Debug($"Stop searching items after {maxTrials} tries.");
					break;
				}

				if (result.Count == 0)
				{
					maxTrials--;
					filterExtent = filterExtent.Expand(2, 2, true);
				}
			}

			return result;
		}

		// todo: (daro) to utils?
		private static WorkItemStatus? GetStatus(WorkItemVisibility? visibility)
		{
			switch (visibility)
			{
				case WorkItemVisibility.Todo:
					return WorkItemStatus.Todo;
				case WorkItemVisibility.Done:
					return WorkItemStatus.Done;
				case WorkItemVisibility.All:
				case null:
					return null;
				default:
					throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
			}
		}

		#endregion

		public int CountLoadedItems()
		{
			return _items.Count;
		}

		public long CountLoadedItems(out int todo)
		{
			todo = 0;
			todo += _items.Count(item => item.Status == WorkItemStatus.Todo);

			return _items.Count;
		}

		public void ComputeTotalCount()
		{
			Assert.Null(TotalCount);
			TotalCount = Repository.Count();
		}

		#region Navigation public

		public virtual bool CanGoFirst()
		{
			try
			{
				return GetFirstVisibleVisitedItemBeforeCurrent() != null;
			}
			catch (Exception e)
			{
				_msg.Error("Error in CanGoFirst", e);
			}

			return false;
		}

		public virtual void GoFirst()
		{
			IWorkItem current = GetItem(CurrentIndex);

			IWorkItem first = GetFirstVisibleVisitedItemBeforeCurrent();

			Assert.NotNull(first);
			Assert.False(Equals(first, Current), "current item and first item are equal");

			SetCurrentItemCore(first, current);
		}

		public virtual bool CanGoNearest()
		{
			try
			{
				int currentIndex = CurrentIndex;
				var index = 0;
				foreach (IWorkItem workItem in _items)
				{
					if (IsVisible(workItem) && workItem.Status == WorkItemStatus.Todo)
					{
						if (! workItem.Visited)
						{
							return true;
						}

						if (index > currentIndex)
						{
							// allow go to nearest if there are visited 'Todo'
							// items *after* the current one
							return true;
						}
					}

					index++;
				}

				// fewer items loaded than TotalCount
				if (_items.Count < TotalCount)
				{
					return true;
				}
			}
			catch (Exception e)
			{
				_msg.Error("Error in CanGoNearest", e);
			}

			return false;
		}

		public virtual void GoNearest(Geometry reference,
		                              Predicate<IWorkItem> match = null,
		                              params Polygon[] contextPerimeters)
		{
			Assert.ArgumentNotNull(reference, nameof(reference));

			Stopwatch watch = _msg.DebugStartTiming();

			// first, try to go to an unvisited item
			bool found = TryGoNearest(contextPerimeters, reference,
			                          VisitedSearchOption.ExcludeVisited);

			if (! found)
			{
				// if none found, search also the visited ones, but
				// only those *after* the current item
				found = TryGoNearest(contextPerimeters, reference,
				                     VisitedSearchOption.IncludeVisited);
			}

			if (! found && HasCurrentItem() && Current != null)
			{
				ClearCurrentItem(Current);
			}

			_msg.DebugStopTiming(watch, nameof(GoNearest));
		}

		public virtual bool CanGoNext()
		{
			try
			{
				return GetNextVisitedVisibleItem() != null;
			}
			catch (Exception e)
			{
				_msg.Error("Error in CanGoNext", e);
			}

			return false;
		}

		public virtual void GoNext()
		{
			IWorkItem next = GetNextVisitedVisibleItem();

			Assert.NotNull(next);
			Assert.False(Equals(next, Current), "current item and next item are equal");

			SetCurrentItemCore(next, Current);
		}

		public virtual bool CanGoPrevious()
		{
			try
			{
				return GetPreviousVisitedVisibleItem() != null;
			}
			catch (Exception e)
			{
				_msg.Error("Error in CanGoPrevious", e);
			}

			return false;
		}

		public virtual void GoPrevious()
		{
			IWorkItem previous = GetPreviousVisitedVisibleItem();

			Assert.NotNull(previous);
			Assert.False(Equals(previous, Current), "current item and previous item are equal");

			SetCurrentItemCore(previous, Current);
		}

		public virtual void GoTo(long oid)
		{
			if (Current?.OID == oid)
			{
				return;
			}

			var filter = new QueryFilter { ObjectIDs = new[] { oid } };
			IWorkItem target = Search(filter).FirstOrDefault();

			if (target != null)
			{
				SetCurrentItem(target, Current);
			}
		}

		#endregion
		
		#region Navigation non-public

		private bool TryGoNearest([NotNull] Polygon[] contextPerimeters,
		                          [NotNull] Geometry reference,
		                          VisitedSearchOption visitedSearchOption)
		{
			IList<IWorkItem> candidates =
				GetWorkItemsForInnermostContext(contextPerimeters,
				                                visitedSearchOption);
			if (candidates.Count > 0)
			{
				IWorkItem nearest = GetNearest(reference, candidates);

				if (nearest != null)
				{
					SetCurrentItem(nearest, Current);
					return true;
				}
			}

			return false;
		}

		[NotNull]
		private IList<IWorkItem> GetWorkItemsForInnermostContext([NotNull] Polygon[] perimeters,
		                                                         VisitedSearchOption visitedSearch)
		{
			Assert.ArgumentNotNull(perimeters, nameof(perimeters));

			_msg.VerboseDebug(() => $"Getting work items for innermost context ({perimeters.Length} perimeters)");

			const CurrentSearchOption currentSearch = CurrentSearchOption.ExcludeCurrent;

			for (var index = 0; index < perimeters.Length; index++)
			{
				Polygon intersection = GetIntersection(perimeters, index);

				if (intersection.IsEmpty)
				{
					// continue with next perimeter
				}
				else
				{
					// Possible optimization: do one search, 
					// and qualify each candidate with "intersects" / "within" 
					// --> filter result

					var workItems =
						GetItems(GdbQueryUtils.CreateSpatialFilter(intersection, SpatialRelationship.Contains), currentSearch, visitedSearch).ToList();

					if (workItems.Count == 0)
					{
						_msg.VerboseDebug(() => "The intersection contains no items, searching partially contained items");
						
						workItems =
							GetItems(GdbQueryUtils.CreateSpatialFilter(intersection), currentSearch, visitedSearch).ToList();
					}

					_msg.VerboseDebug(() => $"{workItems.Count} work item(s) found");

					if (workItems.Count > 0)
					{
						return workItems;
					}

					// else: continue with next perimeter
				}
			}

			// Nothing found so far. Union perimeters and pass filter on to specific work list implementation.
			Geometry union = GeometryUtils.Union(perimeters);
			Assert.NotNull(union);
			Assert.False(union.IsEmpty, "union is empty");

			// Ensure not entire database is searched, filter by AOI.
			List<IWorkItem> candidates = GetItems(GdbQueryUtils.CreateSpatialFilter(union.Extent), currentSearch, visitedSearch, 50);

			if (candidates.Count > 0)
			{
				return candidates;
			}

			// Still nothing found. Let core method try with work list specific logic.
			// Ensure not entire database is searched, filter by AOI.
			return GetWorkItemsForInnermostContextCore(new QueryFilter(), currentSearch, visitedSearch).ToList();
		}

		protected virtual IEnumerable<IWorkItem> GetWorkItemsForInnermostContextCore(
			[NotNull] QueryFilter filter,
			CurrentSearchOption currentSearch,
			VisitedSearchOption visitedSearch)
		{
			return GetItems(filter, currentSearch, visitedSearch);
		}

		[NotNull]
		private static Polygon GetIntersection([NotNull] Polygon[] perimeters, int index)
		{
			Assert.ArgumentNotNull(perimeters, nameof(perimeters));

			_msg.VerboseDebug(() => $"Intersecting perimeters {index} to {perimeters.Length - 1}");

			Polygon intersection = GeometryFactory.Clone(perimeters[index]);

			// intersect with all following perimeters, if any
			int nextIndex = index + 1;

			if (nextIndex < perimeters.Length)
			{
				for (int combineIndex = nextIndex;
				     combineIndex < perimeters.Length;
				     combineIndex++)
				{
					Polygon projectedPerimeter = perimeters[combineIndex];

					if (intersection.IsEmpty)
					{
						// no further intersection, result is empty
						return intersection;
					}

					if (projectedPerimeter.IsEmpty)
					{
						// no further intersection, result is empty
						return projectedPerimeter;
					}

					// both are not empty; calculate intersection
					try
					{
						intersection =
							(Polygon) GeometryEngine.Instance.Intersection(
								intersection, projectedPerimeter,
								GeometryDimensionType.EsriGeometry2Dimension);
					}
					catch (Exception e)
					{
						try
						{
							_msg.ErrorFormat(
								"Error intersecting perimeters ({0}). See log file for details",
								e.Message);

							_msg.DebugFormat("Perimeter index={0}:", combineIndex);

							_msg.DebugFormat(
								"Input intersection at nextIndex={0}:", nextIndex);

							// leave intersection as is, and continue
						}
						catch (Exception e1)
						{
							_msg.Warn("Error writing details to log", e1);
						}
					}
				}
			}

			return intersection;
		}

		[CanBeNull]
		private IWorkItem GetNearest([NotNull] Geometry reference,
		                             [NotNull] IEnumerable<IWorkItem> candidates)
		{
			Assert.ArgumentNotNull(reference, nameof(reference));
			Assert.ArgumentNotNull(candidates, nameof(candidates));

			Geometry searchReference = GetNearestSearchReference(reference);

			if (searchReference == null)
			{
				_msg.Warn("Invalid geometry, unable to find nearest work item");
				return null;
			}

			GeometryType referenceGeometryType = searchReference.GeometryType;

			Geometry referenceGeometry;

			if (referenceGeometryType == GeometryType.Polygon ||
			    referenceGeometryType == GeometryType.Envelope)
			{
				// for polygons and envelopes it does not make much sense to search from the 
				// boundary; search from centroid instead. This also prevents
				// the extreme response times (minutes) of ReturnDistance() from
				// very large polygons
				//referenceProximity = (IProximityOperator)((IArea)searchReference).Centroid;

				referenceGeometry = GeometryEngine.Instance.Centroid(searchReference);
			}
			else
			{
				referenceGeometry = searchReference;
			}

			double minDistance = double.MaxValue;
			IWorkItem nearest = null;
			IWorkItem firstWithoutGeometry = null;
			IWorkItem current = Current;

			foreach (IWorkItem item in candidates)
			{
				if (item == current)
				{
					// current item, ignore
				}
				else
				{
					if (item.HasExtent)
					{
						Envelope otherExtent = Assert.NotNull(item.Extent);

						// IWorkItem.Extent from SDE (and reported from ALGR from occasionally from issues.gdb) seems to
						// to have an unequal SR compared to the referenceGeometry which is the MapView.Current.Extent
						// when the work list ist opened for the first time.
						// EMA: while editing the SR resolution might be set to a very small value. This probably does
						// not happen from FGDB data.
						// DARO: find a better solution than reprojecting in foreach loop
						Geometry projected =
							GeometryUtils.EnsureSpatialReference(
								referenceGeometry, otherExtent.SpatialReference);

						double distance;
						if (GeometryUtils.Disjoint(projected, otherExtent))
						{
							distance = GeometryEngine.Instance.Distance(projected, otherExtent);
						}
						else
						{
							//MapPoint otherCentroid = GeometryEngine.Instance.Centroid(otherExtent);
							distance =
								GeometryEngine.Instance.Distance(projected, otherExtent.Center);
						}

						if (distance < minDistance)
						{
							minDistance = distance;
							nearest = item;
						}
					}
					else
					{
						// item without geometry
						firstWithoutGeometry ??= item;
					}
				}
			}

			return nearest ?? firstWithoutGeometry;
		}

		[CanBeNull]
		private Geometry GetNearestSearchReference([NotNull] Geometry reference)
		{
			MapPoint centroid = null;

			try
			{
				if (! reference.IsEmpty)
				{
					centroid = GeometryEngine.Instance.Centroid(reference);
				}
			}
			catch (Exception e)
			{
				_msg.Debug("Error trying to get centroid of geometry", e);
			}

			if (centroid == null)
			{
				return null;
			}

			reference = centroid;

			return reference;
		}

		/// <summary>
		///     Sets given work item as the current one. Updates the current item
		///     index and sets the work item as visited.
		/// </summary>
		/// <param name="nextItem"></param>
		/// <param name="currentItem">The work item.</param>
		private void SetCurrentItem([NotNull] IWorkItem nextItem, IWorkItem currentItem = null)
		{
			ReorderCurrentItem(nextItem);

			SetCurrentItemCore(nextItem, currentItem);
		}

		private void SetCurrentItemCore([NotNull] IWorkItem nextItem, IWorkItem currentItem = null)
		{
			nextItem.Visited = true;
			CurrentIndex = _items.IndexOf(nextItem);

			Repository.SetCurrentIndex(CurrentIndex);
			Repository.UpdateState(nextItem);

			var oids = currentItem != null
				           ? new List<long> { nextItem.OID, currentItem.OID }
				           : new List<long> { nextItem.OID };

			OnWorkListChanged(null, oids);
		}

		private void ReorderCurrentItem([NotNull] IWorkItem nextItem)
		{
			// move new item to just after previous current
			int insertIndex = GetReorderInsertIndex(nextItem);

			_msg.Debug($"Reorder visited items: {nextItem}, insert index: {insertIndex}");

			WorkListUtils.MoveTo(_items, nextItem, insertIndex);
		}

		private int GetReorderInsertIndex([NotNull] IWorkItem nextItem)
		{
			int firstUnvisitedIndex = _items.FindIndex(item => ! item.Visited);

			int currentItemIndex = _items.IndexOf(nextItem);

			// no unvisited item anymore, don't reorder, just go through the list
			if (firstUnvisitedIndex < 0)
			{
				return currentItemIndex;
			}

			// move the current item to the first unvisited item index in the list
			if (firstUnvisitedIndex <= currentItemIndex)
			{
				return firstUnvisitedIndex;
			}

			return firstUnvisitedIndex == 0
				       ? 0
				       : firstUnvisitedIndex - 1;
		}

		[CanBeNull]
		private IWorkItem GetFirstVisibleVisitedItemBeforeCurrent()
		{
			IWorkItem currentItem = Current;

			foreach (IWorkItem workItem in _items)
			{
				// search for the first visible work item before the 
				// current one
				if (workItem == currentItem)
				{
					// found the current one, stop search
					//continue;
					return null;
				}

				if (! IsVisible(workItem))
				{
					continue;
				}

				if (! workItem.Visited)
				{
					if (currentItem != null)
					{
						// unexpected
						//_msg.Warn("Previous work item not visited");
					}

					return null;
				}

				// not the current one, visited
				return workItem;
			}

			// no visible work items
			return null;
		}

		[CanBeNull]
		private IWorkItem GetNextVisitedVisibleItem()
		{
			if (CurrentIndex >= _items.Count - 1)
			{
				// last item reached
				return null;
			}

			// true if another visible, visited item comes afterwards
			for (int i = CurrentIndex + 1; i < _items.Count; i++)
			{
				IWorkItem item = _items[i];
				if (item.Visited && IsVisible(item))
				{
					return item;
				}
			}

			return null;
		}

		[CanBeNull]
		private IWorkItem GetPreviousVisitedVisibleItem()
		{
			if (CurrentIndex <= 0)
			{
				// no previous item anymore, current is first item
				return null;
			}

			if (CurrentIndex > _items.Count)
			{
				// Items have been removed or could not be loaded at all
				return null;
			}

			for (int i = CurrentIndex - 1; i >= 0; i--)
			{
				IWorkItem item = _items[i];
				if (item.Visited && IsVisible(item))
				{
					return item;
				}
			}

			return null;
		}

		#endregion

		#region Non-public methods

		[CanBeNull]
		private IWorkItem GetItem(int index)
		{
			return 0 <= index && index < _items.Count
				       ? Assert.NotNull(_items[index])
				       : null;
		}

		private static void ComputeExtent(Envelope extent,
		                                  ref double xmin, ref double ymin,
		                                  ref double zmin, ref double xmax,
		                                  ref double ymax, ref double zmax)
		{
			if (extent == null) return;
			if (extent.IsEmpty) return;

			if (extent.XMin < xmin) xmin = extent.XMin;
			if (extent.YMin < ymin) ymin = extent.YMin;
			if (extent.ZMin < zmin) zmin = extent.ZMin;

			if (extent.XMax > xmax) xmax = extent.XMax;
			if (extent.YMax > ymax) ymax = extent.YMax;
			if (extent.ZMax > zmax) zmax = extent.ZMax;
		}

		private bool IsVisible([NotNull] IWorkItem item)
		{
			return IsVisible(item, Visibility);
		}

		private bool IsVisible([NotNull] IWorkItem item, WorkItemVisibility? visibility)
		{
			WorkItemStatus status = item.Status;

			switch (visibility)
			{
				case null:
					return false;
				case WorkItemVisibility.Todo:
					return (status & WorkItemStatus.Todo) != 0;
				case WorkItemVisibility.Done:
					return (status & WorkItemStatus.Done) != 0;
				case WorkItemVisibility.All:
					return true;
				default:
					throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
			}
		}

		#endregion

		private void OnWorkListChanged([CanBeNull] Envelope extent = null,
		                               [CanBeNull] List<long> oids = null)
		{
			WorkListChanged?.Invoke(this, new WorkListChangedEventArgs(extent, oids));
		}

		#region IRowCache

		public void EnsureRowCacheSynchronized()
		{
			if (_rowCacheSynchronizer != null)
			{
				return;
			}

			_rowCacheSynchronizer = new EditEventsRowCacheSynchronizer(this);
		}

		public void DeactivateRowCacheSynchronization()
		{
			_rowCacheSynchronizer?.Dispose();
			_rowCacheSynchronizer = null;
		}

		/// <summary>
		/// Raises WorkListChanged event: forces
		/// to redraw at least ActiveView.Active.Extent which leads
		/// to a GetItems() call.
		/// </summary>
		public void Invalidate()
		{
			_msg.Debug("Invalidate");

			OnWorkListChanged();
		}

		/// <summary>
		/// Raises WorkListChanged event: forces
		/// to redraw the map which leads to a GetItems() call.
		/// </summary>
		/// <param name="geometry">The area to invalidate the work list layer.</param>
		public void Invalidate(Envelope geometry)
		{
			_msg.Debug("Invalidate by geometry");

			OnWorkListChanged(geometry.Extent);
		}

		/// <summary>
		/// Triggers WorkItemTable.Search(QueryFilter)
		/// </summary>
		/// <param name="oids">List of work item OIDs (not ObjectID</param>
		public void Invalidate(List<long> oids)
		{
			OnWorkListChanged(null, oids);
		}

		public void Invalidate(IEnumerable<Table> tables)
		{
			// TODO: (daro) still needed?
			// TODO: More fine-granular invalidation, consider separate row cache containing
			// _rowMap, _items.
			Invalidate();
		}

		public bool CanContain(Table table)
		{
			return Repository.SourceClasses.Any(s => s.TableIdentity.ReferencesTable(table));
		}

		public void ProcessChanges(Dictionary<Table, List<long>> inserts,
		                           Dictionary<Table, List<long>> deletes,
		                           Dictionary<Table, List<long>> updates)
		{
			_msg.Debug(
				$"ProcessChanges - {inserts.Count} inserts, {deletes.Count} deletes, {updates.Count} updates.");

			foreach ((Table table, List<long> oids) in inserts)
			{
				Invalidate(ProcessInserts(table, oids));
			}

			foreach ((Table table, List<long> oids) in updates)
			{
				Invalidate(ProcessUpdates(table, oids));
			}

			foreach ((Table table, List<long> oids) in deletes)
			{
				ProcessDeletes(table, oids);

				// Do not invalidate with item OID because the item has already
				// been removed from _items.
				Invalidate();
			}
		}

		private List<long> ProcessInserts(Table table, List<long> oids)
		{
			_msg.Debug("ProcessInserts");

			var invalidateOids = new List<long>(oids.Count);

			try
			{
				double xmin = double.MaxValue, ymin = double.MaxValue, zmin = double.MaxValue;
				double xmax = double.MinValue, ymax = double.MinValue, zmax = double.MinValue;

				QueryFilter filter = GdbQueryUtils.CreateFilter(oids);
				Stopwatch watch = Stopwatch.StartNew();

				SpatialReference sref = null;
				int newItemsCount = 0;
				foreach ((IWorkItem item, Geometry geometry) in Repository.GetItems(table, filter, null))
				{
					Assert.True(TryAddItem(item), $"Could not add {item}");

					// it's a unkown item > refresh it's state (status, visited) either
					// from DB (DbStatusWorkItem) or from definition file (SelectionItem).
					Repository.Refresh(item);

					newItemsCount++;

					// add oid after add item to _items
					invalidateOids.Add(item.OID);

					UpdateItemGeometry(item, geometry);

					if (geometry == null)
					{
						continue;
					}

					if (geometry.Extent is { IsEmpty: false })
					{
						sref = geometry.SpatialReference;
					}

					ComputeExtent(geometry.Extent,
					              ref xmin, ref ymin, ref zmin,
					              ref xmax, ref ymax, ref zmax);
				}

				// Avoid flooding the log.
				if (newItemsCount > 0)
				{
					_msg.DebugStopTiming(watch, $"{newItemsCount} item geometries updated.");
				}
				else
				{
					// Just to keep everything tidy and clean.
					watch.Stop();
				}

				_msg.Debug($"Added {newItemsCount} new items to work list.");

				Assert.True(newItemsCount > 0, "Could not add items");
				Assert.True(xmin > double.MinValue, "Cannot get coordinate");
				Assert.True(ymin > double.MinValue, "Cannot get coordinate");
				Assert.True(xmax < double.MaxValue, "Cannot get coordinate");
				Assert.True(ymax < double.MaxValue, "Cannot get coordinate");
				Assert.NotNull(_extent);

				Envelope newExtent =
					EnvelopeBuilderEx.CreateEnvelope(new Coordinate3D(xmin, ymin, zmin),
					                                 new Coordinate3D(xmax, ymax, zmax),
					                                 sref);

				_extent = _extent.Union(newExtent);
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return invalidateOids;
		}

		private void ProcessDeletes(Table table, List<long> oids)
		{
			_msg.Debug("ProcessDeletes");

			try
			{
				GdbTableIdentity tableId = new GdbTableIdentity(table);

				foreach (long oid in oids)
				{
					var rowId = new GdbRowIdentity(oid, tableId);

					bool exists;
					lock (_obj)
					{
						exists = _rowMap.ContainsKey(rowId);
					}

					if (Current != null && exists)
					{
						Assert.True(HasCurrentItem(), $"{nameof(HasCurrentItem)} is false");
						ClearCurrentItem(Current);
					}

					// to invalidate item remove it from work list cache
					lock (_obj)
					{
						bool invalidated = _rowMap.Remove(rowId, out IWorkItem item) && _items.Remove(item);
						Assert.True(invalidated, $"Invalidate work item failed: {rowId} not part of work list");
					}
				}

				Assert.NotNull(_extent);
				_extent = CreateExtent(_items, _extent.SpatialReference);
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}
		}

		private List<long> ProcessUpdates(Table table, List<long> oids)
		{
			_msg.Debug("ProcessUpdate");

			var invalidateOids = new List<long>(oids.Count);

			try
			{
				QueryFilter filter = GdbQueryUtils.CreateFilter(oids);
				Stopwatch watch = Stopwatch.StartNew();

				int count = 0;
				foreach ((IWorkItem item, Geometry geometry) in Repository.GetItems(table, filter, null))
				{
					if (! TryGetItem(item.GdbRowProxy, out IWorkItem cachedItem))
					{
						continue;
					}

					count += UpdateItemGeometry(cachedItem, geometry);

					invalidateOids.Add(cachedItem.OID);
				}

				// Avoid flooding the log.
				if (count > 0)
				{
					_msg.DebugStopTiming(watch, $"{count} item geometries updated.");
				}
				else
				{
					// Just to keep everything tidy and clean.
					watch.Stop();
				}

				Assert.NotNull(_extent);
				_extent = CreateExtent(_items, _extent.SpatialReference);
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return invalidateOids;
		}

		#endregion

		private int UpdateExistingItemGeometry([NotNull] IWorkItem item,
		                                       [CanBeNull] Geometry geometry,
		                                       Predicate<IWorkItem> exclusion = null)
		{
			if (geometry == null)
			{
				return 0;
			}

			bool exists = TryGetItem(item.GdbRowProxy, out IWorkItem cachedItem);

			if (! exists)
			{
				return 0;
			}

			Assert.True(cachedItem.OID > 0, "item is not initialized");

			return UpdateItemGeometry(cachedItem, geometry, exclusion);
		}

		private static int UpdateItemGeometry([NotNull] IWorkItem item,
		                                      [CanBeNull] Geometry geometry,
		                                      Predicate<IWorkItem> exclusion = null)
		{
			if (geometry == null)
			{
				return 0;
			}

			if (exclusion != null && exclusion(item))
			{
				return 0;
			}

			if (UseItemGeometry(geometry))
			{
				item.SetGeometry(GeometryUtils.Buffer(geometry, 10));
			}
			else
			{
				item.SetExtent(geometry.Extent);
			}

			return 1;
		}

		private bool TryAddItem(IWorkItem item)
		{
			Assert.True(item.OID <= 0, "item is already initialized");
			item.OID = Repository.GetNextOid();

			lock (_obj)
			{
				if (! _rowMap.TryAdd(item.GdbRowProxy, item))
				{
					return false;
				}

				_items.Add(item);
				return true;
			}
		}

		private bool TryGetItem(GdbRowIdentity rowProxy, out IWorkItem cachedItem)
		{
			bool exists;

			lock (_obj)
			{
				exists = _rowMap.TryGetValue(rowProxy, out cachedItem);
			}

			return exists;
		}

		private void ClearCurrentItem([NotNull] IWorkItem current)
		{
			Assert.ArgumentNotNull(current, nameof(current));

			if (CurrentIndex < 0)
			{
				return;
			}

			CurrentIndex = -1;

			OnWorkListChanged(null, new List<long> { current.OID });
		}

		// TODO: (daro) to Utils?
		private static Envelope CreateExtent(List<IWorkItem> items, SpatialReference sref = null)
		{
			double xmin = double.MaxValue, ymin = double.MaxValue, zmin = double.MaxValue;
			double xmax = double.MinValue, ymax = double.MinValue, zmax = double.MinValue;

			foreach (IWorkItem item in items)
			{
				ComputeExtent(item.Extent,
				              ref xmin, ref ymin,
				              ref zmin, ref xmax,
				              ref ymax, ref zmax);
			}

			Assert.True(xmin > double.MinValue, "Cannot get coordinate");
			Assert.True(ymin > double.MinValue, "Cannot get coordinate");
			Assert.True(xmax < double.MaxValue, "Cannot get coordinate");
			Assert.True(ymax < double.MaxValue, "Cannot get coordinate");

			return EnvelopeBuilderEx.CreateEnvelope(new Coordinate3D(xmin, ymin, zmin),
			                                        new Coordinate3D(xmax, ymax, zmax), sref);
		}

		private bool HasCurrentItem()
		{
			return CurrentIndex >= 0 &&
			       CurrentIndex < _items.Count;
		}

		#region IEquatable implementation

		public bool Equals(WorkList other)
		{
			return string.Equals(Name, other?.Name, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((WorkList) obj);
		}

		public override int GetHashCode()
		{
			// ReSharper disable once NonReadonlyMemberInGetHashCode
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
		}

		#endregion
	}
}
