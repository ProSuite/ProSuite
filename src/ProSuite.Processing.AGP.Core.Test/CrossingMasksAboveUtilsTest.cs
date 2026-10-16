using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ProSuite.Processing.AGP.Core.Utils;

namespace ProSuite.Processing.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class CrossingMasksAboveUtilsTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		//CoreHostProxy.Initialize();
	}

	[Test]
	public void CanParseAboveFeatures()
	{
		var dict0 = CrossingMasksAboveUtils.ParseAboveFeatures(null);
		Assert.IsNull(dict0); // null stays null

		var dict1 = CrossingMasksAboveUtils.ParseAboveFeatures(string.Empty);
		Assert.IsNotNull(dict1);
		Assert.IsEmpty(dict1);

		var dict2 = CrossingMasksAboveUtils.ParseAboveFeatures("FOO,1,2;BAR,3");
		Assert.IsNotNull(dict2);
		Assert.AreEqual(2, dict2.Count);
		Assert.True(dict2.ContainsKey("foo")); // case ignored
		Assert.AreEqual(2, dict2["foo"].Count);
		Assert.True(dict2.ContainsKey("bar")); // case ignored
		Assert.AreEqual(1, dict2["bar"].Count);
		Assert.AreEqual(3, dict2["bar"][0]);

		// white space is ignored, trailing semicolon is ignored
		var dict3 = CrossingMasksAboveUtils.ParseAboveFeatures(" FOO , 1 , 2 ; BAR , 3 ; ");
		Assert.IsNotNull(dict3);
		Assert.AreEqual(2, dict3.Count);
		Assert.True(dict3.ContainsKey("foo")); // case ignored
		Assert.AreEqual(2, dict3["foo"].Count);
		Assert.True(dict3.ContainsKey("bar")); // case ignored
		Assert.AreEqual(1, dict3["bar"].Count);
		Assert.AreEqual(3, dict3["bar"][0]);

		var dict4 = CrossingMasksAboveUtils.ParseAboveFeatures("FOO;BAR;BAZ");
		Assert.IsNotNull(dict4);
		Assert.IsEmpty(dict4); // no OIDs provided, so no entries in the dictionary

		var dict5 = CrossingMasksAboveUtils.ParseAboveFeatures("FOO_BAR,9223372036854775807");
		Assert.IsNotNull(dict5); // ObjectIDs are 64bit longs, names may contain underscores
		Assert.AreEqual("FOO_BAR", dict5.Single().Key);
		Assert.AreEqual(long.MaxValue, dict5.Single().Value.Single());

		var dict6 = CrossingMasksAboveUtils.ParseAboveFeatures("FOO,3;Foo,2;foo,1");
		Assert.NotNull(dict6); // duplicates are merged (and oids sorted)
		Assert.True(new long[] { 1, 2, 3 }.SequenceEqual(dict6.Single().Value));
	}

	[Test]
	public void CanFormatAboveFeatures()
	{
		var text1 = CrossingMasksAboveUtils.FormatAboveFeatures(null);
		Assert.IsNull(text1); // null stays null

		var empty = new Dictionary<string, List<long>>();
		var text2 = CrossingMasksAboveUtils.FormatAboveFeatures(empty);
		Assert.IsEmpty(text2);

		var dict3 = new Dictionary<string, List<long>>
		            {
			            { "A", new List<long> { 1, 2, 3 } },
			            { "B", new List<long> { 4, 5 } }
		            };
		var text3 = CrossingMasksAboveUtils.FormatAboveFeatures(dict3);
		Assert.AreEqual("A,1,2,3;B,4,5", text3);

		var dict4 = new Dictionary<string, List<long>>()
		            {
			            { "FOO", new List<long> { int.MaxValue, long.MaxValue } },
			            { "BAR", new List<long>() },
			            { "BAZ", new List<long> { 0 } }
		            };
		var text4 = CrossingMasksAboveUtils.FormatAboveFeatures(dict4);
		Assert.AreEqual("BAZ,0;FOO,2147483647,9223372036854775807", text4);

		// output is sorted to canonical order (but names keep original case)
		var dict5 = new Dictionary<string, List<long>>
		            {
			            { "FOO", new List<long> { 42, 7, 12 } },
			            { "Bar", new List<long> { 123, 0 } }
		            };
		var text5 = CrossingMasksAboveUtils.FormatAboveFeatures(dict5);
		Assert.AreEqual("Bar,0,123;FOO,7,12,42", text5);
	}
}
