using System;
using NUnit.Framework;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.Test.Text
{
	[TestFixture]
	public class TextMatchingTest
	{
		[Test]
		public void CanWildMatch()
		{
			Assert.IsTrue(WildMatch("abc", "abc"));
			Assert.IsFalse(WildMatch("abc", "abz"));

			Assert.IsTrue(WildMatch("*.txt", "file.txt"));
			Assert.IsFalse(WildMatch("*.txt", "file.doc"));
			Assert.IsTrue(WildMatch("file-?.dat", "file-a.dat"));
			Assert.IsFalse(WildMatch("file-?.dat", "file-zz.dat"));

			Assert.IsTrue(WildMatch("", ""));
			Assert.IsTrue(WildMatch("*", ""));
			Assert.IsTrue(WildMatch("**", ""));
			Assert.IsFalse(WildMatch("?", ""));

			Assert.IsTrue(WildMatch("?", "x"));
			Assert.IsFalse(WildMatch("?", "xx"));
			Assert.IsTrue(WildMatch("*", "x"));
			Assert.IsTrue(WildMatch("*", "xx"));

			Assert.IsFalse(WildMatch("*?", ""));
			Assert.IsTrue(WildMatch("*?", "x"));
			Assert.IsTrue(WildMatch("*?", "xx"));
			Assert.IsTrue(WildMatch("*?", "xxx"));

			Assert.IsFalse(WildMatch("?*", ""));
			Assert.IsTrue(WildMatch("?*", "x"));
			Assert.IsTrue(WildMatch("?*", "xxx"));

			Assert.IsTrue(WildMatch("x**x", "xx"));
			Assert.IsTrue(WildMatch("x**x", "xAx"));
			Assert.IsTrue(WildMatch("x**x", "xAAx"));
			Assert.IsFalse(WildMatch("x**x", "xAAx."));

			Assert.IsFalse(WildMatch("*x*", ""));
			Assert.IsTrue(WildMatch("*x*", "x"));
			Assert.IsTrue(WildMatch("*x*", "xx"));
			Assert.IsTrue(WildMatch("*x*", "Zxx"));
			Assert.IsTrue(WildMatch("*x*", "xZx"));
			Assert.IsTrue(WildMatch("*x*", "xxZ"));
			Assert.IsFalse(WildMatch("*x*", "ZZ"));

			Assert.IsFalse(WildMatch("a*x*b", "ab"));
			Assert.IsTrue(WildMatch("a*x*b", "abxbab"));
			Assert.IsTrue(WildMatch("s*no*", "salentino"));
			Assert.IsTrue(WildMatch("*sip*", "mississippi"));
			Assert.IsTrue(WildMatch("-*-*-*-", "-foo-bar-baz-"));

			// Exercise case folding:
			Assert.IsTrue(WildMatch("abc", "aBc", true));
			Assert.IsFalse(WildMatch("abc", "aBc"));
			Assert.IsTrue(WildMatch("*C", "acbc", true));
			Assert.IsFalse(WildMatch("*C", "acbc"));
		}

		private static bool WildMatch(string pattern, string text, bool ignoreCase = false)
		{
			return TextMatching.WildMatch(pattern, text, ignoreCase);
		}

		[Test]
		public void CanPathMatch()
		{
			Assert.IsTrue(PathMatch("", ""));
			Assert.IsTrue(PathMatch("foo", "foo"));
			Assert.IsTrue(PathMatch("foo/bar", "foo/bar"));
			Assert.IsFalse(PathMatch("foo/baz", "foo/bar"));
			Assert.IsFalse(PathMatch("foo/bar/baz", "foo/bar"));
			Assert.IsTrue(PathMatch("foo/**/bar", "foo/bar"));
			Assert.IsTrue(PathMatch("foo/**/bar", "foo/x/bar"));
			Assert.IsTrue(PathMatch("foo/**/bar", "foo/x/y/z/bar"));
			Assert.IsFalse(PathMatch("foo/**/bar", "foo/x/bar/y"));

			Assert.IsTrue(PathMatch("**/x/y/**", "x/y"));
			Assert.IsTrue(PathMatch("**/x/y/**", "foo/x/y"));
			Assert.IsTrue(PathMatch("**/x/y/**", "x/y/bar"));
			Assert.IsTrue(PathMatch("**/x/y/**", "foo/x/y/bar"));
			Assert.IsTrue(PathMatch("**/x/y/**", "foo/bar/x/y/z/z/z"));
			Assert.IsFalse(PathMatch("**/x/y/**", "foo/bar/x/Y/z/z/z"));

			Assert.IsTrue(PathMatch("foo/**/*", "foo/bar"));
			Assert.IsTrue(PathMatch("foo/**/*", "foo/x/y/baz"));
			Assert.IsTrue(PathMatch("foo/**", "foo/bar"));
			Assert.IsTrue(PathMatch("foo/**", "foo/x/y/baz"));

			Assert.IsTrue(PathMatch("**/ba*", "bar"));
			Assert.IsTrue(PathMatch("**/ba*", "x/y/z/bazaar"));
			Assert.IsTrue(PathMatch("**/ba*/", "x/y/z/bazaar/"));
			Assert.IsFalse(PathMatch("**/ba*", "x/y/z/foo"));

			// And a few assertions for the convenience wrapper:

			Assert.IsTrue(TextMatching.PathMatch("foo/**/bar", "foo/bar"));
			Assert.IsTrue(TextMatching.PathMatch("foo/**/bar", "foo/x/y/bar"));
			Assert.IsFalse(TextMatching.PathMatch("foo/**/bar/", "foo/x/y/bar"));
			Assert.IsTrue(TextMatching.PathMatch("foo/**/bar/", "foo/x/y/bar/"));
			Assert.IsFalse(TextMatching.PathMatch("", ""));

		}

		private static bool PathMatch(string pattern, string path, char separator = '/')
		{
			var patterns = pattern.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
			var names = path.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
			return TextMatching.PathMatch(patterns, names);
		}

		[Test]
		public void CanSimpleMatch()
		{
			// Literal matches (no stars):
			Assert.IsTrue(SimpleMatch("abc", "abc"));
			Assert.IsTrue(SimpleMatch("", ""));
			Assert.IsFalse(SimpleMatch("abc", "abd"));

			// Wildcard matches (with stars):
			Assert.IsTrue(SimpleMatch("a*x", "ax"));
			Assert.IsTrue(SimpleMatch("a*x", "abx"));
			Assert.IsTrue(SimpleMatch("a*x", "abcx"));
			Assert.IsFalse(SimpleMatch("a*x", "abxx"));
			Assert.IsTrue(SimpleMatch("*ba*", "Barbara"));
			Assert.IsTrue(SimpleMatch("*ba*", "Barba"));
			Assert.IsTrue(SimpleMatch("*ba*", "bazaar"));
			Assert.IsTrue(SimpleMatch("*ba*", "abacus"));
			Assert.IsTrue(SimpleMatch("*ba*", "ba"));

			// Multiple stars (2nd star is literal):
			Assert.IsTrue(SimpleMatch("**", "*"));
			Assert.IsTrue(SimpleMatch("***", "x*x"));

			// No pattern matches a null value:
			Assert.IsFalse(SimpleMatch("", null));
			Assert.IsFalse(SimpleMatch("*", null));

			// The pattern is a required parameter:
			Assert.Throws<ArgumentNullException>(() => SimpleMatch(null, "any"));

			// Case folding:
			Assert.IsFalse(SimpleMatch("a", "A"));
			Assert.IsTrue(SimpleMatch("a", "A", true));
			Assert.IsTrue(SimpleMatch("A", "a", true));
			Assert.IsFalse(SimpleMatch("*a", "xA"));
			Assert.IsTrue(SimpleMatch("*a", "xA", true));
			Assert.IsTrue(SimpleMatch("*A", "xa", true));
		}

		private static bool SimpleMatch(string pattern, string text, bool ignoreCase = false)
		{
			return TextMatching.SimpleMatch(pattern, text, ignoreCase);
		}
	}
}
