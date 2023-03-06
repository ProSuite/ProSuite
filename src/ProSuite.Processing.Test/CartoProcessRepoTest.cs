using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using ProSuite.Processing.Domain;

namespace ProSuite.Processing.Test;

[TestFixture]
public class CartoProcessRepoTest
{
	[Test]
	public void CanLoad()
	{
		var knownTypes = new[] { typeof(Foo), typeof(Bar), typeof(GroupCP) };

		var xml = GetTestXmlConfig();
		using var reader = new StringReader(xml);
		var repo = new CartoProcessRepo();
		repo.Load(reader, knownTypes);

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
		var knownTypes = new[] { typeof(Foo), typeof(Bar), typeof(GroupCP) };

		var xml = GetTestXmlConfig();
		using var reader = new StringReader(xml);

		var repo = new CartoProcessRepo();
		repo.Load(reader, knownTypes);

		var buffer = new StringBuilder();
		using (var writer = new StringWriter(buffer))
		{
			repo.Save(writer);
		}

		var xml2 = buffer.ToString();

		var doc1 = XDocument.Parse(xml);
		var doc2 = XDocument.Parse(xml2);

		// compare only the Groups and Processes elements
		// don't care about the XML declaration and ignore
		// the Types element, which legally discards info
		// such as the assembly attribute

		Assert.True(XNode.DeepEquals(doc1.Root?.Element("Groups"), doc2.Root?.Element("Groups")));
		Assert.True(XNode.DeepEquals(doc1.Root?.Element("Processes"), doc2.Root?.Element("Processes")));
	}

	private class Foo { }

	private class Bar { }

	private class GroupCP : IGroupCartoProcess { }

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
