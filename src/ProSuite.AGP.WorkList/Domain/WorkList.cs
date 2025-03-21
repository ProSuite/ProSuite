using System;
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
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Domain
{
	/// <summary>
	/// A WorkList is a named list of work items.
	/// It maintains a current item and provides
	/// navigation to change the current item.
	/// </summary>
	// todo: daro separate geometry processing code
	// todo: daro separate QueuedTask code
	// todo: daro avoid ArcGIS.Desktop.Mapping dependency
	public abstract class WorkList : NotifyPropertyChangedBase, IWorkList, IEquatable<WorkList>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// todo daro: revise
		private const int _initialCapacity = 1000;

		private readonly object _syncLock = new object();

		[CanBeNull] private EditEventsRowCacheSynchronizer _rowCacheSynchronizer;

		[NotNull]
		public IWorkItemRepository Repository { get; }

		public event EventHandler<WorkListChangedEventArgs> WorkListChanged;

		[NotNull] private List<IWorkItem> _items = new List<IWorkItem>(_initialCapacity);

		[NotNull]
		protected List<IWorkItem> Items
		{
			get => _items;
			set => _items = value;
		}

		[NotNull] private readonly Dictionary<GdbRowIdentity, IWorkItem> _rowMap =
			new Dictionary<GdbRowIdentity, IWorkItem>(_initialCapacity);

		[NotNull]
		protected Dictionary<GdbRowIdentity, IWorkItem> RowMap => _rowMap;

		private WorkItemVisibility _visibility;
		private string _displayName;

		protected WorkList([NotNull] IWorkItemRepository repository,
		                   [NotNull] string name,
		                   [CanBeNull] Geometry areaOfInterest = null,
		                   [CanBeNull] string displayName = null)
		{
			_displayName = displayName;
			Name = name;

			Repository = repository;

			Visibility = WorkItemVisibility.Todo;
			AreaOfInterest = areaOfInterest;
			CurrentIndex = repository.GetCurrentIndex();

			RefreshItems();
		}
		
		public string Name { get; set; }

		public string DisplayName
		{
			get
			{
				if (string.IsNullOrEmpty(_displayName))
				{
					return GetDisplayNameCore();
				}

				return _displayName;
			}
			private set => SetProperty(ref _displayName, value);
		}

		public void Rename(string name)
		{
			DisplayName = name;
			Repository.WorkItemStateRepository.Rename(name);
		}

		protected abstract string GetDisplayNameCore();

		// NOTE: An empty work list should return null and not an empty envelope.
		//		 Pluggable Datasource cannot handle an empty envelope.
		public Envelope Extent { get; protected set; }

		public IWorkItem Current => GetItem(CurrentIndex);

		public int CurrentIndex { get; set; }

		public WorkItemVisibility Visibility
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

		public Geometry AreaOfInterest { get; set; }

		public virtual bool QueryLanguageSupported { get; } = false;

		public bool CanSetStatus()
		{
			return HasCurrentItem && CanSetStatusCore();
		}

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

		public async Task SetStatusAsync(IWorkItem item, WorkItemStatus status)
		{
			await Repository.SetStatusAsync(item, status);

			// If an item visibility changes to 'Done' the item is not part
			// of the work list anymore, respectively GetItems(QuerFilter, bool, int)
			// does not return the Done-item anymore. Therefor use the item's Extent
			// to invalidate the work list layer.
			OnWorkListChanged();
		}

		private bool _itemsGeometryDraftMode = true;

		public void SetItemsGeometryDraftMode(bool enable)
		{
			_itemsGeometryDraftMode = enable;

			// invalidate map
			OnWorkListChanged(MapView.Active.Extent);
		}

		public Geometry GetItemGeometry(IWorkItem item)
		{
			try
			{
				if (item?.Extent == null)
				{
					return null;
				}

				if (! UseItemGeometry(item))
				{
					item.QueryPoints(out double xmin, out double ymin,
					                 out double xmax, out double ymax,
					                 out double zmax);

					return PolygonBuilderEx.CreatePolygon(EnvelopeBuilderEx.CreateEnvelope(
						                                      new Coordinate3D(xmin, ymin, zmax),
						                                      new Coordinate3D(xmax, ymax, zmax),
						                                      item.Extent.SpatialReference));
				}

				if (item.HasFeatureGeometry && ! _itemsGeometryDraftMode)
				{
					return (Polygon) item.Geometry;
				}

				return PolygonBuilderEx.CreatePolygon(item.Extent, item.Extent.SpatialReference);
			}
			catch (Exception ex)
			{
				Gateway.LogError(ex, _msg);
			}

			return null;
		}

		private static bool UseItemGeometry([NotNull] IWorkItem item)
		{
			switch (item.GeometryType)
			{
				case GeometryType.Polyline:
				case GeometryType.Polygon:
					return true;

				default:
					return false;
			}
		}

		public virtual void RefreshItems()
		{
			List<IWorkItem> newItems = new List<IWorkItem>(_items.Count);

			foreach (IWorkItem item in Repository.GetItems(AreaOfInterest, WorkItemStatus.Todo))
			{
				newItems.Add(item);

				if (!_rowMap.ContainsKey(item.GdbRowProxy))
				{
					_rowMap.Add(item.GdbRowProxy, item);
				}
				else
				{
					// todo daro: warn
				}
			}

			_msg.DebugFormat("Added {0} items to work list", newItems.Count);

			// initializes the state repository if no states for
			// the work items are read yet
			Repository.UpdateVolatileState(newItems);

			_msg.DebugFormat("Getting extents for {0} items...", newItems.Count);
			// todo daro: EnvelopeBuilder as parameter > do not iterate again over items
			//			  look old work item implementation
			Extent = GetExtentFromItems(newItems);

			_items = newItems;
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

		/// <summary>
		/// Set work items visibility and invokes WorkListChanged event.
		/// </summary>
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

		public virtual IEnumerable<IWorkItem> GetItems(QueryFilter filter = null,
		                                               bool ignoreListSettings = false,
		                                               int startIndex = -1)
		{
			// Subclass should provide more efficient implementation (e.g. pass filter on to database)

			var query = (IEnumerable<IWorkItem>) _items;

			if (! ignoreListSettings && Visibility != WorkItemVisibility.None)
			{
				query = query.Where(item => IsVisible(item, Visibility));
			}

			if (filter?.ObjectIDs != null && filter.ObjectIDs.Count > 0)
			{
				List<long> oids = filter.ObjectIDs.OrderBy(oid => oid).ToList();
				query = query.Where(item => oids.BinarySearch(item.OID) >= 0);
			}

			// filter should never have a WhereClause since we say QueryLanguageSupported = false

			if (filter is SpatialQueryFilter sf && sf.FilterGeometry != null)
			{
				// TODO: Use spatial index! This is slow for a large item count.
				// todo daro: do not use method to build Extent every time
				query = query.Where(
					item => Relates(sf.FilterGeometry, sf.SpatialRelationship, item.Extent));
			}

			if (! ignoreListSettings && AreaOfInterest != null)
			{
				query = query.Where(item => WithinAreaOfInterest(item.Extent, AreaOfInterest));
			}

			// TODO: (daro) drop!
			if (startIndex > -1 && startIndex < _items.Count)
			{
				// This can be ultra-slow for a large item count! Consider looping over all items exactly once!
				query = query.Where(item => _items.IndexOf(item, startIndex) > -1);
			}

			return query;
		}

		//public IEnumerable<IWorkItem> GetItems(WorkItemStatus? filterByStatus = null,
		//                                       int startIndex = 0)
		//{
		//	var query = (IEnumerable<IWorkItem>)_items;

		//	if (!ignoreListSettings && Visibility != WorkItemVisibility.None)
		//	{
		//		query = query.Where(item => IsVisible(item, Visibility));
		//	}

		//	if (filter?.ObjectIDs != null && filter.ObjectIDs.Count > 0)
		//	{
		//		List<long> oids = filter.ObjectIDs.OrderBy(oid => oid).ToList();
		//		query = query.Where(item => oids.BinarySearch(item.OID) >= 0);
		//	}

		//	// filter should never have a WhereClause since we say QueryLanguageSupported = false

		//	if (filter is SpatialQueryFilter sf && sf.FilterGeometry != null)
		//	{
		//		// todo daro: do not use method to build Extent every time
		//		query = query.Where(
		//			item => Relates(sf.FilterGeometry, sf.SpatialRelationship, item.Extent));
		//	}

		//	if (!ignoreListSettings && AreaOfInterest != null)
		//	{
		//		query = query.Where(item => WithinAreaOfInterest(item.Extent, AreaOfInterest));
		//	}

		//	if (startIndex > -1 && startIndex < _items.Count)
		//	{
		//		query = query.Where(item => _items.IndexOf(item, startIndex) > -1);
		//	}

		//	return query;
		//}


		// TODO: daro drop?
		public virtual int Count(QueryFilter filter = null, bool ignoreListSettings = false)
		{
			lock (_syncLock)
			{
				return GetItems(filter, ignoreListSettings).Count();
			}
		}

		// TODO: daro move to base?
		public int Count()
		{
			lock (_syncLock)
			{
				return _items.Count;
			}
		}

		protected virtual bool CanSetStatusCore()
		{
			return true;
		}

		#region Navigation public

		public virtual bool CanGoFirst()
		{
			// TODO: Use Prosuite RelayCommand to prevent crash!
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

			// todo daro to ?? statement
			IWorkItem first = GetFirstVisibleVisitedItemBeforeCurrent();

			// todo daro: remove assertion when sure algorithm works
			//			  CanGoFirst should prevent the assertion
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

			// start after the current item
			int startIndex = CurrentIndex + 1;

			// first, try to go to an unvisited item
			bool found = TryGoNearest(contextPerimeters, reference,
			                          VisitedSearchOption.ExcludeVisited,
			                          startIndex);

			if (! found)
			{
				// if none found, search also the visited ones, but
				// only those *after* the current item
				found = TryGoNearest(contextPerimeters, reference,
				                     VisitedSearchOption.IncludeVisited,
				                     startIndex);
			}

			if (! found && HasCurrentItem && Current != null)
			{
				ClearCurrentItem(Current);
			}

			_msg.DebugStopTiming(watch, nameof(GetNearest));
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

			// todo daro: remove assertion when sure algorithm works
			//			  CanGoNext should prevent the assertion
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

			// todo daro: remove assertion when sure algorithm works
			//			  CanGoPrevious should prevent the assertion
			Assert.NotNull(previous);
			Assert.False(Equals(previous, Current), "current item and previous item are equal");

			SetCurrentItemCore(previous, Current);
		}

		#endregion

		public virtual void GoTo(long oid)
		{
			if (Current?.OID == oid)
			{
				return;
			}

			var filter = new QueryFilter { ObjectIDs = new[] { oid } };
			IWorkItem target = GetItems(filter, false).FirstOrDefault();

			if (target != null)
			{
				SetCurrentItem(target, Current);
			}
		}

		#region Navigation non-public

		private bool TryGoNearest([NotNull] Polygon[] contextPerimeters,
		                          [NotNull] Geometry reference,
		                          VisitedSearchOption visitedSearchOption,
		                          int startIndex)
		{
			IList<IWorkItem> candidates =
				GetWorkItemsForInnermostContext(contextPerimeters,
				                                visitedSearchOption, startIndex);
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
		                                                         VisitedSearchOption visitedSearch,
		                                                         int startIndex)
		{
			Assert.ArgumentNotNull(perimeters, nameof(perimeters));

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Getting work items for innermost context ({0} perimeters)",
				                 perimeters.Length);
			}

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

					// todo daro: old implementation
					// search the items fully within the search extent
					//IList<WorkItem> workItems = GetItems(statusSearch, currentSearch,
					//                                     visitedSearch, startIndex,
					//                                     intersection,
					//                                     SpatialSearchOption.Within,
					//                                     match);

					SpatialQueryFilter filter =
						GdbQueryUtils.CreateSpatialFilter(intersection, SpatialRelationship.Within);

					List<IWorkItem> workItems =
						GetItems(filter, startIndex, currentSearch, visitedSearch).ToList();

					if (workItems.Count == 0)
					{
						if (_msg.IsVerboseDebugEnabled)
						{
							_msg.Debug(
								"No work items fully within the intersection, searching partially contained items");
						}

						// todo daro: old implementation
						// search also intersecting items
						//workItems = GetItems(statusSearch, currentSearch,
						//					 visitedSearch, startIndex,
						//					 intersection, SpatialSearchOption.Intersect,
						//					 match);

						filter = GdbQueryUtils.CreateSpatialFilter(intersection);

						workItems = GetItems(filter, startIndex, currentSearch, visitedSearch)
							.ToList();
					}

					if (_msg.IsVerboseDebugEnabled)
					{
						_msg.DebugFormat("{0} work item(s) found", workItems.Count);
					}

					if (workItems.Count > 0)
					{
						return workItems;
					}

					// else: continue with next perimeter
				}
			}

			// nothing found so far. Search entire work list
			// todo daro: old implementation
			//return GetItems(statusSearch, currentSearch, visitedSearch, startIndex, match);
			return GetItems(null, startIndex, currentSearch, visitedSearch).ToList();
		}

		private IEnumerable<IWorkItem> GetItems(
			QueryFilter filter = null, int startIndex = -1,
			CurrentSearchOption currentSearch = CurrentSearchOption.ExcludeCurrent,
			VisitedSearchOption visitedSearch = VisitedSearchOption.ExcludeVisited)
		{
			IEnumerable<IWorkItem> query = GetItems(filter, false, startIndex);

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

		[NotNull]
		private Polygon GetIntersection([NotNull] Polygon[] perimeters, int index)
		{
			Assert.ArgumentNotNull(perimeters, nameof(perimeters));

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Intersecting perimeters {0} to {1}",
				                 index, perimeters.Length - 1);
			}

			Polygon intersection = GeometryFactory.Clone(perimeters[index]);

			// todo daro old implementation
			//GeometryUtils.EnsureSpatialReference(intersection, SpatialReference, true);

			// intersect with all following perimeters, if any
			int nextIndex = index + 1;

			if (nextIndex < perimeters.Length)
			{
				for (int combineIndex = nextIndex;
				     combineIndex < perimeters.Length;
				     combineIndex++)
				{
					//Polygon projectedPerimeter =
					//	GetInWorkListSpatialReference(perimeters[combineIndex]);

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
						// todo daro: old implementation
						//var topoOp = (ITopologicalOperator)intersection;

						//intersection =
						//	(IPolygon)topoOp.Intersect(
						//		projectedPerimeter,
						//		esriGeometryDimension.esriGeometry2Dimension);

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
							// todo daro: old implementation
							//_msg.Debug(GeometryUtils.ToString(projectedPerimeter));

							_msg.DebugFormat(
								"Input intersection at nextIndex={0}:", nextIndex);
							// todo daro: old implementation
							//_msg.Debug(GeometryUtils.ToString(intersection));

							// leave intersection as is, and continue
						}
						catch (Exception e1)
						{
							_msg.Warn("Error writing details to log", e1);
						}
					}

					// todo daro: old implementation
					//if (_msg.IsVerboseDebugEnabled)
					//{
					//	_msg.DebugFormat("Intersection {0}: {1}",
					//					 combineIndex,
					//					 IntersectionToString(intersection));
					//}
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

			// todo daro: old implementation
			// acceleration?
			//GeometryUtils.AllowIndexing(searchReference);

			//IProximityOperator referenceProximity;
			GeometryType referenceGeometryType = searchReference.GeometryType;

			Geometry referenceGeometry;

			if (referenceGeometryType == GeometryType.Polygon ||
			    referenceGeometryType == GeometryType.Envelope)
			{
				// for polygons and envelopes it does not make much sense to search from the 
				// boundary; search from centroid instead. This also prevents
				// the extreme response times (minutes) of ReturnDistance() from
				// very large polygons
				// todo daro: old implementation
				//referenceProximity = (IProximityOperator)((IArea)searchReference).Centroid;

				referenceGeometry = GeometryEngine.Instance.Centroid(searchReference);
			}
			else
			{
				// todo daro: old implementation
				//referenceProximity = (IProximityOperator)searchReference;
				referenceGeometry = searchReference;
			}

			// todo daro: old implementation
			//var referenceRelation = (IRelationalOperator)searchReference;

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
					if (item.HasGeometry)
					{
						// todo daro: old implementation
						//workItem.QueryExtent(otherExtent);
						Envelope otherExtent = item.Extent;

						// IWorkItem.Extent from SDE (and reported from ALGR from occasionally from issues.gdb) seems to
						// to have an unequal SR compared to the referenceGeometry which is the MapView.Current.Extent
						// when the work list ist opened for the first time.
						// EMA: while editing the SR resolution might be set to a very small value. This probably does
						// not happen from FGDB data.
						// todo daro: find a better solution than reprojecting in foreach loop
						Geometry projected =
							GeometryUtils.EnsureSpatialReference(
								referenceGeometry, otherExtent.SpatialReference);

						double distance;
						try
						{
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

							// todo daro: old implementation
							//if (referenceRelation.Disjoint(otherExtent))
							//{
							//	distance = referenceProximity.ReturnDistance(otherExtent);
							//}
							//else
							//{
							//	((IArea)otherExtent).QueryCentroid(otherCentroid);

							//	distance = referenceProximity.ReturnDistance(
							//		otherCentroid);
							//}
						}
						catch (Exception)
						{
							// todo daro: old implementation
							//_msg.DebugFormat("search reference: {0}", GeometryUtils.ToString(searchReference));
							//_msg.DebugFormat("otherExtent: {0}", GeometryUtils.ToString(otherExtent));

							throw;
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
						if (firstWithoutGeometry == null)
						{
							firstWithoutGeometry = item;
						}
					}
				}
			}

			return nearest ?? firstWithoutGeometry;
		}

		[CanBeNull]
		private Geometry GetNearestSearchReference([NotNull] Geometry reference)
		{
			const int maxPointCount = 10000;

			// todo daro: old implementation
			//bool useCentroid = !GeometryUtils.IsGeometryValid(reference);

			bool useCentroid = true;

			if (! useCentroid && GeometryUtils.GetPointCount(reference) > maxPointCount)
			{
				_msg.Debug(
					"Too many points on reference geometry for searching, using center of envelope");
				useCentroid = true;
			}

			if (useCentroid)
			{
				MapPoint centroid = null;

				try
				{
					// todo daro: old implementation
					//var area = reference.Extent as IArea;
					//if (area != null && ! area.Centroid.IsEmpty)
					//{
					//	centroid = area.Centroid;
					//}

					if (! reference.IsEmpty)
					{
						centroid = GeometryEngine.Instance.Centroid(reference);
					}
				}
				catch (Exception e)
				{
					_msg.Debug("Error trying to get centroid of geometry", e);
					// todo daro: old implementation
					//_msg.Debug(GeometryUtils.ToString(reference));
				}

				if (centroid == null)
				{
					return null;
				}

				reference = centroid;
			}
			// todo daro: old implementation
			//else
			//{
			//	if (reference is IMultiPatch)
			//	{
			//		reference = ((IMultiPatch)reference).XYFootprint;
			//	}
			//}

			// todo daro: old implementation
			//reference = GetInWorkListSpatialReference(reference);

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
			Repository.SetVisited(nextItem);

			var oids = currentItem != null
				           ? new List<long> { nextItem.OID, currentItem.OID }
				           : new List<long> { nextItem.OID };

			OnWorkListChanged(null, oids);
		}

		private void ReorderCurrentItem([NotNull] IWorkItem nextItem)
		{
			// move new item to just after previous current
			int insertIndex = GetReorderInsertIndex(nextItem);

			// todo daro drop
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

		// todo daro: to Utils? Compare with EnvelopeBuilderEx
		// todo: daro drop or refactor
		[CanBeNull]
		public static Envelope GetExtentFromItems([CanBeNull] IEnumerable<IWorkItem> items)
		{
			double xmin = double.MaxValue, ymin = double.MaxValue, zmin = double.MaxValue;
			double xmax = double.MinValue, ymax = double.MinValue, zmax = double.MinValue;
			SpatialReference sref = null;
			long count = 0;

			if (items != null)
			{
				foreach (var item in items)
				{
					if (item == null) continue;
					var extent = item.Extent;
					if (extent == null) continue;
					if (extent.IsEmpty) continue;

					if (extent.XMin < xmin) xmin = extent.XMin;
					if (extent.YMin < ymin) ymin = extent.YMin;
					if (extent.ZMin < zmin) zmin = extent.ZMin;

					if (extent.XMax > xmax) xmax = extent.XMax;
					if (extent.YMax > ymax) ymax = extent.YMax;
					if (extent.ZMax > zmax) zmax = extent.ZMax;

					sref = extent.SpatialReference;

					count += 1;
				}
			}

			// Should return null and not an empty envelope. Pluggable Datasource cannot handle
			// an empty envelope.
			return count > 0
				       ? EnvelopeBuilderEx.CreateEnvelope(new Coordinate3D(xmin, ymin, zmin),
				                                          new Coordinate3D(xmax, ymax, zmax), sref)
				       : null;
		}

		private static bool Relates(Geometry a, SpatialRelationship rel, Geometry b)
		{
			if (a == null || b == null) return false;

			switch (rel)
			{
				case SpatialRelationship.EnvelopeIntersects:
				case SpatialRelationship.IndexIntersects:
				case SpatialRelationship.Intersects:
					return GeometryEngine.Instance.Intersects(a, b);
				case SpatialRelationship.Touches:
					return GeometryEngine.Instance.Touches(a, b);
				case SpatialRelationship.Overlaps:
					return GeometryEngine.Instance.Overlaps(a, b);
				case SpatialRelationship.Crosses:
					return GeometryEngine.Instance.Crosses(a, b);
				case SpatialRelationship.Within:
					return GeometryEngine.Instance.Within(a, b);
				case SpatialRelationship.Contains:
					return GeometryEngine.Instance.Contains(a, b);
			}

			return false;
		}

		private bool IsVisible([NotNull] IWorkItem item)
		{
			return IsVisible(item, Visibility);
		}

		private bool IsVisible([NotNull] IWorkItem item, WorkItemVisibility visibility)
		{
			WorkItemStatus status = item.Status;

			switch (visibility)
			{
				case WorkItemVisibility.None:
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

		private static bool WithinAreaOfInterest(Envelope extent, Geometry areaOfInterest)
		{
			if (extent == null) return false;
			if (areaOfInterest == null) return true;
			return GeometryEngine.Instance.Intersects(extent, areaOfInterest);
		}

		#endregion

		// https://blog.stephencleary.com/2012/02/async-and-await.html
		// The primary use case for async void methods is event handlers.
		private void OnWorkListChanged([CanBeNull] Envelope extent = null,
		                               [CanBeNull] List<long> oids = null)
		{
			WorkListChanged?.Invoke(this, new WorkListChangedEventArgs(extent, oids));
		}

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

		public void Invalidate()
		{
			_msg.Debug("Invalidate");

			RefreshItems();

			if (! HasCurrentItem)
			{
				GoNearest(MapView.Active.Extent);
			}

			OnWorkListChanged();
		}

		public void Invalidate(IEnumerable<Table> tables)
		{
			// TODO: More fine-granular invalidation, consider separate row cache containing
			// _rowMap, _items.
			Invalidate();
		}

		public void ProcessChanges(Dictionary<Table, List<long>> inserts,
		                           Dictionary<Table, List<long>> deletes,
		                           Dictionary<Table, List<long>> updates)
		{
			_msg.Debug(
				$"{nameof(ProcessChanges)} - inserts: {inserts.Count} deletes: {deletes.Count} updates: {updates.Count}");

			foreach (var insert in inserts)
			{
				ProcessInserts(insert.Key, insert.Value);
			}

			foreach (var delete in deletes)
			{
				ProcessDeletes(delete.Key, delete.Value);
			}

			foreach (var update in updates)
			{
				ProcessUpdates(update.Key, update.Value);
			}

			if (! HasCurrentItem)
			{
				GoNearest(MapView.Active.Extent);
			}

			// If a item visibility changes to Done the item is not part
			// of the work list anymore, respectively GetItems(QuerFilter, bool, int)
			// does not return the Done-item anymore. Therefor use the item's Extent
			// to invalidate the work list layer.
			OnWorkListChanged();
		}

		public bool CanContain(Table table)
		{
			return Repository.SourceClasses.Any(s => s.TableIdentity.ReferencesTable(table));
		}

		private void ProcessInserts(Table table, IReadOnlyList<long> oids)
		{
			_msg.Debug($"{nameof(ProcessInserts)}");

			foreach (long oid in oids)
			{
				var rowId = new GdbRowIdentity(oid, new GdbTableIdentity(table));

				if (_rowMap.TryGetValue(rowId, out IWorkItem item))
				{
					Refresh(item);

					_items.Add(item);

					if (! HasCurrentItem)
					{
						SetCurrentItem(item);
					}
				}
			}
		}

		private void ProcessDeletes(Table table, IEnumerable<long> oids)
		{
			_msg.Debug($"{nameof(ProcessDeletes)}");

			foreach (long oid in oids)
			{
				var rowId = new GdbRowIdentity(oid, new GdbTableIdentity(table));

				if (Current != null && Current.GdbRowProxy.Equals(rowId))
				{
					Assert.True(HasCurrentItem, $"{nameof(HasCurrentItem)} is false");

					ClearCurrentItem(Current);
				}

				if (_rowMap.TryGetValue(rowId, out IWorkItem item))
				{
					_items.Remove(item);
				}
			}

			Extent = GetExtentFromItems(_items);
		}

		private void ProcessUpdates(Table table, IEnumerable<long> oids)
		{
			_msg.Debug($"{nameof(ProcessUpdates)}");

			foreach (long oid in oids)
			{
				var rowId = new GdbRowIdentity(oid, new GdbTableIdentity(table));

				if (_rowMap.TryGetValue(rowId, out IWorkItem item))
				{
					Refresh(item);
				}
			}
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

		private void UpdateExtent(Envelope itemExtent)
		{
			Extent = Extent?.Union(itemExtent);
		}

		// todo daro: refresh or update?
		private void Refresh(IWorkItem item)
		{
			Repository.Refresh(item);

			UpdateExtent(item.Extent);
		}

		private bool HasCurrentItem => CurrentIndex >= 0 &&
		                               CurrentIndex < _items.Count;

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
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
		}

		#endregion
	}
}
