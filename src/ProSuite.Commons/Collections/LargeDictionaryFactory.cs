using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Collections
{
	/// <summary>
	/// Adapted from https://gist.github.com/coxsim/956621
	/// </summary>
	public static class LargeDictionaryFactory
	{
		private const int _lohThresholdInBytes = 85000;

		internal static readonly int MaxDictionarySize = DetermineMaximumDictionaryEntries();

		private static readonly int HalfMaxDictionarySize =
			(int) Math.Floor(MaxDictionarySize / 2d);

		internal static readonly int PrimeAboveHalfMaxDictionarySize =
			HashHelpers.GetPrime(HalfMaxDictionarySize);

		[NotNull]
		[PublicAPI]
		public static IDictionary<TKey, TValue> CreateDictionary<TKey, TValue>(
			[NotNull] IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs,
			[CanBeNull] IEqualityComparer<TKey> equalityComparer = null)
		{
			return new ConsistentHashLargeDictionary<TKey, TValue>(keyValuePairs,
				equalityComparer);
		}

		[NotNull]
		[PublicAPI]
		public static IDictionary<TKey, TValue> CreateDictionary<TKey, TValue>(
			int expectedCount = 0,
			[CanBeNull] IEqualityComparer<TKey> equalityComparer = null)
		{
			return new ConsistentHashLargeDictionary<TKey, TValue>(expectedCount,
				equalityComparer);
		}

		private static int DetermineMaximumDictionaryEntries()
		{
			// It turns out it's actually impossible (by intention) to not be able to determine the 
			// size of a struct.  The best we can do is to assume the largest possible, and just 
			// accept the wasted space.
			// (See http://stackoverflow.com/questions/3361986/how-to-check-the-number-of-bytes-consumed-by-my-structure#3362736)

			const int dictEntrySizeInBytes = 32;
			var realMaximumCount =
				(int) Math.Floor(_lohThresholdInBytes / (double) dictEntrySizeInBytes);
			// find the next lowest prime to the number of entries

			return LowerPrime(realMaximumCount);
		}

		/// <summary>Get the next prime that is strictly lower than <code>num</code></summary>
		private static int LowerPrime(int num)
		{
			var nextLowestPrime = num - 1;
			while (! HashHelpers.IsPrime(nextLowestPrime))
			{
				nextLowestPrime--;
			}

			return nextLowestPrime;
		}
	}
}
