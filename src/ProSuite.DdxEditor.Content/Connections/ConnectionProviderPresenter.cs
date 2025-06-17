using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	public class ConnectionProviderPresenter<T> :
		EntityItemPresenter<T, IViewObserver, ConnectionProvider>
		where T : ConnectionProvider
	{
		public ConnectionProviderPresenter([NotNull] ConnectionProviderItem<T> item,
		                                   [NotNull] IBoundView<T, IViewObserver> view)
			: base(item, view)
		{
			view.Observer = this;
		}
	}
}
