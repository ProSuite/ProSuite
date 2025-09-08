using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
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

		private List<IWorkItem> _items = new(_initialCapacity);
		private ConcurrentDictionary<GdbRowIdentity, IWorkItem> _rowMap = new();

		private WorkItemVisibility? _visibility;
		[NotNull] private string _displayName;

		protected WorkList([NotNull] IWorkItemRepository repository,
		                   [CanBeNull] Geometry areaOfInterest,
		                   [NotNull] string name,
		                   [NotNull] string displayName)
		{
			Assert.NotNull(nameof(repository));
			Assert.NotNullOrEmpty(name, nameof(name));
			Assert.NotNullOrEmpty(displayName, nameof(displayName));

			Repository = repository;
			Repository.AreaOfInterest = areaOfInterest;

			Name = name;

			_displayName = displayName;

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
		public IWorkItem CurrentItem
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

		[CanBeNull]
		protected Geometry AreaOfInterest => Repository.AreaOfInterest;

		public double MinimumScaleDenominator { get; set; }

		public bool AlwaysUseDraftMode { get; set; } = true;

		public bool CacheBufferedItemGeometries { get; set; }

		public double ItemDisplayBufferDistance { get; set; }

		public int? MaxBufferedItemCount { get; set; }

		public int? MaxBufferedShapePointCount { get; set; }

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
			// TODO: (DARO) still needed?
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

		public Envelope Extent
		{
			get
			{
				Envelope extent = Repository.Extent;
				if (extent == null || extent.IsEmpty)
				{
					return AreaOfInterest?.Extent;
				}

				return extent;
			}
		}

		#region item geometry

		public void SetItemsGeometryDraftMode(bool enable)
		{
			AlwaysUseDraftMode = enable;
		}

		public Geometry GetItemDisplayGeometry(IWorkItem item)
		{
			try
			{
				if (item == null)
				{
					return null;
				}

				// In draft mode, use extent-based geometry for bufferable items
				if (AlwaysUseDraftMode)
				{
					return CreateExtentGeometry(item);
				}

				// In non-draft mode, try to get detailed geometry
				if (TryGetDetailedGeometry(item, out Geometry detailedGeometry))
				{
					return detailedGeometry;
				}

				// Fallback to extent-based geometry
				return CreateExtentGeometry(item);
			}
			catch (Exception ex)
			{
				_msg.Warn($"Error calculating work item geometry: {ex.Message}", ex);
			}

			return null;
		}

		private static Geometry CreateExtentGeometry(IWorkItem item)
		{
			if (! item.HasExtent)
				return null;

			Assert.NotNull(item.Extent);
			return PolygonBuilderEx.CreatePolygon(item.Extent, item.Extent.SpatialReference);
		}

		private bool TryGetDetailedGeometry([NotNull] IWorkItem item, out Geometry geometry)
		{
			geometry = null;

			// Use cached buffered geometry if available
			if (CacheBufferedItemGeometries &&
			    item.HasBufferedGeometry &&
			    CanUseBufferedGeometryFor(item))
			{
				geometry = GetPolygonGeometry(item);
				return true;
			}

			// For current item, try to load geometry from source (single buffer is fast)
			if (item.GdbRowProxy.Equals(CurrentItem?.GdbRowProxy))
			{
				if (item.HasBufferedGeometry)
				{
					geometry = GetPolygonGeometry(item);
					return true;
				}

				// Try to load geometry from database
				if (GetCurrentItemSourceRow(false) is Feature feature)
				{
					UpdateItemDisplayGeometry(item, feature.GetShape());
				}

				if (item.HasBufferedGeometry)
				{
					geometry = GetPolygonGeometry(item);
					return true;
				}
			}

			return false;
		}

		private static Geometry GetPolygonGeometry(IWorkItem item)
		{
			Assert.NotNull(item.BufferedGeometry);

			switch (item.BufferedGeometry.GeometryType)
			{
				case GeometryType.Polyline:
					var polyline = (Polyline) item.BufferedGeometry;
					return PolygonBuilderEx.CreatePolygon(
						polyline, item.BufferedGeometry.SpatialReference);
				case GeometryType.Polygon:
					return (Polygon) item.BufferedGeometry;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static bool CanUseBufferedGeometryFor([NotNull] IWorkItem item)
		{
			GeometryType? geometryType = item.SourceGeometryType;
			return CanUseBufferedGeometryFor(geometryType);
		}

		private bool CanUseBufferedGeometryFor([CanBeNull] Geometry geometry)
		{
			if (geometry == null)
			{
				return false;
			}

			GeometryType geometryType = geometry.GeometryType;

			if (! CanUseBufferedGeometryFor(geometryType))
			{
				return false;
			}

			int pointCount = GeometryUtils.GetPointCount(geometry);

			if (pointCount > MaxBufferedShapePointCount)
			{
				return false;
			}

			// NOTE: polycurve.HasCurves has been observed to return incorrect values. Also
			//       buffering a single item is less performance-critical.
			//if (geometry is Multipart polycurve && polycurve.HasCurves)
			//{
			//	// creating the buffer for non-linear segments is extremely expensive
			//	return false;
			//}

			return true;
		}

		private static bool CanUseBufferedGeometryFor(GeometryType? geometryType)
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

		[CanBeNull] private SpatialHashSearcher<IWorkItem> _searcher;

		public Row GetCurrentItemSourceRow(bool readOnly = true)
		{
			if (CurrentItem == null)
			{
				return null;
			}

			ITableReference tableReference = CurrentItem.GdbRowProxy.Table;

			ISourceClass sourceClass =
				Repository.SourceClasses.FirstOrDefault(s => s.Uses(tableReference));

			if (sourceClass == null)
			{
				return null;
			}

			return Repository.GetSourceRow(sourceClass, CurrentItem.ObjectID, readOnly);
		}

		public void UpdateExistingItemGeometries(QueryFilter filter)
		{
			// Don't update items already having feature geometry
			Predicate<IWorkItem> exclusion = item => item.HasBufferedGeometry;

			Stopwatch watch = Stopwatch.StartNew();

			int count = 0;
			foreach ((IWorkItem item, Geometry geometry) in Repository.GetItems(filter))
			{
				if (! CanUseBufferedGeometryFor(geometry))
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

		public void LoadItems()
		{
			QueryFilter filter = AreaOfInterest != null
				                     ? GdbQueryUtils.CreateSpatialFilter(AreaOfInterest)
				                     : new QueryFilter();

			WorkItemStatus? status = GetStatus(Visibility);

			string aoiText = AreaOfInterest != null ? " within area of interest" : string.Empty;

			_msg.InfoFormat("Loading work list items for {0}{1}...", DisplayName, aoiText);

			LoadItems(filter, status);

			_msg.InfoFormat("Loaded {0} work list items for {1}.", _items.Count,
			                DisplayName);

			TotalCount = _items.Count;
		}

		public void LoadItems([NotNull] QueryFilter filter, WorkItemStatus? statusFilter = null)
		{
			double xmin = double.MaxValue, ymin = double.MaxValue, zmin = double.MaxValue;
			double xmax = double.MinValue, ymax = double.MinValue, zmax = double.MinValue;

			var rowMap = new ConcurrentDictionary<GdbRowIdentity, IWorkItem>();
			var itemsWithExtent = new List<IWorkItem>();

			Stopwatch watch = _msg.DebugStartTiming($"{this} start loading items.");

			foreach ((IWorkItem item, Geometry geometry) in Repository.GetItems(
				         filter, statusFilter))
			{
				item.OID = Repository.GetNextOid();
				Assert.True(item.OID > 0, "item is not initialized");

				Assert.True(rowMap.TryAdd(item.GdbRowProxy, item), $"Could not add {item}");

				// it's an unknown item > refresh it's state (status, visited) either
				// from DB (DbStatusWorkItem) or from definition file (SelectionItem).
				Repository.Refresh(item);

				if (geometry != null)
				{
					itemsWithExtent.Add(item);

					item.SourceGeometryType = geometry.GeometryType;
					item.SetExtent(geometry.Extent);

					ComputeExtent(geometry.Extent,
					              ref xmin, ref ymin, ref zmin,
					              ref xmax, ref ymax, ref zmax);
				}
			}

			_msg.DebugStopTiming(watch, $"{this} loaded {rowMap.Count} items.");

			Assert.True(xmin > double.MinValue, "Cannot get coordinate");
			Assert.True(ymin > double.MinValue, "Cannot get coordinate");
			Assert.True(xmax < double.MaxValue, "Cannot get coordinate");
			Assert.True(ymax < double.MaxValue, "Cannot get coordinate");

			Repository.Extent = EnvelopeBuilderEx.CreateEnvelope(new Coordinate3D(xmin, ymin, zmin),
			                                                     new Coordinate3D(xmax, ymax, zmax),
			                                                     Repository.SpatialReference);

			// TODO: QueryPoints?
			// TODO: (daro) introduce a loaded flag. Situation: work list is loaded into map. Navigator opened >
			//		 LoadItemsInBackground > close navigator > re-open it, items are already loaded.
			lock (_obj)
			{
				_rowMap = rowMap;
				_items = new List<IWorkItem>(rowMap.Values);
				_searcher = CreateSpatialSearcher(itemsWithExtent);
			}

			OnWorkListChanged();

			LoadItemsCore(filter);
		}

		public IEnumerable<IWorkItem> Search([NotNull] QueryFilter filter)
		{
			// Don't query database here. IWorkItem.OID is the item id. IWorkItem.ObjectID is the
			// ObjectID of its source row. QueryFilter.ObjectIDs are the IWorkItem.OID and not
			// the IWorkItem.ObjectID We'd have to make a lookup: IWorkItem.OID > Table, ObjectID
			// to query database.

			if (filter.ObjectIDs.Count == 0)
			{
				return _items.AsEnumerable();
			}

			List<long> oids = filter.ObjectIDs.OrderBy(oid => oid).ToList();
			return _items.Where(item => oids.BinarySearch(item.OID) >= 0);
		}

		public IEnumerable<IWorkItem> Search([CanBeNull] SpatialQueryFilter filter)
		{
			if (_searcher == null)
			{
				return Enumerable.Empty<IWorkItem>();
			}

			if (filter == null)
			{
				return _searcher;
			}

			WorkItemStatus? currentVisibility = GetStatus(Visibility);

			Predicate<IWorkItem> predicate = null;
			if (currentVisibility.HasValue)
			{
				predicate = item => item.Status == currentVisibility;
			}

			Envelope extent = filter.FilterGeometry.Extent;

			// TODO: (daro) tolerance?
			return _searcher.Search(extent.XMin, extent.YMin,
			                        extent.XMax, extent.YMax,
			                        0.001, predicate);
		}

		protected virtual void LoadItemsCore(QueryFilter filter)
		{
			foreach (IWorkItem item in Search(filter))
			{
				Repository.UpdateState(item);
			}
		}

		private IEnumerable<IWorkItem> GetItems([NotNull] SpatialQueryFilter filter,
		                                        CurrentSearchOption currentSearch,
		                                        VisitedSearchOption visitedSearch)
		{
			IEnumerable<IWorkItem> query = Search(filter);

			if (currentSearch == CurrentSearchOption.ExcludeCurrent)
			{
				query = query.Where(item => ! Equals(item, CurrentItem));
			}

			if (visitedSearch == VisitedSearchOption.ExcludeVisited)
			{
				query = query.Where(item => ! item.Visited);
			}

			return query;
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

		private static SpatialHashSearcher<IWorkItem> CreateSpatialSearcher(List<IWorkItem> items)
		{
			if (items.Count == 0)
			{
				return null;
			}

			return SpatialHashSearcher<IWorkItem>.CreateSpatialSearcher(items, CreateEnvelope);
		}

		[NotNull]
		private static EnvelopeXY CreateEnvelope(IWorkItem item)
		{
			if (item.Extent is null) throw new ArgumentNullException(nameof(item.Extent));

			return new EnvelopeXY(item.Extent.XMin, item.Extent.YMin, item.Extent.XMax,
			                      item.Extent.YMax);
		}

		#endregion

		public long CountLoadedItems(out int todo)
		{
			todo = 0;
			todo += _items.Count(item => item.Status == WorkItemStatus.Todo);

			return _items.Count;
		}

		public void Count()
		{
			var watch = _msg.DebugStartTiming($"{this} start counting items.");

			TotalCount ??= Repository.Count();

			_msg.DebugStopTiming(watch, $"{this} counted {TotalCount} items.");
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
			Assert.False(Equals(first, CurrentItem), "current item and first item are equal");

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

			if (! found && HasCurrentItem() && CurrentItem != null)
			{
				ClearCurrentItem(CurrentItem);
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
			Assert.False(Equals(next, CurrentItem), "current item and next item are equal");

			SetCurrentItemCore(next, CurrentItem);
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
			Assert.False(Equals(previous, CurrentItem), "current item and previous item are equal");

			SetCurrentItemCore(previous, CurrentItem);
		}

		public virtual void GoTo(long oid)
		{
			if (CurrentItem?.OID == oid)
			{
				return;
			}

			var filter = new QueryFilter { ObjectIDs = new[] { oid } };
			IWorkItem target = Search(filter).FirstOrDefault();

			if (target != null)
			{
				SetCurrentItem(target, CurrentItem);
			}
		}

		#endregion

		#region Navigation non-public

		private bool TryGoNearest([NotNull] Polygon[] contextPerimeters,
		                          [NotNull] Geometry reference,
		                          VisitedSearchOption visitedSearchOption)
		{
			IList<IWorkItem> candidates;
			IWorkItem nearest;

			if (TryGetItemsForInnermostContext(contextPerimeters,
			                                   visitedSearchOption,
			                                   out candidates))
			{
				nearest = GetNearest(reference, candidates);
			}
			else
			{
				// nothing found, try Extent
				candidates =
					GetItems(GdbQueryUtils.CreateSpatialFilter(Extent),
					         CurrentSearchOption.ExcludeCurrent, visitedSearchOption).ToList();

				nearest = GetNearest(reference, candidates);
			}

			if (nearest == null)
			{
				return false;
			}

			SetCurrentItem(nearest, CurrentItem);
			return true;
		}

		private bool TryGetItemsForInnermostContext(Polygon[] contextPerimeters,
		                                            VisitedSearchOption visitedSearchOption,
		                                            out IList<IWorkItem> items)
		{
			items = new List<IWorkItem>(0);

			// TODO: (daro) magic number
			int trials = 10;
			int count = 0;

			while (count == 0)
			{
				if (trials == 0)
				{
					_msg.Debug($"Stop searching items after {trials} tries.");
					return false;
				}

				items =
					GetWorkItemsForInnermostContext(contextPerimeters,
					                                visitedSearchOption);
				count = items.Count;
				trials -= 1;

				contextPerimeters =
					contextPerimeters
						.Select(p => PolygonBuilderEx.CreatePolygon(
							        p.Extent.Expand(2, 2, true), p.SpatialReference))
						.ToArray();
			}

			return true;
		}

		// TODO: (daro) rename to GetItemsForInnermostContext
		[NotNull]
		private IList<IWorkItem> GetWorkItemsForInnermostContext([NotNull] Polygon[] perimeters,
		                                                         VisitedSearchOption visitedSearch)
		{
			Assert.ArgumentNotNull(perimeters, nameof(perimeters));

			_msg.VerboseDebug(
				() => $"Getting work items for innermost context ({perimeters.Length} perimeters)");

			// TODO: DARO revise it's always Exclude Current
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
						GetItems(
							GdbQueryUtils.CreateSpatialFilter(
								intersection, SpatialRelationship.Contains), currentSearch,
							visitedSearch).ToList();

					if (workItems.Count == 0)
					{
						_msg.VerboseDebug(
							() =>
								"The intersection contains no items, searching partially contained items");

						workItems =
							GetItems(GdbQueryUtils.CreateSpatialFilter(intersection), currentSearch,
							         visitedSearch).ToList();
					}

					_msg.VerboseDebug(() => $"{workItems.Count} work item(s) found");

					if (workItems.Count > 0)
					{
						return workItems;
					}

					// else: continue with next perimeter
				}
			}

			return new List<IWorkItem>(0);
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
			IWorkItem current = CurrentItem;

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
			IWorkItem currentItem = CurrentItem;

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

		/// <summary>
		/// Invalidates and re-loads all items of the work list. This method is needed when the
		/// invalidation of source objects cannot be specified by OID or geometry but only by
		/// modified table. Typical scenarios are 'discard edits' or 'reconcile'.
		/// </summary>
		public void Invalidate()
		{
			_msg.Debug($"{nameof(Invalidate)}: All items");

			LoadItems();

			if (! HasCurrentItem())
			{
				GoNearest(MapView.Active.Extent);
			}

			OnWorkListChanged();
		}

		/// <summary>
		/// Raises WorkListChanged event: forces
		/// to redraw the map which leads to a GetItems() call.
		/// </summary>
		/// <param name="geometry">The area to invalidate the work list layer.</param>
		public void Invalidate(Envelope geometry)
		{
			_msg.Debug($"{nameof(Invalidate)}: by geometry");

			OnWorkListChanged(geometry.Extent);
		}

		/// <summary>
		/// Triggers WorkItemTable.Search(QueryFilter)
		/// </summary>
		/// <param name="oids">List of work item OIDs (not ObjectID</param>
		public void Invalidate(List<long> oids)
		{
			_msg.Debug($"{nameof(Invalidate)}: {oids.Count} object IDs");

			OnWorkListChanged(null, oids);
		}

		/// <summary>
		/// Invalidates and re-loads all items of the specified source tables. This method can
		/// be employed when the changes cannot be identified on a per-object level but only
		/// per source table (e.g. deletes).
		/// </summary>
		/// <param name="tables"></param>
		public void Invalidate(IEnumerable<Table> tables)
		{
			_msg.Debug($"{nameof(Invalidate)}: Specific source tables");

			// TODO: More fine-granular invalidation, consider separate row cache containing
			// _rowMap, _items.
			Invalidate();
		}

		public bool CanContain(Table table)
		{
			// Is it the work list itself?
			if (string.Equals(table.GetName(), Name, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

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
			_msg.Debug($"ProcessInserts {table.GetName()}.");

			var invalidateOids = new List<long>(oids.Count);

			try
			{
				QueryFilter filter = GdbQueryUtils.CreateFilter(oids);
				Stopwatch watch = Stopwatch.StartNew();

				foreach ((IWorkItem item, Geometry geometry) in Repository.GetItems(
					         table, filter, null))
				{
					Assert.True(TryAddItem(item), $"Could not add {item}");

					// it's a unkown item > refresh it's state (status, visited) either
					// from DB (DbStatusWorkItem) or from definition file (SelectionItem).
					// TODO: (daro) really necessary? It's a virgin new item...
					Repository.Refresh(item);

					if (CacheBufferedItemGeometries)
					{
						UpdateItemDisplayGeometry(item, geometry);
					}

					invalidateOids.Add(item.OID);
				}

				Repository.Extent = CreateExtent(_items, Repository.SpatialReference);

				_msg.DebugStopTiming(watch, $"{invalidateOids.Count} items inserted.");
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return invalidateOids;
		}

		private void ProcessDeletes(Table table, List<long> oids)
		{
			_msg.Debug($"ProcessDeletes {table.GetName()}.");

			try
			{
				GdbTableIdentity tableId = new GdbTableIdentity(table);

				foreach (long oid in oids)
				{
					var rowId = new GdbRowIdentity(oid, tableId);

					if (! TryGetItem(rowId, out IWorkItem cachedItem))
					{
						continue;
					}

					if (CurrentItem != null && CurrentItem.Equals(cachedItem))
					{
						Assert.True(HasCurrentItem(), $"{nameof(HasCurrentItem)} is false");
						ClearCurrentItem(cachedItem);
					}

					// to invalidate item remove it from work list cache
					bool invalidated =
						_rowMap.Remove(cachedItem.GdbRowProxy, out IWorkItem item) &&
						_items.Remove(item);
					Assert.True(invalidated,
					            $"Invalidate work item failed: {rowId} not part of work list");

					Envelope extent = cachedItem.Extent;
					if (extent != null)
					{
						_searcher?.Remove(cachedItem,
						                  extent.XMin, extent.YMin,
						                  extent.XMax, extent.YMax);
					}
				}

				Repository.Extent = CreateExtent(_items, Repository.SpatialReference);
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}
		}

		private List<long> ProcessUpdates(Table table, List<long> oids)
		{
			_msg.Debug($"ProcessUpdate {table.GetName()}.");

			var invalidateOids = new List<long>(oids.Count);

			try
			{
				QueryFilter filter = GdbQueryUtils.CreateFilter(oids);
				Stopwatch watch = Stopwatch.StartNew();

				foreach ((IWorkItem item, Geometry geometry) in Repository.GetItems(
					         table, filter, null))
				{
					Assert.True(TryGetItem(item.GdbRowProxy, out IWorkItem cachedItem),
					            $"Could not get {cachedItem}");

					invalidateOids.Add(cachedItem.OID);

					if (Equals(cachedItem.Status, item.Status))
					{
						// Status hasn't changed but maybe the geometry => update SpatialHashSearcher.
						if (cachedItem.HasExtent)
						{
							Envelope extent = Assert.NotNull(cachedItem.Extent);

							Assert.NotNull(_searcher).Remove(cachedItem,
							                                 extent.XMin, extent.YMin,
							                                 extent.XMax, extent.YMax);

							if (CacheBufferedItemGeometries)
							{
								UpdateItemDisplayGeometry(cachedItem, geometry);
							}

							_searcher.Add(cachedItem, CreateEnvelope(cachedItem));
						}
					}

					// Update cached item's state from database item. IWorkItem.Status
					// also is updated in GdbItemRepository.SetStatusCoreAsync().
					cachedItem.Status = item.Status;
				}

				// TODO: Move to repository!
				Repository.Extent = CreateExtent(_items, Repository.SpatialReference);

				_msg.DebugStopTiming(watch, $"{invalidateOids.Count} items updated.");
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

			return UpdateItemDisplayGeometry(cachedItem, geometry, exclusion);
		}

		private int UpdateItemDisplayGeometry([NotNull] IWorkItem item,
		                                      [CanBeNull] Geometry shapeGeometry,
		                                      Predicate<IWorkItem> exclusion = null)
		{
			if (shapeGeometry == null)
			{
				return 0;
			}

			if (exclusion != null && exclusion(item))
			{
				return 0;
			}

			if (CanUseBufferedGeometryFor(shapeGeometry))
			{
				// TODO: Add units to configuration, convert to data spatial reference if necessary
				//       So far, the buffer distance is assumed to be in the data spatial reference units.
				double bufferDistance = ItemDisplayBufferDistance;

				item.SetBufferedGeometry(GeometryUtils.Buffer(shapeGeometry, bufferDistance));
			}
			else
			{
				item.SetExtent(shapeGeometry.Extent);
			}

			return 1;
		}

		private bool TryAddItem(IWorkItem item)
		{
			Assert.True(item.OID <= 0, "item is already initialized");
			item.OID = Repository.GetNextOid();

			if (! _rowMap.TryAdd(item.GdbRowProxy, item))
			{
				return false;
			}

			_items.Add(item);

			if (item.HasExtent)
			{
				if (_searcher == null)
				{
					_searcher = CreateSpatialSearcher(_items);
				}
				else
				{
					_searcher.Add(item, CreateEnvelope(item));
				}
			}

			return true;
		}

		private bool TryGetItem(GdbRowIdentity rowProxy, out IWorkItem cachedItem)
		{
			bool exists = _rowMap.TryGetValue(rowProxy, out cachedItem);

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

		public virtual string ToString()
		{
			return $"{DisplayName}: {Name}";
		}
	}
}
