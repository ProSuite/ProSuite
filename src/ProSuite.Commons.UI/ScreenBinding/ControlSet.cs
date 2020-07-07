using System.Collections;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public class ControlSet
	{
		private readonly ArrayList _all = new ArrayList();

		public int Count => _all.Count;

		public Control[] AllControls => (Control[]) _all.ToArray(typeof(Control));

		public void Add(object control)
		{
			if (! _all.Contains(control))
			{
				_all.Add(control);
			}
		}

		public bool Contains(object control)
		{
			return _all.Contains(control);
		}

		public void AddRange(object[] controls)
		{
			foreach (object control in controls)
			{
				Add(control);
			}
		}
	}
}
