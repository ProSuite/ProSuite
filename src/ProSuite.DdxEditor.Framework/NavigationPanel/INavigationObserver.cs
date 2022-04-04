using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public interface INavigationObserver
	{
		void HandleItemSelected([NotNull] Item item);

		bool PrepareItemSelection([NotNull] Item item);
	}
}
