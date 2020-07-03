using System;
using NUnit.Framework;

namespace ProSuite.Commons.Test
{
	[TestFixture]
	public class RegistryUtilsTest
	{
		[Test]
		public void CanGetNonexistingValue()
		{
			string value = RegistryUtils.GetString(RegistryRootKey.LocalMachine,
			                                       "doesnotexist",
			                                       "neither");
			Assert.IsNull(value);
		}

		[Test]
		public void CanGetExistingStringValue()
		{
			string value = RegistryUtils.GetString(RegistryRootKey.LocalMachine,
			                                       @"SYSTEM\ControlSet001\Control",
			                                       "CurrentUser");

			Console.WriteLine(value);
			Assert.IsNotNull(value);
		}

		[Test]
		public void CanGetExistingInt32Value()
		{
			int? value = RegistryUtils.GetInt32(RegistryRootKey.LocalMachine,
			                                    @"SYSTEM\ControlSet001\Control",
			                                    "BootDriverFlags");

			Console.WriteLine(value);
			Assert.IsNotNull(value);
		}
	}
}