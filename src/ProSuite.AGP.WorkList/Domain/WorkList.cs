using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
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
		private readonly object _syncLock = new object();
		private readonly List<IWorkItem> _items;

		protected WorkList(string name)
		{
			Name = name ?? string.Empty;

			Visibility = WorkItemVisibility.Todo;
			AreaOfInterest = null;

			_items = new List<IWorkItem>();
			CurrentIndex = -1;
		}

		public string Name { get; }

		public GeometryType GeometryType { get; protected set; }

		public Envelope Extent { get; protected set; }

		public WorkItemVisibility Visibility { get; set; }

		public Polygon AreaOfInterest { get; set; }

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool ignoreListSettings = false)
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

			if (! string.IsNullOrEmpty(filter?.WhereClause))
			{
				int foo = 13; // TODO honour WhereClause
			}

			if (filter is SpatialQueryFilter sf)
			{
				int bar = 42; // TODO honour spatial filter
			}

			if (!ignoreListSettings && AreaOfInterest != null)
			{
				query = query.Where(item => WithinAreaOfInterest(item.Extent, AreaOfInterest));
			}

			return query;
		}

		public int CountItems(QueryFilter filter = null, bool ignoreListSettings = false)
		{
			lock (_syncLock)
			{
				// TODO honour filter (incl spatial)!
				return _items.Count(item => ignoreListSettings ||
				                            StatusVisible(item.Status, Visibility) &&
				                            WithinAreaOfInterest(item.Extent, AreaOfInterest));
			}
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

		/* Navigation */

		public IWorkItem Current => GetItem(CurrentIndex);

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
			throw new System.NotImplementedException();
		}

		public virtual bool CanGoNext()
		{
			return _items.Count > 0 && CurrentIndex < _items.Count - 1;
		}

		public virtual void GoNext()
		{
			CurrentIndex += 1;
		}

		public virtual bool CanGoPrevious()
		{
			return _items.Count > 0 && CurrentIndex > 0;
		}

		public virtual void GoPrevious()
		{
			CurrentIndex -= 1;
		}

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
				GeometryType = GeometryType.Unknown;
			}
		}

		protected static GeometryType GetGeometryTypeFromItems(IEnumerable<IWorkItem> items)
		{
			GeometryType? type = null;

			if (items != null)
			{
				foreach (var item in items)
				{
					if (item == null) continue;
					var shape = item.Shape;
					if (shape == null) continue;

					if (type == null)
						type = shape.GeometryType;
					else if (type != shape.GeometryType)
						type = GeometryType.Unknown;
				}
			}

			return type ?? GeometryType.Unknown;
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

		#endregion
	}
}
