using ProSuite.Commons.DomainModels;
using ProSuite.Commons.UI.ScreenBinding;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public interface IEntityPanel<E> where E : Entity
	{
		string Title { get; }

		void OnBindingTo(E entity);

		void SetBinder(ScreenBinder<E> binder);

		void OnBoundTo(E entity);
	}
}
