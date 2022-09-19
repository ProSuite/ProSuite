using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public abstract class ItemPresenter<I> : IItemPresenter, IViewObserver where I : Item
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemPresenter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		protected ItemPresenter([NotNull] I item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			Item = item;
		}

		[NotNull]
		protected I Item { get; }

		#region IViewObserver Members

		public void NotifyChanged(bool dirty)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("ItemPresenter.NotifyChanged({0})", dirty);
			}

			//if (dirty)
			//{
			//    Item.NotifyChanged();                
			//}

			// NOTE not all controls have a boundscreenelement, so we can't rely on
			//      screenbinder.IsDirty to set Item.Dirty state. Explore alternative: nh change detection?
			Item.NotifyChanged();
		}

		#endregion
	}
}
