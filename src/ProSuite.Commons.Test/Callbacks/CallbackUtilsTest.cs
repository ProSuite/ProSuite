using NUnit.Framework;
using ProSuite.Commons.Callbacks;

namespace ProSuite.Commons.Test.Callbacks
{
	public class CallbackUtilsTest
	{
		[Test]
		public void CanDoWithNonNull()
		{
			string result = null;
			CallbackUtils.DoWithNonNull("bla", s => result = s);

			Assert.AreEqual("bla", result);

			CallbackUtils.DoWithNonNull<string>(null, s => result = s);

			Assert.AreEqual("bla", result);
		}
	}
}
