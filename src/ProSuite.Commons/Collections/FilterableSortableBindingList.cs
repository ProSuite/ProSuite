using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Collections
{
	public class FilterableSortableBindingList<T> : SortableBindingList<T>
	{
		private readonly List<T> _itemsCache = new List<T>();

		public FilterableSortableBindingList() {}

		public FilterableSortableBindingList([NotNull] IList<T> items,
		                                     bool raiseListChangedEventAfterSort = false) :
			base(items, raiseListChangedEventAfterSort) { }

		public void ApplySort([NotNull] PropertyDescriptor property,
		                      ListSortDirection direction)
		{
			ApplySortCore(property, direction);
		}

		public void ApplyFilter([CanBeNull] Predicate<T> predicate)
		{
			if (_itemsCache.Count > 0)
			{
				Items.Clear();

				_itemsCache.Reverse();

				foreach (T item in _itemsCache)
				{
					Items.Add(item);
				}

				_itemsCache.Clear();
			}

			if (predicate != null)
			{
				for (int index = Count - 1; index >= 0; index--)
				{
					T item = Items[index];

					_itemsCache.Add(item);

					if (! predicate(item))
					{
						Items.RemoveAt(index);
					}
				}
			}

			if (SortPropertyCore != null)
			{
				ApplySortCore(SortPropertyCore, SortDirectionCore);
				// this includes an OnListChanged
			}
			else
			{
				OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
			}
		}

		public void RemoveFilter()
		{
			ApplyFilter(null);
		}

		protected override void ClearItems()
		{
			_itemsCache.Clear();

			base.ClearItems();
		}

		protected override void RemoveItem(int index)
		{
			T item = Items[index]; // candidate for removal

			// Expensive... But hey! FilterableSortableBindingList
			// is meant for small lists of, say, a few dozen items.
			if (_itemsCache.Contains(item))
			{
				_itemsCache.Remove(item);
			}

			base.RemoveItem(index);
		}
	}
}
