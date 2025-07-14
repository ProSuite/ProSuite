using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.CommandLine;

namespace ProSuite.Commons.Test.CommandLine
{
	[TestFixture]
	public class CommandLineUtilsTest
	{
		[Test]
		public void CanParseGlobalOptions()
		{
			var dict1 = CommandLineUtils.ParseGlobalOptions(null);
			Assert.IsNull(dict1);

			var dict2 = CommandLineUtils.ParseGlobalOptions(Array.Empty<string>());
			Assert.IsNull(dict2);

			var dict3 = CommandLineUtils.ParseGlobalOptions(MakeArgs("arg", "--opt"));
			Assert.NotNull(dict3);
			Assert.IsEmpty(dict3);

			var dict4 = CommandLineUtils.ParseGlobalOptions(
				MakeArgs("--flag", "--opt", "arg", "--foo=bar", "--", "--not-an-option"));
			Assert.NotNull(dict4);
			Assert.Contains(MakePair("opt", "arg"), dict4);
			Assert.Contains(MakePair("flag", "true"), dict4);
			Assert.Contains(MakePair("foo", "bar"), dict4);
			Assert.AreEqual(3, dict4.Count);

			var dict5 = CommandLineUtils.ParseGlobalOptions(
				MakeArgs("--ddx=OSA", "--python", "Path/To/Python.exe", "command"));
			Assert.NotNull(dict5);
			Assert.Contains(MakePair("ddx", "OSA"), dict5);
			Assert.Contains(MakePair("python", "Path/To/Python.exe"), dict5);
			Assert.AreEqual(2, dict5.Count);

			var dict6 = CommandLineUtils.ParseGlobalOptions(
				MakeArgs("--ddx=", "list")); // empty option argument
			Assert.NotNull(dict6);
			Assert.Contains(MakePair("ddx", ""), dict6);
			Assert.AreEqual(1, dict6.Count);
		}

		private static KeyValuePair<string, string> MakePair(string option, string argument)
		{
			return new KeyValuePair<string, string>(option, argument);
		}

		private static string[] MakeArgs(params string[] args)
		{
			return args;
		}
	}
}
