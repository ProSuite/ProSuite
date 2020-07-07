using System.Windows.Forms;

namespace ProSuite.Commons.UI.ScreenBinding.StateMachine
{
	public class VisibleState
	{
		private readonly ControlSet _allControls;
		private readonly ControlSet _controls = new ControlSet();

		public VisibleState(ControlSet allControls)
		{
			_allControls = allControls;
		}

		public void Show(params Control[] controls)
		{
			_controls.AddRange(controls);
			_allControls.AddRange(controls);
		}

		public void Process(IScreenBinder binder)
		{
			binder.InsideLatch(
				delegate
				{
					foreach (Control control in _allControls.AllControls)
					{
						IScreenElement element = binder.FindElementForControl(control);
						if (element == null)
						{
							control.Hide();
						}
						else
						{
							element.Hide();
						}
					}

					foreach (Control control in _controls.AllControls)
					{
						IScreenElement element = binder.FindElementForControl(control);
						if (element == null)
						{
							control.Show();
						}
						else
						{
							element.Show();
						}
					}
				});
		}
	}
}
