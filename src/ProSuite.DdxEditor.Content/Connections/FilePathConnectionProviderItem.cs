using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	public class FilePathConnectionProviderItem :
		ConnectionProviderItem<FilePathConnectionProviderBase>
	{
		public FilePathConnectionProviderItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] FilePathConnectionProviderBase descriptor,
			[NotNull] IRepository<ConnectionProvider> repository)
			: base(modelBuilder, descriptor, repository) { }

		protected override void AddEntityPanels(
			ICompositeEntityControl<FilePathConnectionProviderBase, IViewObserver>
				compositeControl, IItemNavigation itemNavigation)
		{
			base.AddEntityPanels(compositeControl, itemNavigation);

			compositeControl.AddPanel(
				new FilePathConnectionProviderControl<FilePathConnectionProviderBase>(GetEntity()));
		}
	}
}
