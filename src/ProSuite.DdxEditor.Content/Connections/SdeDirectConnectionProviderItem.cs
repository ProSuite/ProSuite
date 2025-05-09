using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	public class SdeDirectConnectionProviderItem<T> : SdeConnectionProviderItem<T>
		where T : SdeDirectConnectionProvider
	{
		public SdeDirectConnectionProviderItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] T descriptor,
			[NotNull] IRepository<ConnectionProvider> repository)
			: base(modelBuilder, descriptor, repository) { }

		protected override void AddEntityPanels(
			ICompositeEntityControl<T, IViewObserver> compositeControl,
			IItemNavigation itemNavigation)
		{
			base.AddEntityPanels(compositeControl, itemNavigation);

			// could attach panel-specific presenter here, if needed
			compositeControl.AddPanel(new SdeDirectConnProviderCtrl<T>());
		}
	}
}
