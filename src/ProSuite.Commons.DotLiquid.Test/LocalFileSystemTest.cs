using System;
using System.IO;
using NUnit.Framework;
using Assert = ProSuite.Commons.Essentials.Assertions.Assert;

namespace ProSuite.Commons.DotLiquid.Test
{
	[TestFixture]
	public class LocalFileSystemTest
	{
		[Test]
		public void CanResolvePartialTemplate()
		{
			const string templatePath =
				"C:\\Program Files (x86)\\ProSuite\\bin\\config\\template.html.tpl";
			const string partialName = "partial_name";

			string templateDirectory =
				Assert.NotNull(Path.GetDirectoryName(Path.GetFullPath(templatePath)));

			var fileSystem = new LocalFileSystem(templateDirectory);

			string result = fileSystem.FullPath(partialName);

			Console.WriteLine(result);
			NUnit.Framework.Assert.AreEqual(Path.Combine(templateDirectory,
			                                             $"_{partialName}.liquid"),
			                                result);
		}

		[Test]
		public void CanResolvePartialTemplateInSubdirectory()
		{
			const string templatePath =
				"C:\\Program Files (x86)\\ProSuite\\bin\\config\\template.html.tpl";
			const string partialName = "partial_name";
			const string partialRelativeName = "sub/directory/" + partialName;

			string templateDirectory =
				Assert.NotNull(Path.GetDirectoryName(Path.GetFullPath(templatePath)));

			var fileSystem = new LocalFileSystem(templateDirectory);

			string result = fileSystem.FullPath(partialRelativeName);

			Console.WriteLine(result);
			string expectedDirectory = Path.Combine(Path.Combine(templateDirectory, "sub"),
			                                        "directory");
			NUnit.Framework.Assert.AreEqual(Path.Combine(expectedDirectory,
			                                             $"_{partialName}.liquid"),
			                                result);
		}
	}
}
