using NUnit.Framework;
using ProSuite.Commons.Collections;
using System;

namespace ProSuite.Commons.Test.Collections
{
	[TestFixture]
	public class NumericStringComparerTest
	{
		[Test]
		public void CanCompareStrings()
		{
			// NOTE: .net 5 changed the string.Compare behaviour compared to .NET Framework:
			// The actual numeric distance between the characters is returned.
			// For backward-compatibility, use StringComparison.CurrentCultureIgnoreCase
			var currentCulture = new NumericStringComparer(StringComparison.CurrentCultureIgnoreCase);

			AssertBasicStringsNonNumeric(currentCulture);

			Assert.AreEqual(-1, currentCulture.Compare(" ", "a"));
			Assert.AreEqual(1, currentCulture.Compare("a", " "));

			// Default: Compare ordinal
			var ordinal = new NumericStringComparer();

			AssertBasicStringsNonNumeric(ordinal);

			int d = ordinal.Compare(" ", "a");
			// d is -1 in .NET Framework, -33 in .net 6
			Assert.Less(d, 0);

			d = ordinal.Compare("a", " ");
			// d is 1 in .NET Framework, 33 in .net 6
			Assert.Greater(d, 0);
		}

		private static void AssertBasicStringsNonNumeric(NumericStringComparer comparer)
		{
			Assert.AreEqual(0, comparer.Compare(null, null));
			Assert.AreEqual(0, comparer.Compare(string.Empty, string.Empty));
			Assert.AreEqual(0, comparer.Compare("", ""));
			Assert.AreEqual(0, comparer.Compare(" ", " "));
			Assert.AreEqual(0, comparer.Compare("a", "a"));
			Assert.AreEqual(0, comparer.Compare("a", "A"));

			Assert.AreEqual(-1, comparer.Compare(null, "a"));
			Assert.AreEqual(-1, comparer.Compare(string.Empty, "a"));
			Assert.AreEqual(-1, comparer.Compare("", "a"));
			
			Assert.AreEqual(-1, comparer.Compare("a", "b"));
			Assert.AreEqual(-1, comparer.Compare("A", "b"));

			Assert.AreEqual(1, comparer.Compare("a", null));
			Assert.AreEqual(1, comparer.Compare("a", string.Empty));
			Assert.AreEqual(1, comparer.Compare("a", ""));
			
			Assert.AreEqual(1, comparer.Compare("b", "a"));
			Assert.AreEqual(1, comparer.Compare("b", "A"));
		}

		[Test]
		public void CanCompareNumbers()
		{
			var comparer = new NumericStringComparer();

			Assert.AreEqual(0, comparer.Compare("1", "1"));
			Assert.AreEqual(0, comparer.Compare("1.0", "1.0"));

			Assert.AreEqual(-1, comparer.Compare("1", "2"));
			Assert.AreEqual(-1, comparer.Compare("1.0", "1.1"));
			Assert.AreEqual(-1, comparer.Compare("1.1", "2"));
			Assert.AreEqual(-1, comparer.Compare("2", "10.0"));

			Assert.AreEqual(1, comparer.Compare("2", "1"));
			Assert.AreEqual(1, comparer.Compare("2", "1.0"));
			Assert.AreEqual(1, comparer.Compare("2", "1.1"));
			Assert.AreEqual(1, comparer.Compare("10.0", "2"));
		}

		[Test]
		public void CanCompareNumericStrings()
		{
			var comparer = new NumericStringComparer();

			Assert.AreEqual(0, comparer.Compare("1a", "1a"));
			Assert.AreEqual(0, comparer.Compare("a1", "a1"));

			Assert.AreEqual(-1, comparer.Compare("1a", "2a"));
			Assert.AreEqual(-1, comparer.Compare("1a", "10a"));
			Assert.AreEqual(-1, comparer.Compare("a1", "a2"));
			Assert.AreEqual(-1, comparer.Compare("a2", "a11"));
			Assert.AreEqual(-1, comparer.Compare("a2a", "a11a"));

			Assert.AreEqual(1, comparer.Compare("2a", "1a"));
			Assert.AreEqual(1, comparer.Compare("10a", "1a"));
			Assert.AreEqual(1, comparer.Compare("a2", "a1"));
			Assert.AreEqual(1, comparer.Compare("a11", "a2"));
			Assert.AreEqual(1, comparer.Compare("a11a", "a2a"));

			//multiple numbers
			Assert.AreEqual(-1, comparer.Compare("1a2a", "2a1a"));
			Assert.AreEqual(1, comparer.Compare("10a1a", "2a2a"));
		}
	}
}
