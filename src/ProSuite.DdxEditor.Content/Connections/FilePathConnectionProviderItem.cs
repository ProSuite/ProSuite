using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.Geodatabase;

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
				compositeControl)
		{
			base.AddEntityPanels(compositeControl);

			compositeControl.AddPanel(
				new FilePathConnectionProviderControl<FilePathConnectionProviderBase>(GetEntity()));
		}
	}
}
