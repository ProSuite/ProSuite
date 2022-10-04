using System;
using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.TableRows;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public class ItemTablePresenter<T> : IItemTableObserver<T> where T : class
	{
		private readonly IItemTableView<T> _view;
		private readonly Item _item;
		private readonly IItemNavigation _itemNavigation;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemTablePresenter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="item">The item.</param>
		/// <param name="itemNavigation">The item navigation.</param>
		public ItemTablePresenter([NotNull] IItemTableView<T> view,
		                          [NotNull] Item item,
		                          [NotNull] IItemNavigation itemNavigation)
		{
			Assert.ArgumentNotNull(view, nameof(view));
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			_view = view;
			_view.Observer = this;
			_item = item;
			_itemNavigation = itemNavigation;

			WireEvents();
		}

		#endregion

		#region IItemsTableObserver<T> Members

		void IItemTableObserver<T>.RowDoubleClicked(T row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			Item item = GetItem(row);

			if (item != null)
			{
				_itemNavigation.GoToItem(item);
			}
		}

		void IItemTableObserver<T>.RowRightClicked(T row, Point location)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			IList<Item> selectedChildren = GetItems(_view.GetSelectedRows());
			if (selectedChildren.Count > 0)
			{
				_view.ShowItemCommands(_item, selectedChildren, location);
			}
		}

		#endregion

		#region Non-public members

		[NotNull]
		private IEnumerable<T> GetRows([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			var result = new List<T>();

			foreach (T row in _view.Rows)
			{
				if (row is IItemRow itemRow)
				{
					if (Equals(itemRow.Item, item))
					{
						result.Add(row);
					}
				}
				else if (row is IEntityRow entityRow &&
				         item is IEntityItem entityItem)
				{
					Entity entity = entityRow.Entity;

					if (entityItem.IsBasedOn(entity))
					{
						result.Add(row);
					}
				}
			}

			return result;
		}

		[NotNull]
		private IList<Item> GetItems([NotNull] IEnumerable<T> rows)
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

			var result = new List<Item>();

			var entities = new List<Entity>();
			foreach (T row in rows)
			{
				if (row is IItemRow itemRow)
				{
					result.Add(itemRow.Item);
				}
				else
				{
					var entityRow = row as IEntityRow;

					if (entityRow?.Entity != null)
					{
						entities.Add(entityRow.Entity);
					}
				}
			}

			if (entities.Count > 0)
			{
				foreach (Item child in _item.Children)
				{
					if (child is IEntityItem entityItem)
					{
						foreach (Entity entity in entities)
						{
							if (entityItem.IsBasedOn(entity))
							{
								result.Add(child);
							}
						}
					}
				}
			}

			return result;
		}

		[CanBeNull]
		private Item GetItem([NotNull] T row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			if (row is IEntityRow entityRow)
			{
				Entity entity = entityRow.Entity;

				foreach (Item child in _item.Children)
				{
					if (child is IEntityItem entityItem)
					{
						if (entityItem.IsBasedOn(entity))
						{
							return child;
						}
					}
				}
			}
			else if (row is IItemRow itemRow)
			{
				return itemRow.Item;
			}

			return null;
		}

		private void WireEvents()
		{
			_view.Disposed += _view_Disposed;
			_item.ChildRemoved += _item_ChildRemoved;
			_item.ChildrenRefreshed += _item_ChildrenRefreshed;
		}

		private void UnwireEvents()
		{
			_view.Disposed -= _view_Disposed;
			_item.ChildRemoved -= _item_ChildRemoved;
			_item.ChildrenRefreshed -= _item_ChildrenRefreshed;
		}

		#region Event handlers

		private void _view_Disposed(object sender, EventArgs e)
		{
			UnwireEvents();
		}

		private void _item_ChildrenRefreshed(object sender, EventArgs e)
		{
			_view.UpdateRows();
		}

		private void _item_ChildRemoved(object sender, ItemEventArgs e)
		{
			foreach (T row in GetRows(e.Item))
			{
				_view.RemoveRow(row);
			}
		}

		#endregion

		#endregion
	}
}
