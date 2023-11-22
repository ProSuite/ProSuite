using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Framework.Items
{
	public abstract class SubclassedEntityItem<E, BASE> : EntityItem<E, BASE>
		where BASE : Entity
		where E : BASE
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SubclassedEntityItem&lt;T, BASE&gt;"/> class.
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <param name="repository">The repository.</param>
		protected SubclassedEntityItem([NotNull] E entity,
		                               [NotNull] IRepository<BASE> repository)
			: base(entity, repository) { }

		protected sealed override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			ICompositeEntityControl<E, IViewObserver> control =
				new SegmentedEntityControl<E>();

			AddEntityPanels(control, itemNavigation);
			AttachPresenter(control);

			return (Control) control;
		}

		protected abstract void AttachPresenter(
			[NotNull] ICompositeEntityControl<E, IViewObserver> control);

		protected abstract void AddEntityPanels(
			[NotNull] ICompositeEntityControl<E, IViewObserver> compositeControl,
			[NotNull] IItemNavigation itemNavigation);
	}
}
