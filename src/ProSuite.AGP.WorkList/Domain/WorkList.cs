using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	/// <summary>
	/// A WorkList is a named list of work items.
	/// It maintains a current item and provides
	/// navigation to change the current item.
	/// </summary>
	// todo daro: separate geometry processing code
	// todo daro: separate QueuedTask code
	public abstract class WorkList : IWorkList, IRowCache
	{
		private const int _initialCapacity = 1000;

		private readonly object _syncLock = new object();

		protected IWorkItemRepository Repository { get; }

		private readonly List<IWorkItem> _items = new List<IWorkItem>(_initialCapacity);

		[NotNull]
		private Dictionary<GdbRowIdentity, IWorkItem> _rowMap =
			new Dictionary<GdbRowIdentity, IWorkItem>(_initialCapacity);

		private EventHandler<WorkListChangedEventArgs> _workListChanged;

		protected WorkList(IWorkItemRepository repository, string name)
		{
			Repository = repository;

			Name = name ?? string.Empty;

			Visibility = WorkItemVisibility.Todo;
			AreaOfInterest = null;
			CurrentIndex = -1;

			foreach (IWorkItem item in Repository.GetItems())
			{
				_items.Add(item);

				if (! _rowMap.ContainsKey(item.Proxy))
				{
					_rowMap.Add(item.Proxy, item);
				}
				else
				{
					// todo daro: warn
				}
			}


			// todo daro: EnvelopeBuilder as parameter > do not iterate again over items
			//			  look old work item implementation
			Extent = GetExtentFromItems(_items);
		}
		
		public event EventHandler<WorkListChangedEventArgs> WorkListChanged
		{
			add { _workListChanged += value; }
			remove { _workListChanged -= value; }
		}

		public string Name { get; }

		public Envelope Extent { get; protected set; }

		public WorkItemVisibility Visibility { get; set; }

		public Polygon AreaOfInterest { get; set; }

		public virtual bool QueryLanguageSupported { get; } = false;

		public virtual IEnumerable<IWorkItem> GetItems(QueryFilter filter = null,
		                                               bool ignoreListSettings = false)
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
				// todo daro: do not use method to build Extent every time
				query = query.Where(item => Relates(sf.FilterGeometry, sf.SpatialRelationship, item.Extent));
			}

			if (! ignoreListSettings && AreaOfInterest != null)
			{
				query = query.Where(item => WithinAreaOfInterest(item.Extent, AreaOfInterest));
			}

			return query;
		}

		public virtual int Count(QueryFilter filter = null, bool ignoreListSettings = false)
		{
			lock (_syncLock)
			{
				return GetItems(filter, ignoreListSettings).Count();
			}
		}

		/* Navigation */

		public virtual IWorkItem Current => GetItem(CurrentIndex);

		// note daro: only change the item index for navigation! the index is the only valid truth!
		protected int CurrentIndex { get; set; }

		public int DisplayIndex
		{
			get
			{
				return CurrentIndex;
			}
		}

		/* This base class provides an overly simplistic implementation */
		/* TODO should honour Status, Visibility, and AreaOfInterest */

		public virtual bool CanGoFirst()
		{
			return _items.Count > 0;
		}

		public virtual void GoFirst()
		{
			CurrentIndex = 0;

			IWorkItem nextItem = GetNextVisibleItem();
			//TODO should also set current item visited=true

			if (nextItem != null)
			{
				Assert.False(Equals(nextItem, Current), "current item and next item are equal");

				SetCurrentItem(nextItem, Current);
			}
		}

		public virtual bool CanGoNearest()
		{
			return false;
		}

		public virtual void GoNearest()
		{
			throw new NotImplementedException();
		}

		public virtual bool CanGoNext()
		{
			return _items.Count > 0 && CurrentIndex < _items.Count - 1;
		}

		public virtual void GoNext()
		{
			IWorkItem nextItem = GetNextVisibleItem();
			//TODO should also set current item visited=true

			if (nextItem != null)
			{
				Assert.False(Equals(nextItem, Current), "current item and next item are equal");

				SetCurrentItem(nextItem, Current);
			}
		}

		public virtual bool CanGoPrevious()
		{
			return _items.Count > 0 && CurrentIndex > 0;
		}

		public virtual void GoPrevious()
		{
			if (CurrentIndex > 0)
			{
				CurrentIndex -= 1;
				//TODO should also set current item Visited=true
			}
		}

		public abstract void Dispose();

		protected void SetItems(IEnumerable<IWorkItem> items)
		{
			lock (_syncLock)
			{
				_items.Clear();
				_items.AddRange(items.Where(item => item != null));

				CurrentIndex = -1;
			}
		}

		#region Work list navigation

		/// <summary>
		///     Sets given work item as the current one. Updates the current item
		///     index and sets the work item as visited.
		/// </summary>
		/// <param name="nextItem"></param>
		/// <param name="currentItem">The work item.</param>
		private void SetCurrentItem([NotNull] IWorkItem nextItem, [CanBeNull] IWorkItem currentItem)
		{
			nextItem.Visited = true;
			CurrentIndex = _items.IndexOf(nextItem);

			var oids = currentItem != null
				           ? new List<long> {nextItem.OID, currentItem.OID}
				           : new List<long> {nextItem.OID};

			OnWorkListChanged(null, oids);
		}

		[CanBeNull]
		private IWorkItem GetNextVisibleItem()
		{
			if (CurrentIndex >= _items.Count - 1)
			{
				// last item reached
				return null;
			}

			IWorkItem item = _items[CurrentIndex + 1];

			return IsVisible(item) ? item : null;
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

		protected static Envelope GetExtentFromItems(IEnumerable<IWorkItem> items)
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

			return count > 0
				       ? EnvelopeBuilder.CreateEnvelope(new Coordinate3D(xmin, ymin, zmin),
				                                        new Coordinate3D(xmax, ymax, zmax), sref)
				       : EnvelopeBuilder.CreateEnvelope(sref);
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

		private static bool WithinAreaOfInterest(Envelope extent, Polygon areaOfInterest)
		{
			if (extent == null) return false;
			if (areaOfInterest == null) return true;
			return GeometryEngine.Instance.Intersects(extent, areaOfInterest);
		}

		#endregion

		private void OnWorkListChanged([CanBeNull] Envelope extent = null, [CanBeNull] List<long> oids = null)
		{
			_workListChanged?.Invoke(this, new WorkListChangedEventArgs(extent, oids));
		}

		public void Invalidate()
		{
			throw new NotImplementedException();
		}

		// todo daro: Is SDK type Table the right type?
		public void ProcessChanges(Dictionary<Table, List<long>> inserts,
		                           Dictionary<Table, List<long>> deletes,
		                           Dictionary<Table, List<long>> updates)
		{
			foreach (var insert in inserts)
			{
				var tableId = new GdbTableIdentity(insert.Key);
				List<long> oids = insert.Value;

				ProcessInserts(tableId, oids);
			}

			foreach (var delete in deletes)
			{
				var tableId = new GdbTableIdentity(delete.Key);
				List<long> oids = delete.Value;

				ProcessDeletes(tableId, oids);
			}

			foreach (var update in updates)
			{
				var tableId = new GdbTableIdentity(update.Key);
				List<long> oids = update.Value;

				ProcessUpdates(tableId, oids);

				// does not work because ObjectIDs = (IReadOnlyList<long>) modify.Value (oids) are the
				// ObjectIds of source feature not the work item OIDs.
				//IEnumerable<IWorkItem> workItems = GetItems(filter);
			}
		}

		private void ProcessDeletes(GdbTableIdentity tableId, List<long> oids)
		{
			foreach (long oid in oids)
			{
				// todo daro: refactor, simplify
				var rowId = new GdbRowIdentity(oid, tableId);

				if (HasCurrentItem && Current != null && Current.Proxy.Equals(rowId))
				{
					ClearCurrentItem(Current);
				}

				if (_rowMap.TryGetValue(rowId, out IWorkItem item))
				{
					RemoveWorkItem(item);
					// todo daro: WorkListChanged > invalidate map
				}
			}

			// todo daro: update work list extent?
			Extent = GetExtentFromItems(_items);
		}

		private void RemoveWorkItem(IWorkItem item)
		{
			_items.Remove(item);
			_rowMap.Remove(item.Proxy);
		}

		private void ClearCurrentItem([NotNull] IWorkItem current)
		{
			Assert.ArgumentNotNull(current, nameof(current));

			if (CurrentIndex < 0)
			{
				return;
			}

			CurrentIndex = -1;

			OnWorkListChanged(null, new List<long> {current.OID});
		}

		private void ProcessInserts(GdbTableIdentity tableId, List<long> oids)
		{
			var filter = new QueryFilter {ObjectIDs = oids};

			foreach (IWorkItem item in Repository.GetItems(tableId, filter).ToList())
			{
				if (_rowMap.ContainsKey(item.Proxy))
				{
					// todo daro: warn
				}

				_items.Add(item);
				_rowMap.Add(item.Proxy, item);

				if (! HasCurrentItem)
				{
					SetCurrentItem(item, null);
					// todo daro: WorkListChanged > invalidate map
				}

				UpdateExtent(item.Extent);
			}
		}

		private void UpdateExtent(Envelope itemExtent)
		{
			Extent = Extent.Union(itemExtent);
		}

		private void ProcessUpdates(GdbTableIdentity tableId, IEnumerable<long> oids)
		{
			foreach (long oid in oids)
			{
				if (_rowMap.TryGetValue(new GdbRowIdentity(oid, tableId), out IWorkItem item))
				{
					Repository.UpdateItem(item);

					UpdateExtent(item.Extent);
				}
			}
		}

		public bool HasCurrentItem => CurrentIndex >= 0 &&
		                              _items != null &&
		                              CurrentIndex < _items.Count;
	}
}
