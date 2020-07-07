using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class TriStateTreeView : CustomStateTreeView
	{
		protected override void CreateStateImages()
		{
			base.CreateStateImages();

			VisualStyleRenderer vsr = null;
			if (VisualStyleRenderer.IsSupported)
			{
				vsr = new VisualStyleRenderer(
					VisualStyleElement.Button.CheckBox.UncheckedNormal);
			}

			var smallIconSize =
				new Rectangle(0, 0,
				              StateImageList.ImageSize.Width,
				              StateImageList.ImageSize.Height);

			var bitmap = new Bitmap(StateImageList.ImageSize.Width,
			                        StateImageList.ImageSize.Height);

			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				// add unchecked checkbox image
				if (vsr == null)
				{
					ControlPaint.DrawCheckBox(graphics, smallIconSize, ButtonState.Normal);
				}
				else
				{
					vsr.SetParameters(VisualStyleElement.Button.CheckBox.UncheckedNormal);
					vsr.DrawBackground(graphics, smallIconSize);
				}

				StateImageList.Images.Add(bitmap, Color.Transparent);

				// add checked checkbox image
				if (vsr == null)
				{
					ControlPaint.DrawCheckBox(graphics, smallIconSize, ButtonState.Checked);
				}
				else
				{
					vsr.SetParameters(VisualStyleElement.Button.CheckBox.CheckedNormal);
					vsr.DrawBackground(graphics, smallIconSize);
				}

				StateImageList.Images.Add(bitmap, Color.Transparent);

				// add mixed checkbox image
				if (vsr == null)
				{
					ControlPaint.DrawMixedCheckBox(graphics, smallIconSize,
					                               ButtonState.Pushed);
				}
				else
				{
					vsr.SetParameters(VisualStyleElement.Button.CheckBox.MixedNormal);
					vsr.DrawBackground(graphics, smallIconSize);
				}

				StateImageList.Images.Add(bitmap, Color.Transparent);
			}
		}
	}
}
