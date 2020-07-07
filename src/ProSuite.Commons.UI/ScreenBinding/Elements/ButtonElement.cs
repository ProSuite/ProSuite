using System;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class ButtonElement : ScreenElement<Button>
	{
		public ButtonElement(Button control) : base(control)
		{
			Alias = control.Text;
		}

		public void OnClick(Action action)
		{
			BoundControl.Click += delegate { action(); };
		}
	}
}
