using System;
using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.KeySets;
using Tuple = ProSuite.QA.Tests.KeySets.Tuple;

namespace ProSuite.QA.Tests.Test.KeySets
{
	[TestFixture]
	public class TupleKeySetTest
	{
		[Test]
		public void CanBuildKeySetFastEnough()
		{
			const int count1 = 1000;
			const int count2 = 500;

			Stopwatch watch = Stopwatch.StartNew();

			ITupleKeySet keyset = CreateTupleKeySet(count1, count2);

			watch.Stop();

			Console.WriteLine(@"Building key set for {0:N0} string tuples: {1:N0} ms",
			                  keyset.Count, watch.ElapsedMilliseconds);
			Assert.IsTrue(watch.ElapsedMilliseconds < 3000, "too slow");
		}

		[Test]
		public void CanCheckTupleKeySetForExistingTuplesFastEnough()
		{
			ITupleKeySet keyset = CreateTupleKeySet(1000, 500);

			Stopwatch watch = Stopwatch.StartNew();

			const int containsCount = 10000;
			for (int i = 0; i < containsCount; i++)
			{
				keyset.Contains(new Tuple("500", "200"));
			}

			watch.Stop();

			Console.WriteLine(
				@"Checking key set of {0:N0} string tuples {1:N0} times for Contains: {2:N0} ms",
				keyset.Count, containsCount, watch.ElapsedMilliseconds);

			Assert.IsTrue(watch.ElapsedMilliseconds < 1000, "too slow");
		}

		[Test]
		public void CanCheckTupleKeySetForNonExistingTuplesFastEnough()
		{
			ITupleKeySet keyset = CreateTupleKeySet(1000, 500);

			Stopwatch watch = Stopwatch.StartNew();

			const int containsCount = 10000;
			for (int i = 0; i < containsCount; i++)
			{
				keyset.Contains(new Tuple("doesnotexist", "neither"));
			}

			watch.Stop();

			Console.WriteLine(
				@"Checking key set of {0:N0} string tuples {1:N0} times for Contains: {2:N0} ms",
				keyset.Count, containsCount, watch.ElapsedMilliseconds);

			Assert.IsTrue(watch.ElapsedMilliseconds < 1000, "too slow");
		}

		[NotNull]
		private static ITupleKeySet CreateTupleKeySet(int count1, int count2)
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;

			var result = new TupleKeySet();

			for (int i = 0; i < count1; i++)
			{
				for (int j = 0; j < count2; j++)
				{
					result.Add(new Tuple(i.ToString(invariantCulture),
					                     j.ToString(invariantCulture)));
				}
			}

			return result;
		}
	}
}
