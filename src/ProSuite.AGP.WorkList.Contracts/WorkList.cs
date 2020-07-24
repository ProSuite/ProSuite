using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	/// <summary>
	/// A WorkList is a named list of work items.
	/// It maintains a current item and provides
	/// navigation to change the current item.
	/// </summary>
	public abstract class WorkList
	{
		private readonly object _syncLock = new object();
		private readonly List<WorkItem> _items;

		protected WorkList(string name)
		{
			Name = name ?? string.Empty;

			Visibility = WorkItemVisibility.Todo;
			AreaOfInterest = null;

			_items = new List<WorkItem>();
			CurrentIndex = -1;

			Items = new ReadOnlyCollection<WorkItem>(_items);
		}

		[NotNull]
		public string Name { get; }

		public WorkItemVisibility Visibility { get; set; }

		[CanBeNull]
		public Polygon AreaOfInterest { get; set; }

		[CanBeNull]
		public Envelope Extent { get; protected set; }

		public GeometryType GeometryType { get; protected set; }

		[CanBeNull]
		public WorkItem Current => GetItem(CurrentIndex);

		protected int CurrentIndex { get; set; }

		[NotNull]
		public IReadOnlyList<WorkItem> Items { get; }

		/* Navigation */

		public abstract bool CanGoFirst();

		public abstract void GoFirst();

		public abstract bool CanGoNearest();

		public abstract void GoNearest();

		public abstract bool CanGoNext();

		public abstract void GoNext();

		public abstract bool CanGoPrevious();

		public abstract void GoPrevious();

		#region Non-public methods

		private WorkItem GetItem(int index)
		{
			lock (_syncLock)
			{
				return 0 <= index && index < _items.Count
					       ? Assert.NotNull(_items[index])
					       : null;
			}
		}

		protected void SetItems(IEnumerable<WorkItem> items)
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

		protected static GeometryType GetGeometryTypeFromItems(IEnumerable<WorkItem> items)
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

		protected static Envelope GetExtentFromItems(IEnumerable<WorkItem> items)
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
