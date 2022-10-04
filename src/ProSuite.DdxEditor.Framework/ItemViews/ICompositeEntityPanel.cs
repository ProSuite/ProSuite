using ProSuite.Commons.DomainModels;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public interface ICompositeEntityControl<T, O> : IBoundView<T, O> where T : Entity
		where O :
		IViewObserver
	{
		void AddPanel(IEntityPanel<T> panel);
	}
}
