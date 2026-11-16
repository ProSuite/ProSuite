using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using ProSuite.Commons.UI.WinForms;

namespace ProSuite.Commons.UI.Test.WinForms
{
	[TestFixture]
	public class WinFormsThemeUtilsTest
	{
		[Test]
		public void GetDarkBackColor_WhiteBecomesControlBackground()
		{
			Color dark = WinFormsThemeUtils.GetDarkBackColor(Color.White);

			Assert.AreEqual(Color.FromArgb(0x24, 0x24, 0x24).ToArgb(), dark.ToArgb());
		}

		[Test]
		public void GetDarkBackColor_TintedColorKeepsHue()
		{
			Color dark = WinFormsThemeUtils.GetDarkBackColor(Color.LightBlue);

			Assert.Less(dark.GetBrightness(), 0.5f);
			Assert.AreEqual(Color.LightBlue.GetHue(), dark.GetHue(), 2);
		}

		[Test]
		public void GetDarkBackColor_DarkColorIsUnchanged()
		{
			Color red = Color.FromArgb(200, 0, 0);

			Assert.AreEqual(red, WinFormsThemeUtils.GetDarkBackColor(red));
		}

		[Test]
		public void ApplyDarkThemeOfOwner_FollowsTheFormOfTheOwner()
		{
			using (var darkForm = new Form())
			using (var lightForm = new Form())
			using (var darkDialog = new Form())
			using (var lightDialog = new Form())
			{
				var owner = new Button();
				darkForm.Controls.Add(owner);
				WinFormsThemeUtils.ApplyDarkTheme(darkForm);

				WinFormsThemeUtils.ApplyDarkThemeOfOwner(owner, darkDialog);
				WinFormsThemeUtils.ApplyDarkThemeOfOwner(lightForm, lightDialog);

				Assert.Less(darkDialog.BackColor.GetBrightness(), 0.5f);
				Assert.Greater(lightDialog.BackColor.GetBrightness(), 0.5f);
			}
		}
	}
}
