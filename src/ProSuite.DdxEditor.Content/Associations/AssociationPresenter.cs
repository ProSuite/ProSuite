using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.Associations
{
	public class AssociationPresenter : SimpleEntityItemPresenter<AssociationItem>
	{
		public AssociationPresenter([NotNull] AssociationItem item) : base(item)
		{
			//            _view = view;
			//            _view.Observer = this;
		}
	}
}
