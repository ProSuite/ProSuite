using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.Menus
{
	/// <summary>
	/// Manages context menus based on the context commands of items.
	/// </summary>
	public class MenuManager : IMenuManager
	{
		private readonly IApplicationController _applicationController;

		/// <summary>
		/// Initializes a new instance of the <see cref="MenuManager"/> class.
		/// </summary>
		/// <param name="applicationController">The application controller.</param>
		public MenuManager([NotNull] IApplicationController applicationController)
		{
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));

			_applicationController = applicationController;
		}

		#region IMenuManager Members

		public void AddMenuItems(ToolStripDropDownMenu menu,
		                         Item item,
		                         IList<Item> selectedChildren)
		{
			Assert.ArgumentNotNull(menu, nameof(menu));
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(selectedChildren, nameof(selectedChildren));

			menu.Items.Clear();

			ICollection<ICommand> commands =
				selectedChildren.Count == 1
					? selectedChildren[0].GetCommands(_applicationController)
					: item.GetCommands(_applicationController, selectedChildren);

			// get the commands for the item, create and add menu items
			foreach (ICommand command in commands)
			{
				// must be ToolStripMenuItems, not ToolStripButtons. Otherwise
				// the rendering is not correct (size, image in wrong place)
				menu.Items.Add(new CommandMenuItem(command));
			}
		}

		public void AddMenuItems(ToolStripDropDownMenu menu, Item item)
		{
			Assert.ArgumentNotNull(menu, nameof(menu));
			Assert.ArgumentNotNull(item, nameof(item));

			menu.Items.Clear();

			// get the commands for the item, create and add menu items
			foreach (ICommand command in item.GetCommands(_applicationController))
			{
				// must be ToolStripMenuItems, not ToolStripButtons. Otherwise
				// the rendering is not correct (size, image in wrong place)
				menu.Items.Add(new CommandMenuItem(command));
			}
		}

		#endregion
	}
}
