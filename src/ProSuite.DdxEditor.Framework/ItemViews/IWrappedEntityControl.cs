using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public interface IWrappedEntityControl<E> where E : Entity
	{
		void OnBindingTo([NotNull] E entity);

		void SetBinder([NotNull] ScreenBinder<E> binder);

		void OnBoundTo([NotNull] E entity);
	}
}
