using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	public class ConnectionProviderPresenter<T> :
		EntityItemPresenter<T, IViewObserver, ConnectionProvider>
		where T : ConnectionProvider
	{
		public ConnectionProviderPresenter(ConnectionProviderItem<T> item,
		                                   IBoundView<T, IViewObserver> view)
			: base(item, view)
		{
			view.Observer = this;
		}
	}
}
