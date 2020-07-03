using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Collections
{
	/// <summary>
	/// A sortable BindingList implementation, using the
	/// PropertyComparer for sorting the items.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SortableBindingList<T> : BindingList<T>
	{
		private readonly bool _raiseListChangedEventAfterSort;
		private bool _isSorted;
		private ListSortDirection _sortDirection;
		[CanBeNull] private PropertyDescriptor _sortProperty;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		public SortableBindingList()
		{
			_isSorted = false;
			_sortProperty = null;
		}

		public SortableBindingList([NotNull] IList<T> items,
		                           bool raiseListChangedEventAfterSort = false) : base(items)
		{
			_raiseListChangedEventAfterSort = raiseListChangedEventAfterSort;
			_isSorted = false;
			_sortProperty = null;
		}

		#endregion

		public bool SuspendListChangedEvents
		{
			get { return ! RaiseListChangedEvents; }
			set
			{
				if (value == SuspendListChangedEvents)
				{
					// no change
					return;
				}

				const string fieldName = "raiseListChangedEvents";
				FieldInfo fi = typeof(BindingList<T>).GetField(fieldName,
				                                               BindingFlags.Instance |
				                                               BindingFlags.NonPublic);
				if (fi != null)
				{
					fi.SetValue(this, ! value);
				}
				else
				{
					_msg.DebugFormat("NOTE: field not found in BindingList<T>: {0}",
					                 fieldName);
				}
			}
		}

		public void WithSuspendedListChangedEvents([NotNull] Action procedure,
		                                           bool resetBindings = true)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			bool wasSuspended = SuspendListChangedEvents;

			try
			{
				SuspendListChangedEvents = true;

				procedure();
			}
			finally
			{
				SuspendListChangedEvents = wasSuspended;
			}

			if (! wasSuspended && resetBindings)
			{
				// was initially not suspended; reset bindings
				ResetBindings();
			}
		}

		protected override bool SupportsSortingCore => true;

		protected override void ApplySortCore(
			PropertyDescriptor property, ListSortDirection direction)
		{
			_sortProperty = property;
			_sortDirection = direction;
			// Get list to sort
			var items = Items as List<T>;

			// Apply and set the sort, if items to sort
			if (items != null)
			{
				var pc = new PropertyComparer<T>(property, direction);
				items.Sort(pc);
				_isSorted = true;
			}
			else
			{
				_isSorted = false;
			}

			// TOP-5096: this event seems to be unneeded (at least on .Net 4.5), and causes
			// a full data grid rebind, which takes a long time with longer lists -> for now, control via ctor parameter
			if (_raiseListChangedEventAfterSort)
			{
				// Let bound controls know they should refresh their views
				OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
			}
		}

		protected override bool IsSortedCore => _isSorted;

		protected override void RemoveSortCore()
		{
			_isSorted = false;
			_sortProperty = null;
		}

		protected override ListSortDirection SortDirectionCore => _sortDirection;

		protected override PropertyDescriptor SortPropertyCore => _sortProperty;
	}
}