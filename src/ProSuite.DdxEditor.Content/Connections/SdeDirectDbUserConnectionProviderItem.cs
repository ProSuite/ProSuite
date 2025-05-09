using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	public class SdeDirectDbUserConnectionProviderItem :
		SdeDirectConnectionProviderItem<SdeDirectDbUserConnectionProvider>
	{
		public SdeDirectDbUserConnectionProviderItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] SdeDirectDbUserConnectionProvider descriptor,
			[NotNull] IRepository<ConnectionProvider> repository)
			: base(modelBuilder, descriptor, repository) { }

		protected override void AddEntityPanels(
			ICompositeEntityControl<SdeDirectDbUserConnectionProvider, IViewObserver>
				compositeControl, IItemNavigation itemNavigation)
		{
			base.AddEntityPanels(compositeControl, itemNavigation);

			compositeControl.AddPanel(
				new SdeDirectDbUserConnProviderCtrl<SdeDirectDbUserConnectionProvider>());
		}
	}
}
