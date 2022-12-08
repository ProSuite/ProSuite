using System.Linq;
using NUnit.Framework;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class CartoProcessConfigTest
	{
		[Test]
		public void CanParseConfigText()
		{
			var c1 = CartoProcessConfig.Parse(string.Empty);
			Assert.AreEqual(0, c1.Count);

			var c2 = CartoProcessConfig.Parse("a=b\nc=d\r\ne=f\ng=h");
			Assert.AreEqual(4, c2.Count);
			Assert.AreEqual("b", c2.GetString("a"));
			Assert.AreEqual("d", c2.GetString("c"));
			Assert.AreEqual("f", c2.GetString("e"));
			Assert.AreEqual("h", c2.GetString("g"));

			Assert.IsNull(c2.GetString("x", null));
			Assert.AreEqual("default", c2.GetString("x", "default"));

			var c3 = CartoProcessConfig.Parse("InputDataset = MyPolylines\r\nMaxAngle = 42.1\r\n");
			Assert.AreEqual(2, c3.Count);
			Assert.AreEqual("MyPolylines", c3.GetString("InputDataset"));
			Assert.AreEqual(42.1, c3.GetValue<double>("MaxAngle"));
		}

		[Test]
		public void CanGetEmptyValue()
		{
			var config = CartoProcessConfig.Parse("foo=\n bar = \t \n");

			Assert.AreEqual(2, config.Count);

			Assert.AreEqual(string.Empty, config.GetString("foo"));
			Assert.AreEqual("default", config.GetValue("foo", "default"));

			Assert.AreEqual(string.Empty, config.GetString("bar"));
			Assert.AreEqual("default", config.GetValue("bar", "default"));
		}

		[Test]
		public void CanGetMultiValue()
		{
			var config = CartoProcessConfig.Parse(
				"foo=a\nbar=11\n bar = 22 \n baz = 'str' \n bar = 33\n");

			Assert.AreEqual(5, config.Count);

			var values = config.GetValues<int>("bar").ToList();
			Assert.AreEqual(3, values.Count);
			Assert.AreEqual(11, values[0]);
			Assert.AreEqual(22, values[1]);
			Assert.AreEqual(33, values[2]);
		}

		[Test]
		public void CanGetQuotedValue()
		{
			var config = CartoProcessConfig.Parse("a='foo'\nb=\"bar\"\nc = a 'b' c \"d\" e \n d = \"a  b\" \t c\t \t\n");

			Assert.AreEqual("'foo'", config.GetString("a"));
			Assert.AreEqual("\"bar\"", config.GetString("b"));
			Assert.AreEqual("a 'b' c \"d\" e", config.GetString("c"));
			Assert.AreEqual("\"a  b\" c", config.GetString("d"));
		}

		[Test]
		public void CanGetAllNames()
		{
			var config = CartoProcessConfig.Parse("foo=1\nbar=2\nbaz=3\nfoo=duplicate\n");

			Assert.AreEqual(4, config.Count);
			var names = config.GetAllNames().ToArray();
			Assert.AreEqual(3, names.Length);
			Assert.AreEqual("foo", names[0]);
			Assert.AreEqual("bar", names[1]);
			Assert.AreEqual("baz", names[2]);
		}

		[Test]
		public void CanParseProcessXml()
		{
			const string xml = @"<Process name=""Align Buildings"" description=""to nearest road or railroad"">
  <ModelReference name=""ABC"" />
  <TypeReference name=""AlignMarkers"" />
  <Parameters>
    <Parameter name=""InputDataset"" value=""FooPoints; TYPEID IN (1,3,5)"" />
    <Parameter name=""ReferenceDataset1"" value=""Roads"" />
    <Parameter name=""ReferenceDataset2"" value=""Rails"" />
    <Parameter name=""SearchDistance"" value=""50"" />
    <Parameter name=""MarkerAttributes"" value=""ANGLE = NormalAngle"" />
    <Parameter name=""MarkerAttributes"" value=""OPERATOR = 'Jones'"" />
  </Parameters>
 </Process>";

			var config = CartoProcessConfig.Parse(xml);

			Assert.NotNull(config);
			Assert.AreEqual("Align Buildings", config.Name);
			Assert.AreEqual("to nearest road or railroad", config.Description);
			Assert.AreEqual(6, config.Count);
			Assert.AreEqual("FooPoints; TYPEID IN (1,3,5)", config.GetString("InputDataset"));
			// ReferenceDataset{1,2} are renamed to new convention:
			Assert.AreEqual("RoadsRails", string.Join("", config.GetValues("ReferenceDatasets")));
			Assert.IsNull(config.GetString("ReferenceDataset1", null));
			Assert.Throws<CartoConfigException>(() => config.GetString("ReferenceDataset2"));
			// MarkerAttributes is multivalued; quotes preserved:
			Assert.AreEqual("ANGLE = NormalAngle; OPERATOR = 'Jones'", string.Join("; ", config.GetValues("MarkerAttributes")));
		}

		[Test]
		public void CanParseProcessGroupXml()
		{
			const string xml = @"<ProcessGroup name=""Create Markers"">
  <AssociatedGroupProcessTypeReference name=""GroupGdbProcess"" />
    <Processes>
      <Process name=""Foo"" />
      <Process name=""Bar"" />
      <Process name=""Baz"" />
    </Processes>
  </ProcessGroup>";

			var config = CartoProcessConfig.Parse(xml);

			Assert.NotNull(config);
			Assert.AreEqual("Create Markers", config.Name);
			Assert.IsNull(config.Description);
			Assert.AreEqual(3, config.Count);
			Assert.AreEqual("FooBarBaz", string.Join("", config.GetValues("Process")));
		}
	}
}
