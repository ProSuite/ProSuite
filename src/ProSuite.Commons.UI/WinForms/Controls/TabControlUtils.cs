using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public static class TabControlUtils
	{
		/// <summary>
		/// Gets the name of the selected tab page on the specified tab control.
		/// </summary>
		/// <param name="tabControl">The tab control</param>
		/// <returns>The name of the selected tab page, or null if there is no selected tab page.</returns>
		[CanBeNull]
		public static string GetSelectedTabPageName([NotNull] TabControl tabControl)
		{
			Assert.ArgumentNotNull(tabControl, nameof(tabControl));

			return tabControl.SelectedTab?.Name;
		}

		/// <summary>
		/// Selects the tab page of a given name on a specified tab control, and returns the selected tab page.
		/// </summary>
		/// <param name="tabControl">The tab control to select on</param>
		/// <param name="tabPageName">The name of the tab page to select</param>
		/// <returns>The selected tab page, or null if no tab page was selected.</returns>
		[CanBeNull]
		public static TabPage SelectTabPage([NotNull] TabControl tabControl,
		                                    [CanBeNull] string tabPageName)
		{
			Assert.ArgumentNotNull(tabControl, nameof(tabControl));

			foreach (TabPage tabPage in tabControl.TabPages)
			{
				if (! string.Equals(tabPage.Name, tabPageName, StringComparison.Ordinal))
				{
					continue;
				}

				tabControl.SelectTab(tabPage);
				return tabPage;
			}

			return null;
		}
	}
}
