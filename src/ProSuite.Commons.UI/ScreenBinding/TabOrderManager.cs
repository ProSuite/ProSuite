using System.Windows.Forms;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public class TabOrderManager
	{
		//private readonly Control _parent;
		private int _tabOrder = 1;

		public TabOrderManager(Control parent)
		{
			var tab = 1000;
			foreach (Control child in parent.Controls)
			{
				child.TabIndex = tab++;
			}
		}

		public TabOrderManager TheTabOrderIs(params Control[] controls)
		{
			foreach (Control control in controls)
			{
				control.TabIndex = _tabOrder++;
			}

			return this;
		}

		public TabOrderManager Then(params Control[] controls)
		{
			foreach (Control control in controls)
			{
				control.TabIndex = _tabOrder++;
			}

			return this;
		}
	}
}
