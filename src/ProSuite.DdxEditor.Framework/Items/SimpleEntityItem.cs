using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Framework.Items
{
	public abstract class SimpleEntityItem<E, BASE> : EntityItem<E, BASE>
		where BASE : Entity
		where E : BASE
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleEntityItem&lt;E, BASE&gt;"/> class.
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <param name="repository">The repository.</param>
		protected SimpleEntityItem([NotNull] E entity,
		                           [NotNull] IRepository<BASE> repository)
			: base(entity, repository) { }

		protected sealed override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			var result = new EntityControlWrapper<E>();

			result.SetControl(CreateEntityControl(itemNavigation));
			new WrappedEntityItemPresenter<E, BASE>(this, result);

			return result;
		}

		[NotNull]
		protected abstract IWrappedEntityControl<E> CreateEntityControl(
			[NotNull] IItemNavigation itemNavigation);
	}
}
