using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using ProSuite.Processing.Domain;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class CartoProcessRepoTest
	{
		[Test]
		public void CanLoad()
		{
			var repo = new CartoProcessRepo();

			var knownTypes = GetKnownTypes();
			var xml = GetTestXmlConfig();

			using (var reader = new StringReader(xml))
			{
				repo.Load(reader, knownTypes);
			}

			Assert.AreEqual(3, repo.ProcessDefinitions.Count);

			var gcp = repo.ProcessDefinitions.Single(d => d.Name == "Test Group");
			Assert.AreEqual("Test Group", gcp.Name);
			Assert.AreEqual("GroupAlias", gcp.TypeAlias);
			Assert.AreEqual("Testing", gcp.Description);
			Assert.AreEqual(typeof(GroupCP), gcp.ResolvedType);

			var fcp = repo.ProcessDefinitions.Single(d => d.Name == "Test Foo");
			Assert.AreEqual("Test Foo", fcp.Name);
			Assert.AreEqual("FooAlias", fcp.TypeAlias);
			Assert.AreEqual("Testing Foo", fcp.Description);
			Assert.AreEqual(typeof(Foo), fcp.ResolvedType);

			var bcp = repo.ProcessDefinitions.Single(d => d.Name == "Test Bar");
			Assert.AreEqual("Test Bar", bcp.Name);
			Assert.AreEqual("BarAlias", bcp.TypeAlias);
			Assert.AreEqual("Testing Bar", bcp.Description);
			Assert.AreEqual(typeof(Bar), bcp.ResolvedType);
		}

		[Test]
		public void CanSave()
		{
			var repo = new CartoProcessRepo();

			var knownTypes = GetKnownTypes();
			var xml1 = GetTestXmlConfig();

			using (var reader = new StringReader(xml1))
			{
				repo.Load(reader, knownTypes);
			}

			var buffer = new StringBuilder();

			using (var writer = new StringWriter(buffer))
			{
				repo.Save(writer);
			}

			var xml2 = buffer.ToString();

			var doc1 = XDocument.Parse(xml1);
			var doc2 = XDocument.Parse(xml2);

			// compare only the Groups and Processes elements
			// don't care about the XML declaration and ignore
			// the Types element, which legally discards info
			// such as the assembly attribute

		Assert.True(XNode.DeepEquals(doc1.Root?.Element("Groups"), doc2.Root?.Element("Groups")));
		Assert.True(XNode.DeepEquals(doc1.Root?.Element("Processes"), doc2.Root?.Element("Processes")));
		}

		[Test]
		public void CanUpdateProcess()
		{
			var repo = new CartoProcessRepo();

			var knownTypes = GetKnownTypes();
			var xml = GetTestXmlConfig();

			using (var reader = new StringReader(xml))
			{
				repo.Load(reader, knownTypes);
			}

			// Update existing CP "Test Foo"

			var config = CartoProcessConfig.Parse("Name=Test Foo\nTypeAlias=FooAlias\n" +
			                                      "A=one\nC=three\nDescription=Updated");
			var newType = typeof(NewType);

			repo.UpdateProcess(config, newType);

			Assert.AreEqual(3, repo.ProcessDefinitions.Count);

			var gcp = repo.ProcessDefinitions.Single(d => d.Name == "Test Group");
			Assert.AreEqual(typeof(GroupCP), gcp.ResolvedType);

			var fcp = repo.ProcessDefinitions.Single(d => d.Name == "Test Foo");
			Assert.AreEqual("Test Foo", fcp.Name);
			Assert.AreEqual("NewType", fcp.TypeAlias);
			Assert.AreEqual("Updated", fcp.Description);
			Assert.AreEqual(typeof(NewType), fcp.ResolvedType);
			Assert.AreEqual("one", fcp.Config.GetString("A"));
			Assert.IsNull(fcp.Config.GetString("B", null));
			Assert.AreEqual("three", fcp.Config.GetString("C"));
			Assert.AreEqual(2, fcp.Config.Count);

			var bcp = repo.ProcessDefinitions.Single(d => d.Name == "Test Bar");
			Assert.AreEqual(typeof(Bar), bcp.ResolvedType);

			// Insert ("update") non-existing CP "Test New"

			config = CartoProcessConfig.Parse("Name=Test New\nFoo=Bar");

			repo.UpdateProcess(config, typeof(NewType));

			Assert.AreEqual(4, repo.ProcessDefinitions.Count);

			var ncp = repo.ProcessDefinitions.Single(d => d.Name == "Test New");
			Assert.AreEqual("Test New", ncp.Name);
			Assert.IsNull(ncp.Description);
			Assert.AreEqual("NewType", ncp.TypeAlias);
			Assert.AreEqual(typeof(NewType), ncp.ResolvedType);
			Assert.AreEqual("Bar", ncp.Config.GetString("Foo"));
			Assert.AreEqual(1, ncp.Config.Count);
		}

		[Test]
		public void CanRemoveProcess()
		{
			var repo = new CartoProcessRepo();

			var knownTypes = GetKnownTypes();
			var xml = GetTestXmlConfig();

			using (var reader = new StringReader(xml))
			{
				repo.Load(reader, knownTypes);
			}

			Assert.False(repo.RemoveProcess("NoSuchProcess"));
			Assert.AreEqual(3, repo.ProcessDefinitions.Count);

			Assert.True(repo.RemoveProcess("Test Foo"));
			Assert.AreEqual(2, repo.ProcessDefinitions.Count);

			Assert.True(repo.RemoveProcess("Test Bar"));
			Assert.AreEqual(1, repo.ProcessDefinitions.Count);

			Assert.True(repo.RemoveProcess("Test Group"));
			Assert.AreEqual(0, repo.ProcessDefinitions.Count);
		}

		private class Foo { }

		private class Bar { }

		private class GroupCP : IGroupCartoProcess { }

		private class NewType { }

		private static Type[] GetKnownTypes()
		{
			return new[] { typeof(Foo), typeof(Bar), typeof(GroupCP) };
		}

		private static string GetTestXmlConfig()
		{
			return @"<?xml version=""1.0""?>
<CartoProcesses>
  <Groups>
    <ProcessGroup name=""Test Group"" description=""Testing"">
      <GroupProcessTypeReference name=""GroupAlias"" />
      <Processes>
        <Process name=""Foo"" />
        <Process name=""Bar"" />
      </Processes>
    </ProcessGroup>
  </Groups>
  <Processes>
    <Process name=""Test Foo"" description=""Testing Foo"">
      <TypeReference name=""FooAlias"" />
      <Parameters>
        <Parameter name=""A"" value=""1"" />
        <Parameter name=""B"" value=""two"" />
      </Parameters>
    </Process>
    <Process name=""Test Bar"" description=""Testing Bar"">
      <TypeReference name=""BarAlias"" />
      <Parameters>
        <Parameter name=""A"" value=""one"" />
        <Parameter name=""B"" value=""2"" />
      </Parameters>
    </Process>
  </Processes>
  <Types>
    <ProcessType name=""FooAlias"">
      <ClassDescriptor type=""Name.Space.Foo"" />
    </ProcessType>
    <ProcessType name=""BarAlias"">
      <ClassDescriptor type=""Name.Space.Bar"" assembly=""ignored"" />
    </ProcessType>
	<ProcessType name=""GroupAlias"">
      <ClassDescriptor type=""GroupCP"" />
    </ProcessType>
  </Types>
</CartoProcesses>
";
		}
	}
}
