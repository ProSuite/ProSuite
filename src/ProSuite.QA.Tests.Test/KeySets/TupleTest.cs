using System;
using System.Diagnostics;
using NUnit.Framework;
using Tuple = ProSuite.QA.Tests.KeySets.Tuple;

namespace ProSuite.QA.Tests.Test.KeySets
{
	[TestFixture]
	public class TupleTest
	{
		[Test]
		public void CanCreateTuplesFastEnough()
		{
			const int count = 100000;
			const int stringLength = 1000;

			var value1 = new string('A', stringLength);
			var value2 = new string('B', stringLength);

			Stopwatch watch = Stopwatch.StartNew();

			for (int i = 0; i < count; i++)
			{
				new Tuple(value1, value2);
			}

			watch.Stop();

			Console.WriteLine(@"Creating {0:N0} string tuples: {1:N0} ms",
			                  count, watch.ElapsedMilliseconds);
			Assert.IsTrue(watch.ElapsedMilliseconds < 200, "too slow");
		}

		[Test]
		public void CanFormat()
		{
			var tuple = new Tuple(100, null, "abc");

			Assert.IsFalse(tuple.IsNull); // only if all keys are null/DBNull
			Assert.AreEqual("100, <null>, abc", tuple.ToString());
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanGetHashCode1()
		{
			var tuple = new Tuple(int.MaxValue, double.MaxValue, new string('A', 1000));

			Assert.AreEqual(967154681, tuple.GetHashCode());
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanGetHashCode2()
		{
			var tuple = new Tuple(int.MinValue, double.MinValue, string.Empty);

			Assert.AreEqual(-1242032386, tuple.GetHashCode());
		}

		[Test]
		public void CanGetHashCodeForNullTuple()
		{
			var tuple = new Tuple(null, null, null, null);

			Assert.IsTrue(tuple.IsNull);
			Assert.AreEqual(686549176, tuple.GetHashCode());
		}

		[Test]
		public void NullTuplesAreEqual()
		{
			var tuple1 = new Tuple(null, null, null, null);
			var tuple2 = new Tuple(null, null, null, null);

			Assert.IsTrue(tuple1.IsNull);
			Assert.IsTrue(tuple2.IsNull);
			Assert.AreEqual(tuple1, tuple2);
		}

		[Test]
		public void EmptyTuplesAreEqual()
		{
			var tuple1 = new Tuple();
			var tuple2 = new Tuple();

			Assert.IsTrue(tuple1.IsEmpty);
			Assert.IsTrue(tuple2.IsEmpty);
			Assert.AreEqual(tuple1, tuple2);
		}

		[Test]
		public void CanCompareTuplesOfDifferentLengths()
		{
			var tuple1 = new Tuple(1, 2);
			var tuple2 = new Tuple(1, 2, 3);

			Assert.AreNotEqual(tuple1, tuple2);
		}
	}
}
