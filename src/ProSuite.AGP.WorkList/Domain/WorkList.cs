using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.AGP.WorkList.Domain
{
	/// <summary>
	/// A WorkList is a named list of work items.
	/// It maintains a current item and provides
	/// navigation to change the current item.
	/// </summary>
	public abstract class WorkList : IWorkList
	{
		private const int _initialCapacity = 1000;

		private readonly object _syncLock = new object();

		private readonly List<IWorkItem> _items = new List<IWorkItem>(_initialCapacity);

		private readonly Dictionary<GdbRowReference, IWorkItem> _itemMap =
			new Dictionary<GdbRowReference, IWorkItem>(_initialCapacity);

		private readonly IWorkItemRepository _repository;

		protected WorkList(IWorkItemRepository repository, string name)
		{
			_repository = repository;

			Name = name ?? string.Empty;

			Visibility = WorkItemVisibility.Todo;
			AreaOfInterest = null;
			CurrentIndex = -1;

			foreach (IWorkItem item in _repository.GetAll())
			{
				_itemMap.Add(item.Proxy, item);

				_items.Add(item);
			}
		}

		public string Name { get; }

		public virtual Envelope Extent { get; protected set; }

		public WorkItemVisibility Visibility { get; set; }

		public Polygon AreaOfInterest { get; set; }

		public virtual bool QueryLanguageSupported { get; } = false;

		public virtual IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool ignoreListSettings = false)
		{
			// Subclass should provide more efficient implementation (e.g. pass filter on to database)

			var query = (IEnumerable<IWorkItem>) _items;

			if (!ignoreListSettings && Visibility != WorkItemVisibility.None)
			{
				query = query.Where(item => StatusVisible(item.Status, Visibility));
			}

			if (filter?.ObjectIDs != null && filter.ObjectIDs.Count > 0)
			{
				var oids = filter.ObjectIDs.OrderBy(oid => oid).ToList();
				query = query.Where(item => oids.BinarySearch(item.OID) >= 0);
			}

			// filter should never have a WhereClause since we say QueryLanguageSupported = false

			if (filter is SpatialQueryFilter sf && sf.FilterGeometry != null)
			{
				// todo daro: do not use method to build Extent every time
				query = query.Where(
					item => Relates(sf.FilterGeometry, sf.SpatialRelationship, item.Extent));
			}

			if (!ignoreListSettings && AreaOfInterest != null)
			{
				query = query.Where(item => WithinAreaOfInterest(item.Extent, AreaOfInterest));
			}

			return query;
		}

		public virtual int CountItems(QueryFilter filter = null, bool ignoreListSettings = false)
		{
			lock (_syncLock)
			{
				return GetItems(filter, ignoreListSettings).Count();
			}
		}

		/* Navigation */

		public virtual IWorkItem Current => GetItem(CurrentIndex);

		protected int CurrentIndex { get; set; }

		/* This base class provides an overly simplistic implementation */
		/* TODO should honour Status, Visibility, and AreaOfInterest */

		public virtual bool CanGoFirst()
		{
			return _items.Count > 0;
		}

		public virtual void GoFirst()
		{
			CurrentIndex = 0;
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
			if (CurrentIndex < _items.Count - 1)
			{
				CurrentIndex += 1;
				//TODO should also set current item visited=true
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

		#region Non-public methods

		private IWorkItem GetItem(int index)
		{
			lock (_syncLock)
			{
				return 0 <= index && index < _items.Count
					       ? Assert.NotNull(_items[index])
					       : null;
			}
		}

		protected void SetItems(IEnumerable<IWorkItem> items)
		{
			lock (_syncLock)
			{
				_items.Clear();
				_items.AddRange(items.Where(item => item != null));

				CurrentIndex = -1;

				Extent = null;
			}
		}

		protected static Envelope GetExtentFromItems(IEnumerable<IWorkItem> items)
		{
			double xmin = double.MaxValue, ymin = double.MaxValue;
			double xmax = double.MinValue, ymax = double.MinValue;
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

					if (extent.XMax > xmax) xmax = extent.XMax;
					if (extent.YMax > ymax) ymax = extent.YMax;

					sref = extent.SpatialReference;

					count += 1;
				}
			}

			return count > 0
				       ? EnvelopeBuilder.CreateEnvelope(xmin, ymin, xmax, ymax, sref)
				       : EnvelopeBuilder.CreateEnvelope(sref); // empty
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

		private static bool StatusVisible(WorkItemStatus status, WorkItemVisibility visibility)
		{
			switch (status)
			{
				case WorkItemStatus.Todo:
					return (visibility & WorkItemVisibility.Todo) != 0;

				case WorkItemStatus.Done:
					return (visibility & WorkItemVisibility.Done) != 0;
			}

			return false;
		}

		private static bool WithinAreaOfInterest(Envelope extent, Polygon areaOfInterest)
		{
			if (extent == null) return false;
			if (areaOfInterest == null) return true;
			return GeometryEngine.Instance.Intersects(extent, areaOfInterest);
		}

		#endregion
	}
}
