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
		public void ApplyDarkTheme_LeavesTheColorsOfExcludedControls()
		{
			using (var form = new Form())
			{
				var kept = new TextBox { BackColor = Color.LightBlue, ForeColor = Color.Black };
				var themed = new TextBox { BackColor = Color.LightBlue };
				form.Controls.Add(kept);
				form.Controls.Add(themed);

				WinFormsThemeUtils.ExcludeColorsFromDarkTheme(kept);
				WinFormsThemeUtils.ApplyDarkTheme(form);

				Assert.AreEqual(Color.LightBlue, kept.BackColor);
				Assert.AreEqual(Color.Black, kept.ForeColor);
				Assert.Less(themed.BackColor.GetBrightness(), 0.5f);
			}
		}

		[Test]
		public void GetDarkThemeTextColorWithMostContrast_BlackOnLightBackground()
		{
			Assert.AreEqual(Color.Black, GetTextColor(Color.LightBlue));
			Assert.AreEqual(Color.Black, GetTextColor(Color.Red));
			Assert.AreEqual(Color.Black, GetTextColor(Color.Yellow));
			Assert.AreEqual(Color.Black, GetTextColor(Color.Orange));
			Assert.Greater(GetTextColor(Color.FromArgb(29, 80, 96)).GetBrightness(), 0.5f);
			Assert.Greater(GetTextColor(WinFormsThemeUtils.GetDarkBackColor(Color.White))
				               .GetBrightness(), 0.5f);
		}

		private static Color GetTextColor(Color backColor)
		{
			return WinFormsThemeUtils.GetDarkThemeTextColorWithMostContrast(backColor);
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
