using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.AssociationEnds
{
	public class AssociationEndPresenter : SimpleEntityItemPresenter<AssociationEndItem>,
	                                       IAssociationEndObserver
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AssociationEndPresenter"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="view">The view.</param>
		public AssociationEndPresenter([NotNull] AssociationEndItem item,
		                               [NotNull] IAssociationEndView view)
			: base(item)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			view.Observer = this;
		}
	}
}
