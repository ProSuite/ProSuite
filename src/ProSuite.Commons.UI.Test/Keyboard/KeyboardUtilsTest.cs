using System.Runtime.Versioning;
using System.Windows.Forms;
using NUnit.Framework;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.Commons.UI.Test.Keyboard
{
	[SupportedOSPlatform("windows")]
	[TestFixture]
	public class KeyboardUtilsTest
	{
		[Test]
		public void CanGetDisplayText1()
		{
			Assert.AreEqual("Escape",
			                KeyboardUtils.GetDisplayText(
				                KeyboardUtils.CreateShortcut(Keys.Escape)));
		}

		[Test]
		public void CanGetDisplayText2()
		{
			Assert.AreEqual("CTRL+SHIFT+S",
			                KeyboardUtils.GetDisplayText(
				                KeyboardUtils.CreateShortcut(Keys.S, true, true)));
		}

		[Test]
		public void CanGetDisplayText3()
		{
			Assert.AreEqual("ALT+A",
			                KeyboardUtils.GetDisplayText(
				                KeyboardUtils.CreateShortcut(Keys.A, false, false,
				                                             true)));
		}

		[Test]
		public void CanGetDisplayText4()
		{
			Assert.AreEqual("F6",
			                KeyboardUtils.GetDisplayText(
				                KeyboardUtils.CreateShortcut(Keys.F6)));
		}

		[Test]
		public void CanGetDisplayTextNumericKeys()
		{
			var numericKeys = new[]
			                  {
				                  Keys.D1, Keys.D2, Keys.D3,
				                  Keys.D4, Keys.D5, Keys.D6,
				                  Keys.D7, Keys.D8, Keys.D9
			                  };

			int keyValue = 1;

			foreach (Keys numericKey in numericKeys)
			{
				Assert.AreEqual("CTRL+" + keyValue,
				                KeyboardUtils.GetDisplayText(
					                KeyboardUtils.CreateShortcut(numericKey, true)));
				keyValue++;
			}
		}
	}
}
