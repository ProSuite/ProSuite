using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Framework.Search
{
	public class SearchCommand : CommandBase
	{
		[NotNull] private readonly ISearchProvider _searchProvider;
		[NotNull] private readonly IItemNavigation _itemNavigation;
		[NotNull] private readonly IWin32Window _owner;

		public SearchCommand([NotNull] ISearchProvider searchProvider,
		                     [NotNull] IItemNavigation itemNavigation,
		                     [NotNull] IWin32Window owner)
		{
			Assert.ArgumentNotNull(searchProvider, nameof(searchProvider));
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));
			Assert.ArgumentNotNull(owner, nameof(owner));

			_searchProvider = searchProvider;
			_itemNavigation = itemNavigation;
			_owner = owner;
		}

		public override Image Image => _searchProvider.Image;

		public override string Text => _searchProvider.Text;

		protected override void ExecuteCore()
		{
			Entity entity = _searchProvider.SearchEntity(_owner);

			if (entity == null)
			{
				return;
			}

			_itemNavigation.GoToItem(entity);
		}
	}
}
