using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.Models
{
	public class ModelsPresenter : ItemPresenter<ModelsItemBase>, IModelsObserver
	{
		public ModelsPresenter(IModelsView view, ModelsItemBase item) : base(item)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			view.Observer = this;
		}
	}
}
