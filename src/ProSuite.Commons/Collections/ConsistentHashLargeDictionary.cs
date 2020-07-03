using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using C5Lib = C5;

namespace ProSuite.Commons.Collections
{
	/// <summary>
	/// Dictionary that uses consistent hashing to maintain a set of sub-dictionaries, all of which
	/// should never grow past the point where they would cause allocations on the large object heap.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <remarks>
	/// Consistent hashing is used to reduce the number of nodes to rehash when the number of 
	/// dictionaries needs to change.
	/// 
	/// All values are stored in buckets spread evenly (to begin with) at points around a circle.  
	/// The points on the circle are all the integers between <code>MIN_HASH</code> and 
	/// <code>MAX_HASH</code>. The hash-code of a key represents a point on a circle. The key's 
	/// value is stored in the closest previous bucket around the circle.
	/// 
	/// See http://en.wikipedia.org/wiki/Consistent_hashing
	/// 
	/// Adapted from https://gist.github.com/coxsim/956621
	/// </remarks>
	internal class ConsistentHashLargeDictionary<TKey, TValue> :
		IDictionary<TKey, TValue>
	{
		private const int _minHash = int.MinValue;
		private const int _maxHash = int.MaxValue;

		[NotNull] private readonly IEqualityComparer<TKey> _equalityComparer;

		[NotNull] private readonly C5Lib.TreeDictionary<int, IDictionary<TKey, TValue>>
			_circle =
				new C5Lib.TreeDictionary<int, IDictionary<TKey, TValue>>();

		private C5Lib.KeyValuePair<int, IDictionary<TKey, TValue>> _firstNode;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConsistentHashLargeDictionary{TKey, TValue}"/> class.
		/// </summary>
		/// <param name="keyValuePairs">The collection of key/value pairs to add to the dictionary.</param>
		/// <param name="equalityComparer">The equality comparer.</param>
		public ConsistentHashLargeDictionary(
				[NotNull] IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs,
				[CanBeNull] IEqualityComparer<TKey> equalityComparer = null)
			// ReSharper disable once PossibleMultipleEnumeration
			: this(TryGetCollectionCount(keyValuePairs), equalityComparer)
		{
			// ReSharper disable once PossibleMultipleEnumeration
			foreach (var item in keyValuePairs)
			{
				Add(item.Key, item.Value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConsistentHashLargeDictionary{TKey, TValue}"/> class.
		/// </summary>
		/// <param name="initialCapacity">The initial capacity.</param>
		/// <param name="equalityComparer">The equality comparer.</param>
		public ConsistentHashLargeDictionary(
			int initialCapacity = 0,
			[CanBeNull] IEqualityComparer<TKey> equalityComparer = null)
		{
			_equalityComparer = equalityComparer ?? EqualityComparer<TKey>.Default;

			var nodeCount = initialCapacity > 0
				                ? (int) Math.Ceiling(initialCapacity /
				                                     (double)
				                                     LargeDictionaryFactory.MaxDictionarySize)
				                : 1;

			// split the circle into 'nodeCount' dictionaries spread equaly apart

			// optimisation for small dictionaries: just create the single node at the minimum point
			// on the circle

			if (nodeCount == 1)
			{
				_circle[_minHash] = new Dictionary<TKey, TValue>(initialCapacity, equalityComparer);
			}
			else
			{
				// for larger dictionaries, create the points on the circle, but leave the creation 
				// of each dictionary until actually need to add to it

				var hashGap = (_maxHash - (long) _minHash) / nodeCount;

				// need to use a long for the hash to avoid wrapping around on the last gap.  It's safe
				// to cast as has is always bounded by MIN_HASH and MAX_HASH which are ints.

				for (long hash = _minHash; hash < _maxHash; hash += hashGap)
				{
					_circle[(int) hash] = null;
				}
			}

			_firstNode = _circle.First();
		}

		private static int TryGetCollectionCount<T>([NotNull] IEnumerable<T> enumerable)
		{
			var collection = enumerable as ICollection<T>;
			return collection?.Count ?? 0;
		}

		private C5Lib.KeyValuePair<int, IDictionary<TKey, TValue>> GetNode(TKey key)
		{
			// optimisation for small dictionaries

			if (_circle.Count == 1)
			{
				return _firstNode;
			}

			var hash = _equalityComparer.GetHashCode(key);

			return GetNode(hash);
		}

		/// <summary>
		/// Get the node that may hold a key.
		/// </summary>
		/// <param name="hash">key to locate node for</param>
		/// <returns>node that might hold the key</returns>
		/// <remarks>This will not create any nodes on the circle.</remarks>
		private C5Lib.KeyValuePair<int, IDictionary<TKey, TValue>> GetNode(int hash)
		{
			// optimisation for small dictionaries
			//
			if (_circle.Count == 1)
			{
				return _firstNode;
			}

			C5Lib.KeyValuePair<int, IDictionary<TKey, TValue>> node;
			if (! _circle.TryWeakPredecessor(hash, out node))
			{
				// this means something very bad has happened as there should always be a node at the 
				// lowest point on the circle

				throw new Exception("Root node is missing!");
			}

			return node;
		}

		/// <summary>
		/// Get the dictionary that may hold a key.
		/// </summary>
		/// <param name="key">key to locate dictionary for</param>
		/// <returns>dictionary that might hold the key</returns>
		/// <remarks>This will not create any nodes on the circle.</remarks>
		[CanBeNull]
		private IDictionary<TKey, TValue> GetDictionary(TKey key)
		{
			return GetNode(key).Value;
		}

		private void Add(TKey key, TValue value, bool adding)
		{
			var hash = _equalityComparer.GetHashCode(key);
			var node = GetNode(hash);

			if (node.Value == null)
			{
				// this is the first node in the bucket
				//
				CreateSubDictionary(node.Key, key, value);
				return;
			}

			var dictionary = node.Value;

			// If we started as a 'small' dictionary then the capacity will not be the maximum.  
			// (For dictionaries with more than one node, i.e. 'large' dictionaries, the individual
			// dictionaries will always be at maximum capacity).

			// So, this checks if the Add will cause the smaller dictionary's capacity to go beyond
			// the maximum size.

			if (_circle.Count == 1 &&
			    dictionary.Count == LargeDictionaryFactory.PrimeAboveHalfMaxDictionarySize)
			{
				// In this case, we need to take a hit and replace the dictionary at some point with one at 
				// a the maximum capacity.  This is because the process of doubling the initial capacity 
				// will take us over the maximum at this point (count == prime above half max capacity).
				//

				var newDictionary =
					new Dictionary<TKey, TValue>(LargeDictionaryFactory.MaxDictionarySize,
					                             _equalityComparer);

				foreach (var entry in dictionary)
				{
					newDictionary.Add(entry.Key, entry.Value);
				}

				_circle[node.Key] = newDictionary;

				if (node.Key == _minHash)
				{
					_firstNode.Value = newDictionary;
				}

				dictionary = newDictionary;
			}
			else
			{
				// We can assume here that the dictionary's capacity is at the maximum, either 
				// because it is part of a 'large' dictionary where all the sub dictionaries
				// started off as maximum, or because it was once a 'small' dictionary that got
				// replaced with a maximum capacity dictionary above.

				// Now check if the Add would cause an already maximum capacity dictionary to be expanded.
				//
				// Check if size has been breached and 

				while (dictionary.Count >= LargeDictionaryFactory.MaxDictionarySize)
				{
					// If so, keep splitting up the offending node until there's space. If the hash
					// code is well distributed this should only happen once.  However, if all of the
					// nodes in the offending dictionary have hash codes in the lower half of the 
					// split the node will be split again.

					var newNode = SplitNode(node);

					if (hash >= newNode.Key)
					{
						node = newNode;
						dictionary = node.Value;
					}
				}
			}

			if (adding)
			{
				dictionary.Add(key, value);
			}
			else
			{
				dictionary[key] = value;
			}

			Debug.Assert(dictionary.Count <= LargeDictionaryFactory.MaxDictionarySize);
		}

		/// <summary>Create a new sub dictionary for a single entry.</summary>
		private void CreateSubDictionary(int circleKey, TKey initialKey, TValue initialValue)
		{
			var newDictionary =
				new Dictionary<TKey, TValue>(LargeDictionaryFactory.MaxDictionarySize,
				                             _equalityComparer)
				{
					{
						initialKey,
						initialValue
					}
				};

			_circle[circleKey] = newDictionary;

			if (circleKey == _minHash)
			{
				_firstNode.Value = newDictionary;
			}
		}

		/// <summary>
		/// Splits the dictionary at the given node.
		/// </summary>
		/// <returns>Dictionary at the new node</returns>
		private C5Lib.KeyValuePair<int, IDictionary<TKey, TValue>> SplitNode(
			C5Lib.KeyValuePair<int, IDictionary<TKey, TValue>> node)
		{
			C5Lib.KeyValuePair<int, IDictionary<TKey, TValue>> nextNode;
			var nextHash = _circle.TrySuccessor(node.Key, out nextNode)
				               ? nextNode.Key
				               : _maxHash;

			var midHash = node.Key + (int) ((nextHash - (long) node.Key) / 2);

			Debug.Assert(node.Key < nextHash && node.Key < midHash && midHash < nextHash);

			if (_circle.Contains(midHash))
			{
				// TODO allow larger sub-dictionaries, to avoid exception?
				throw new Exception(
					"Run out of nodes. Hash code is not evenly distributed enough.");
			}

			// now take (ideally half) the keys from the old node and insert them into the new node

			var dictionary = node.Value;
			var entriesToMove =
				dictionary.Where(kv => _equalityComparer.GetHashCode(kv.Key) >= midHash)
				          .ToList();

			var emptyDictionary =
				new Dictionary<TKey, TValue>(LargeDictionaryFactory.MaxDictionarySize,
				                             _equalityComparer);

			IDictionary<TKey, TValue> dictionaryForMidNode;
			if (entriesToMove.Count == dictionary.Count)
			{
				// optimisation where *all* of the keys need moving

				_circle[node.Key] = emptyDictionary;
				_circle[midHash] = dictionary;
				dictionaryForMidNode = dictionary;
			}
			else
			{
				foreach (var entryToMove in entriesToMove)
				{
					var removed = dictionary.Remove(entryToMove.Key);
					Debug.Assert(removed);

					emptyDictionary.Add(entryToMove.Key, entryToMove.Value);
				}

				_circle[midHash] = emptyDictionary;
				dictionaryForMidNode = emptyDictionary;
			}

			return new C5Lib.KeyValuePair<int, IDictionary<TKey, TValue>>(midHash,
			                                                              dictionaryForMidNode);
		}

		#region Implementation of IDictionary<TKey,TValue>

		public bool ContainsKey(TKey key)
		{
			// ReSharper disable CompareNonConstrainedGenericWithNull
			Debug.Assert(key != null);
			// ReSharper restore CompareNonConstrainedGenericWithNull

			IDictionary<TKey, TValue> dictionary = GetDictionary(key);

			return dictionary != null && dictionary.ContainsKey(key);
		}

		public void Add(TKey key, TValue value)
		{
			Add(key, value, true);
		}

		public bool Remove(TKey key)
		{
			IDictionary<TKey, TValue> dictionary = GetDictionary(key);

			return dictionary != null && dictionary.Remove(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			IDictionary<TKey, TValue> dictionary = GetDictionary(key);

			if (dictionary == null)
			{
				value = default(TValue);
				return false;
			}

			return dictionary.TryGetValue(key, out value);
		}

		public TValue this[TKey key]
		{
			get
			{
				IDictionary<TKey, TValue> dictionary = GetDictionary(key);

				if (dictionary == null)
				{
					throw new KeyNotFoundException();
				}

				return dictionary[key];
			}
			set { Add(key, value, false); }
		}

		public ICollection<TKey> Keys
		{
			get
			{
				return _circle.Where(kvp => kvp.Value != null)
				              .SelectMany(kvp => kvp.Value.Keys)
				              .ToList();
			}
		}

		public ICollection<TValue> Values
		{
			get
			{
				return _circle.Where(kvp => kvp.Value != null)
				              .SelectMany(kvp => kvp.Value.Values)
				              .ToList();
			}
		}

		#endregion

		#region Implementation of IEnumerable

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _circle.Where(kvp => kvp.Value != null)
			              .SelectMany(kvp => kvp.Value)
			              .GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Implementation of ICollection<KeyValuePair<TKey,TValue>>

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			// circle.Values.Where(dict => dict != null).ForEach(dict => dict.Clear());
			foreach (var dict in _circle.Values.Where(d => d != null))
			{
				dict.Clear();
			}
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return ContainsKey(item.Key);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			foreach (var dict in _circle.Where(kvp => kvp.Value != null)
			                            .Select(kvp => kvp.Value))
			{
				dict.CopyTo(array, arrayIndex);
				arrayIndex += dict.Count;
			}
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			return Remove(item.Key);
		}

		internal int PartitionCount
		{
			get { return _circle.Count; }
		}

		public int Count
		{
			get { return _circle.Where(kvp => kvp.Value != null).Sum(kvp => kvp.Value.Count); }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		#endregion
	}
}