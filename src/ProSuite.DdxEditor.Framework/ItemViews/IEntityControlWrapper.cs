using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public interface IEntityControlWrapper<E> : IBoundView<E, IViewObserver>
		where E : Entity
	{
		void SetControl([NotNull] IWrappedEntityControl<E> panel);

		void OnBoundTo([NotNull] E entity);
	}
}
