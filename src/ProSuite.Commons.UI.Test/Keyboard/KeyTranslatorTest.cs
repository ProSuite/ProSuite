#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using System.Windows.Forms;
using NUnit.Framework;
using ProSuite.Commons.Keyboard;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.Commons.UI.Test.Keyboard
{
#if NET6_0_OR_GREATER
	[SupportedOSPlatform("windows")]
#endif
	[TestFixture]
	public class KeyTranslatorTest
	{
		[Test]
		public void CanGetKey()
		{
			IKeyTranslator translator = new KeyTranslator();
			int keyCode = translator.GetKey("NumPad7");

			Assert.AreEqual((int) Keys.NumPad7, keyCode);
		}

		[Test]
		public void CanGetKeyString()
		{
			IKeyTranslator translator = new KeyTranslator();
			string s = translator.GetKeyString((int) Keys.NumPad7);

			Assert.AreEqual("NumPad7", s);
		}
	}
}
