using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using ProSuite.Commons.Exceptions;

namespace ProSuite.Commons.Test.Exceptions
{
	[TestFixture]
	public class ExceptionUtilsTest
	{
		[Test]
		public void CanFormatSimpleExceptionMessage()
		{
			var exception = new Exception("message");

			string message = ExceptionUtils.FormatMessage(exception);
			Console.WriteLine(message);

			Assert.AreEqual("message", message);
		}

		[Test]
		public void CanFormatComExceptionMessage()
		{
			var exception = new COMException("message", 2000);

			string message = ExceptionUtils.FormatMessage(exception);
			Console.WriteLine(message);

			Assert.AreEqual("message\r\n- Error code: 2000", message);
		}

		[Test]
		public void CanFormatMessageWithInnerException()
		{
			var inner = new COMException("inner", 2000);
			var outer = new Exception("outer", inner);

			string message = ExceptionUtils.FormatMessage(outer);
			Console.WriteLine(message);

			const string expected = "outer\r\n\r\n---> inner\r\n- Error code: 2000";

			Assert.AreEqual(expected, message);
		}

		[Test]
		public void CanFormatMessageWithInnerExceptions()
		{
			var inner2 = new COMException("inner2", 2000);
			var inner1 = new COMException("inner1", inner2);
			var outer = new Exception("outer", inner1);

			string message = ExceptionUtils.FormatMessage(outer);
			Console.WriteLine(message);

			const string expected =
				"outer\r\n\r\n---> inner1\r\n- Error code: -2147467259\r\n\r\n---> inner2\r\n- Error code: 2000";

			Assert.AreEqual(expected, message);
		}
	}
}