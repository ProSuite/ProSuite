using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace ProSuite.Commons.Test
{
	[TestFixture]
	public class ConversionUtilsTest
	{
		private static void AssertValidGuid(string guidString)
		{
			Console.WriteLine(guidString);

			Assert.IsTrue(ConversionUtils.IsValidGuid(guidString));

			string upper = guidString.ToUpper();
			Console.WriteLine(upper);

			Assert.IsTrue(ConversionUtils.IsValidGuid(upper));
		}

		[Test]
		public void CanRecognizeValidGuid()
		{
			AssertValidGuid(Guid.NewGuid().ToString());
		}

		[Test]
		public void CanRecognizeValidGuidsFastEnough()
		{
			// call once to make sure the regex is compiled (happens on first call)
			ConversionUtils.IsValidGuid(Guid.NewGuid().ToString());

			const int count = 100000;
			const double maxMilliseconds = 200;

			var guids = new List<string>(count);

			for (int i = 0; i < count; i++)
			{
				guids.Add(Guid.NewGuid().ToString());
			}

			var watch = new Stopwatch();
			watch.Start();

			foreach (string guidString in guids)
			{
				bool valid = ConversionUtils.IsValidGuid(guidString);
				if (! valid)
				{
					Assert.Fail("guid not recognized as valid: {0}", guidString);
				}
			}

			watch.Stop();

			Console.Out.WriteLine("Generating {0} guids took {1:N0} ms", count,
			                      watch.ElapsedMilliseconds);

			Assert.Less(watch.ElapsedMilliseconds, maxMilliseconds, "guid validation too slow");
		}

		[Test]
		public void CanRecognizeValidGuidFormatB()
		{
			AssertValidGuid(Guid.NewGuid().ToString("B"));
		}

		[Test]
		public void CanRecognizeValidGuidFormatD()
		{
			AssertValidGuid(Guid.NewGuid().ToString("D"));
		}

		[Test]
		[Ignore("Format 'N' not supported. Example: d57f872f84034fba8b5edd28c6847cf8")]
		public void CanRecognizeValidGuidFormatN()
		{
			AssertValidGuid(Guid.NewGuid().ToString("N"));
		}

		[Test]
		[Ignore("Format 'P' not supported. Example: (c073107b-d78a-4107-9496-cec80074fde9)")
		]
		public void CanRecognizeValidGuidFormatP()
		{
			AssertValidGuid(Guid.NewGuid().ToString("P"));
		}
	}
}