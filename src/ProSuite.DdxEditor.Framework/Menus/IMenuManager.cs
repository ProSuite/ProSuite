using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.Menus
{
	public interface IMenuManager
	{
		void AddMenuItems([NotNull] ToolStripDropDownMenu menu,
		                  [NotNull] Item item,
		                  [NotNull] IList<Item> selectedChildren);

		void AddMenuItems([NotNull] ToolStripDropDownMenu menu,
		                  [NotNull] Item item);
	}
}
