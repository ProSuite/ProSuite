using System;
using NUnit.Framework;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.Test.Text
{
	[TestFixture]
	public class TaggingUtilsTest
	{
		[Test]
		public void CanAddTag()
		{
			Assert.Null(TaggingUtils.AddTag(null, null));
			Assert.AreEqual("foo", TaggingUtils.AddTag(null, "foo"));
			Assert.AreEqual("foo", TaggingUtils.AddTag(string.Empty, "foo"));
			Assert.AreEqual("foo", TaggingUtils.AddTag("foo", null));
			Assert.AreEqual("foo", TaggingUtils.AddTag("foo", string.Empty));
			Assert.AreEqual("foo,bar,new", TaggingUtils.AddTag("foo,bar", "new"));
			Assert.AreEqual("foo,bar", TaggingUtils.AddTag("foo,bar", "foo"));
		}

		[Test]
		public void CanAddTags()
		{
			Assert.Null(TaggingUtils.AddTags(null, null));
			Assert.AreEqual("foo", TaggingUtils.AddTags(null, "foo"));
			Assert.AreEqual("foo,bar", TaggingUtils.AddTags(null, "foo", "bar"));
			Assert.AreEqual("foo,bar", TaggingUtils.AddTags("foo", "foo", "bar"));
			Assert.AreEqual("foo,bar", TaggingUtils.AddTags("foo,bar", "foo", "bar"));
			Assert.AreEqual("foo,bar,new", TaggingUtils.AddTags("foo,bar", "bar", "new"));
			Assert.AreEqual("foo,bar", TaggingUtils.AddTags("foo,bar", null));
			Assert.AreEqual("foo,bar", TaggingUtils.AddTags("foo,bar"));
		}

		[Test]
		public void CanHasTag()
		{
			Assert.False(TaggingUtils.HasTag(null, null));
			Assert.False(TaggingUtils.HasTag(null, "foo"));
			Assert.False(TaggingUtils.HasTag("foo,,bar", null));
			Assert.False(TaggingUtils.HasTag("foo,,bar", string.Empty));
			Assert.True(TaggingUtils.HasTag("start,mid,end", "start"));
			Assert.True(TaggingUtils.HasTag("start,mid,end", "mid"));
			Assert.True(TaggingUtils.HasTag("start,mid,end", "end"));
			Assert.False(TaggingUtils.HasTag("start,mid,end", "NoSuchTag"));
			Assert.False(TaggingUtils.HasTag("start,mid,end", "art"));

			// HasTag is separator-agnostic:
			Assert.True(TaggingUtils.HasTag("start; mid, end", "mid"));
			Assert.True(TaggingUtils.HasTag("start  mid  end", "mid"));
			Assert.True(TaggingUtils.HasTag("start, mid; end", "mid"));
		}

		[Test]
		public void CanHasTags()
		{
			Assert.False(TaggingUtils.HasTags(null, null));
			Assert.False(TaggingUtils.HasTags(null, ""));
			Assert.False(TaggingUtils.HasTags(null, "foo"));
			Assert.False(TaggingUtils.HasTags(null, "foo", "bar"));

			Assert.False(TaggingUtils.HasTags("foo", null));
			Assert.False(TaggingUtils.HasTags("foo", ""));
			Assert.True(TaggingUtils.HasTags("foo", "foo"));
			Assert.False(TaggingUtils.HasTags("foo", "foo", "bar"));

			Assert.False(TaggingUtils.HasTags("foo,bar", null));
			Assert.False(TaggingUtils.HasTags("foo,bar", ""));
			Assert.True(TaggingUtils.HasTags("foo,bar", "foo"));
			Assert.True(TaggingUtils.HasTags("foo,bar", "foo", "bar"));

			// HasTags is separator-agnostic:
			Assert.True(TaggingUtils.HasTags(" foo ; bar ; baz", "foo", "bar"));
			Assert.True(TaggingUtils.HasTags(" foo   bar   baz", "bar", "baz"));
		}

		[Test]
		public void CanSplitTags()
		{
			AssertTags(TaggingUtils.SplitTags(null));
			AssertTags(TaggingUtils.SplitTags(string.Empty));
			AssertTags(TaggingUtils.SplitTags(" \t"));
			AssertTags(TaggingUtils.SplitTags("foo,bar;baz"), "foo", "bar", "baz");
			AssertTags(TaggingUtils.SplitTags(" , foo ,, bar ;; baz ; "), "foo", "bar", "baz");
		}

		private static void AssertTags(string[] actual, params string[] expected)
		{
			if (actual == null && expected == null) return;
			if (actual == null || expected == null) Assert.Fail();
			if (actual.Length != expected.Length) Assert.Fail("Tag count differs");
			for (int i = 0; i < actual.Length; i++)
			{
				if (! string.Equals(expected[i], actual[i], StringComparison.Ordinal))
					Assert.Fail();
			}
		}
	}
}
