using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	public class SdeConnectionProviderItem<T> : ConnectionProviderItem<T>
		where T : SdeConnectionProvider
	{
		public SdeConnectionProviderItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] T descriptor,
			[NotNull] IRepository<ConnectionProvider> repository)
			: base(modelBuilder, descriptor, repository) { }

		protected override void AddEntityPanels(
			ICompositeEntityControl<T, IViewObserver> compositeControl)
		{
			base.AddEntityPanels(compositeControl);

			// could attach panel-specific presenter here, if needed
			compositeControl.AddPanel(new SdeConnProviderCtrl<T>());
		}
	}
}
