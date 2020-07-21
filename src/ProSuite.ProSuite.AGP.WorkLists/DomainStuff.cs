using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AO.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.ProSuite.AGP.WorkLists
{
	// TODO to be refactored into separate files

	public enum WorkItemStatus
	{
		Unknown = 0,
		Todo,
		Done
	}

	public enum WorkItemVisibility
	{
		None = 0,
		Todo,
		All
	}

	public abstract class WorkItem
	{
		public GdbObjectReference Proxy { get; protected set; }

		public string Description { get; protected set; }

		public WorkItemStatus Status { get; protected set; }

		public bool Visited { get; protected set; }

		public Envelope Extent { get; protected set; }

		public abstract void SetStatus(WorkItemStatus status);

		public abstract void SetVisited(bool visited);
	}

	/// <summary>
	/// A named list of work items; maintains a current item;
	/// provides navigation to change current item.
	/// </summary>
	public abstract class WorkList
	{
		private IList<WorkItem> _items;

		protected WorkList(string name)
		{
			Name = name;
			Current = null;

			SetItems(Enumerable.Empty<WorkItem>());
		}

		protected void SetItems(IEnumerable<WorkItem> items)
		{
			_items = items.ToList();
			Items = new ReadOnlyCollection<WorkItem>(_items);

			Current = null;
		}

		public string Name { get; }

		public WorkItemVisibility Visibility { get; set; }

		public Polygon AreaOfInterest { get; set; }

		public Envelope Extent { get; }

		[CanBeNull]
		public WorkItem Current { get; private set; }

		[NotNull]
		public IReadOnlyList<WorkItem> Items { get; private set; }

		/* Navigation */

		public abstract bool CanGoFirst();

		public abstract void GoFirst();

		public abstract bool CanGoNearest();

		public abstract void GoNearest();

		public abstract bool CanGoNext();

		public abstract void GoNext();

		public abstract bool CanGoPrevious();

		public abstract void GoPrevious();
	}
}
