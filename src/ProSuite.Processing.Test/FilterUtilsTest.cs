using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test;

[TestFixture]
public class FilterUtilsTest
{
	[Test]
	public void CanCamelRide()
	{
		var r0 = FilterUtils.CamelRide(string.Empty).ToList();
		Assert.AreEqual(0, r0.Count);

		var r1 = FilterUtils.CamelRide("Foo").ToList();
		Assert.AreEqual(1, r1.Count);
		Assert.AreEqual("Foo", r1[0]);

		var r2 = FilterUtils.CamelRide("FooBar").ToList();
		Assert.AreEqual(3, r2.Count);
		Assert.AreEqual("Foo", r2[0]);
		Assert.AreEqual("FooBar", r2[1]);
		Assert.AreEqual("Bar", r2[2]);

		var r3 = FilterUtils.CamelRide("FooBarBaz").ToList();
		Assert.AreEqual(6, r3.Count);
		Assert.AreEqual("Foo", r3[0]);
		Assert.AreEqual("FooBar", r3[1]);
		Assert.AreEqual("FooBarBaz", r3[2]);
		Assert.AreEqual("Bar", r3[3]);
		Assert.AreEqual("BarBaz", r3[4]);
		Assert.AreEqual("Baz", r3[5]);
	}

	[Test]
	public void CanMakeTags()
	{
		var tags0 = FilterUtils.MakeTags(null).ToList();
		Assert.AreEqual(0, tags0.Count);

		var tags1 = FilterUtils.MakeTags("Hello").ToList();
		Assert.AreEqual(1, tags1.Count);
		Assert.AreEqual("hello", tags1[0]);

		var tags2 = FilterUtils.MakeTags("HelloWorld").ToList();
		Assert.AreEqual(3, tags2.Count);
		Assert.AreEqual("hello", tags2[0]);
		Assert.AreEqual("helloworld", tags2[1]);
		Assert.AreEqual("world", tags2[2]);

		var tags3 = FilterUtils.MakeTags("  fooBarBaz-aar - Quux_ly  ").ToList();
		Assert.AreEqual(9, tags3.Count);
		Assert.AreEqual("foo", tags3[0]);
		Assert.AreEqual("foobar", tags3[1]);
		Assert.AreEqual("foobarbaz", tags3[2]);
		Assert.AreEqual("bar", tags3[3]);
		Assert.AreEqual("barbaz", tags3[4]);
		Assert.AreEqual("baz", tags3[5]);
		Assert.AreEqual("aar", tags3[6]);
		Assert.AreEqual("quux", tags3[7]);
		Assert.AreEqual("ly", tags3[8]);

		var tags4 = FilterUtils.MakeTags("FooBar: -Quux- ").ToList();
		Assert.AreEqual(4, tags4.Count);
		Assert.AreEqual("foo", tags4[0]);
		Assert.AreEqual("foobar", tags4[1]);
		Assert.AreEqual("bar", tags4[2]);
		Assert.AreEqual("quux", tags4[3]);

		var tags5 = FilterUtils.MakeTags("AlignMarkersCP", "type").ToList();
		Assert.AreEqual(6, tags5.Count);
		Assert.AreEqual("type:align", tags5[0]);
		Assert.AreEqual("type:alignmarkers", tags5[1]);
		Assert.AreEqual("type:alignmarkerscp", tags5[2]);
		Assert.AreEqual("type:markers", tags5[3]);
		Assert.AreEqual("type:markerscp", tags5[4]);
		Assert.AreEqual("type:cp", tags5[5]);
	}

	[Test]
	public void CanParseFilter()
	{
		Assert.IsEmpty(FilterUtils.ParseFilter(null));
		Assert.IsEmpty(FilterUtils.ParseFilter(string.Empty));
		Assert.IsEmpty(FilterUtils.ParseFilter(" \t\n\r\v "));

		var toks1 = FilterUtils.ParseFilter("Foo -Bar +Quux").ToList();
		Assert.AreEqual(3, toks1.Count);
		Assert.AreEqual("foo", toks1[0]);
		Assert.AreEqual("-bar", toks1[1]);
		Assert.AreEqual("+quux", toks1[2]);

		var toks2 = FilterUtils.ParseFilter(" FooBar: -Quux! ").ToList();
		Assert.AreEqual(2, toks2.Count);
		Assert.AreEqual("foobar", toks2[0]);
		Assert.AreEqual("-quux", toks2[1]);

		var toks3 = FilterUtils.ParseFilter("Foo type:Bar -type:Baz-:Quux:").ToList();
		Assert.AreEqual(4, toks3.Count);
		Assert.AreEqual("foo", toks3[0]);
		Assert.AreEqual("type:bar", toks3[1]);
		Assert.AreEqual("-type:baz", toks3[2]);
		Assert.AreEqual("quux", toks3[3]);

		var toks4 = FilterUtils.ParseFilter("type:AlignMarkers").ToList();
		Assert.AreEqual(1, toks4.Count);
		Assert.AreEqual("type:alignmarkers", toks4[0]);
	}

	[Test]
	public void CanMatchFilter()
	{
		var item0 = new Tagged(string.Empty);
		Match(item0, string.Empty);
		Match(item0, "foo");

		var item1 = new Tagged("Foo Bar Baz");
		Assert.True(Match(item1, string.Empty));
		Assert.True(Match(item1, "foo"));
		Assert.True(Match(item1, "Bar"));
		Assert.True(Match(item1, "Foo Bar Baz"));
		Assert.False(Match(item1, "Foo Bar Bazaar"));
		Assert.False(Match(item1, "Foo Bar -Baz"));
		Assert.True(Match(item1, "+Foo +Bar -Quux"));
	}

	private static bool Match(ITagged item, string filterText)
	{
		var filter = FilterUtils.ParseFilter(filterText).ToList();
		return FilterUtils.MatchFilter(item, filter);
	}

	private class Tagged : ITagged
	{
		public ICollection<string> Tags { get; }

		public Tagged(string text)
		{
			Tags = FilterUtils.MakeTags(text).ToList();
		}
	}
}
