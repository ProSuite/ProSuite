using System;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public abstract class EntityItemPresenter<E, O, BASE> :
		ItemPresenter<EntityItem<E, BASE>>
		where E : BASE
		where O : IViewObserver
		where BASE : Entity
	{
		[NotNull] private readonly IBoundView<E, O> _view;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="EntityItemPresenter&lt;E, O, BASE&gt;"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="view">The view.</param>
		protected EntityItemPresenter([NotNull] EntityItem<E, BASE> item,
		                              [NotNull] IBoundView<E, O> view)
			: base(item)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			_view = view;

			E entity = item.GetEntity();
			if (entity != null)
			{
				_view.BindTo(entity);
			}

			_view.Load += _view_Load;

			WireEvents(item);
		}

		#endregion

		#region Non-public members

		protected virtual void OnBoundTo([NotNull] E entity) { }

		protected virtual void OnUnloaded() { }

		private void BindToEntity()
		{
			E entity = Item.GetEntity();

			// can't use Assert.NotNull() -> runtime exception due to generic type constraint violation
			if (entity == null)
			{
				throw new AssertionException("entity is null");
			}

			_view.BindTo(entity);

			OnBoundTo(entity);
		}

		private void WireEvents([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			item.Unloaded += item_Unloaded;
			item.SavedChanges += item_SavedChanges;
			item.DiscardedChanges += item_DiscardedChanges;
		}

		private void UnwireEvents([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			item.Unloaded -= item_Unloaded;
			item.SavedChanges -= item_SavedChanges;
			item.DiscardedChanges -= item_DiscardedChanges;
		}

		#region Event handlers

		private void _view_Load(object sender, EventArgs e)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("EntityItemPresenter._view_Load");
			}

			E entity = Item.GetEntity();
			if (entity != null)
			{
				OnBoundTo(entity);
			}
		}

		private void item_Unloaded(object sender, EventArgs e)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("EntityItemPresenter.item_Unloaded");
			}

			UnwireEvents(Item);

			OnUnloaded();
		}

		private void item_DiscardedChanges(object sender, EventArgs e)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("EntityItemPresenter.item_DiscardedChanges");
			}

			BindToEntity();
		}

		private void item_SavedChanges(object sender, EventArgs e)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("EntityItemPresenter.item_SavedChanges");
			}

			BindToEntity();
		}

		#endregion

		#endregion
	}
}
