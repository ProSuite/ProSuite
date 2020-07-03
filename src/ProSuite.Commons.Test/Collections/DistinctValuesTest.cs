using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.Collections;

namespace ProSuite.Commons.Test.Collections
{
	[TestFixture]
	public class DistinctValuesTest
	{
		[Test]
		public void CanGetMostFrequentValue()
		{
			var distinctValues = new DistinctValues<string>();

			distinctValues.Add("a");
			distinctValues.Add("a");
			distinctValues.Add("a");
			distinctValues.Add("b");
			distinctValues.Add(null);

			string value;
			int count;

			Assert.AreEqual(1, distinctValues.NullCount);
			Assert.IsTrue(distinctValues.TryGetMostFrequentValue(out value, out count));
			Assert.AreEqual("a", value);
			Assert.AreEqual(3, count);
		}

		[Test]
		public void CanGetMostFrequentNullValue()
		{
			var distinctValues = new DistinctValues<string>();

			distinctValues.Add("a");
			distinctValues.Add("a");
			distinctValues.Add("a");
			distinctValues.Add("b");
			distinctValues.Add(null);
			distinctValues.Add(null);
			distinctValues.Add(null);
			distinctValues.Add(null);

			string value;
			int count;

			Assert.AreEqual(4, distinctValues.NullCount);
			Assert.IsTrue(distinctValues.TryGetMostFrequentValue(out value, out count));
			Assert.IsNull(value);
			Assert.AreEqual(4, count);
		}

		[Test]
		public void CantGetMostFrequentValueIfEmpty()
		{
			var distinctValues = new DistinctValues<string>();

			string value;
			int count;

			Assert.IsFalse(distinctValues.TryGetMostFrequentValue(out value, out count));
		}

		[Test]
		public void CanUnion()
		{
			var dv1 = new DistinctValues<string>();

			dv1.Add("a");
			dv1.Add("a");
			dv1.Add("a");
			dv1.Add("b");
			dv1.Add(null);
			dv1.Add(null);
			dv1.Add(null);
			dv1.Add(null);

			var dv2 = new DistinctValues<string>();

			dv2.Add("a");
			dv2.Add("b");
			dv2.Add("b");
			dv2.Add("b");
			dv2.Add("b");
			dv2.Add("c");
			dv2.Add(null);

			dv1.Union(dv2);

			Assert.AreEqual(5, dv1.NullCount);
			string value;
			int count;

			Assert.IsTrue(dv1.TryGetMostFrequentValue(out value, out count));
			Assert.AreEqual("b", value);
			Assert.AreEqual(5, count);

			var list = new List<DistinctValue<string>>(dv1.Values);
			Assert.AreEqual(3, list.Count);
		}
	}
}