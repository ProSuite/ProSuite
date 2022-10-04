using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public abstract class SimpleEntityItemPresenter<I> : ItemPresenter<I> where I : Item
	{
		protected SimpleEntityItemPresenter([NotNull] I item) : base(item) { }
	}
}
