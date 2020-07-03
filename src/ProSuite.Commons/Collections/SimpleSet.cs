using System.Collections;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Collections
{
	/// <summary>
	/// This class implements a simple collection of objects without
	/// duplicates, that is, the mathematical notion of a set.
	/// Null cannot be added to the set and testing Contains(null)
	/// throws an ArgumentNullException.
	/// </summary>
	/// <remarks>Besides the Add/Remove/Contains methods from the generic
	/// ICollection interface, overloaded versions of these methods exist
	/// that return a reference to the object that <b>equals</b> the one
	/// in the parameter and is <b>identical</b> to the one in the set.
	/// They return true if the set was modified and false otherwise.
	/// The <see cref="TryGetValue(T, out T)"/> method is a synonym for the
	/// two-argument <see cref="Contains(T,out T)"/> method.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the set</typeparam>
	public sealed class SimpleSet<T> : ICollection<T>
	{
		private const int _defaultInitialCapacity = 100;

		// This set implementation uses a dictionary, that is,
		// a set of (key,value) pairs, and always sets key=value.
		private readonly Dictionary<T, T> _dict;

		#region Constructors

		public SimpleSet() : this((IEqualityComparer<T>) null) { }

		public SimpleSet([CanBeNull] IEqualityComparer<T> comparer)
		{
			_dict = new Dictionary<T, T>(comparer);
		}

		public SimpleSet(int initialCapacity,
		                 [CanBeNull] IEqualityComparer<T> comparer = null)
		{
			_dict = new Dictionary<T, T>(initialCapacity, comparer);
		}

		public SimpleSet([NotNull] ICollection<T> items)
			: this(items, items.Count) { }

		public SimpleSet([NotNull] IEnumerable<T> items,
		                 [CanBeNull] IEqualityComparer<T> comparer)
			: this(items, _defaultInitialCapacity, comparer) { }

		public SimpleSet([NotNull] IEnumerable<T> items,
		                 int initialCapacity = _defaultInitialCapacity,
		                 [CanBeNull] IEqualityComparer<T> comparer = null)
			: this(initialCapacity, comparer)
		{
			Assert.ArgumentNotNull(items, nameof(items));

			foreach (T item in items)
			{
				if (! Contains(item))
				{
					Add(item);
				}
			}
		}

		#endregion

		#region ICollection<T> Members

		public void Add(T item)
		{
			_dict.Add(item, item);
		}

		public bool Contains(T item)
		{
			return _dict.ContainsKey(item);
		}

		public bool Remove(T item)
		{
			return _dict.Remove(item); // true if set modified
		}

		public void Clear()
		{
			_dict.Clear();
		}

		// The Keys property of a Dictionary object returns the
		// collection of keys as a KeyCollection object, which
		// implements (generic) ICollection and IEnumerable.
		//
		// Throws ArgumentNullException if array is null;
		// ArgumentOutOfRangeException if startIndex less than zero;
		// ArgumentException if the number of elements in the set
		//  is greater than the available slots from startIndex
		//  to the end of the array.
		public void CopyTo(T[] array, int startIndex)
		{
			_dict.Keys.CopyTo(array, startIndex);
		}

		public int Count
		{
			get { return _dict.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		#endregion

		#region IEnumerable and IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return ((IEnumerable<T>) _dict.Keys).GetEnumerator();
		}

		public IEnumerator GetEnumerator()
		{
			return _dict.Keys.GetEnumerator();
		}

		#endregion

		/// <summary>
		/// Tries to add the given item to the set. If an item Equal to the
		/// item to add is already in the set, then the set is not modified. 
		/// </summary>
		/// <param name="item">The item to add, not null.</param>
		/// <returns>True if the set was modified, false otherwise.</returns>
		public bool TryAdd([NotNull] T item)
		{
			T storedItem;
			return TryAdd(item, out storedItem);
		}

		/// <summary>
		/// Add the given item to the set. In the storedItem out
		/// parameter, store a reference to the item actually in
		/// the set; this always equals the given item but may
		/// not be identical.
		/// </summary>
		/// <param name="item">The item to add, not null.</param>
		/// <param name="storedItem">Reference to the item in the set.</param>
		/// <returns>True if the set was modified, false otherwise.</returns>
		public bool TryAdd([NotNull] T item, out T storedItem)
		{
			if (! _dict.TryGetValue(item, out storedItem))
			{
				_dict.Add(item, item);
				storedItem = item;

				return true; // set modified
			}

			return false; // set not modified
		}

		/// <summary>
		/// Remove the given item from the set.
		/// </summary>
		/// <param name="item">The item to remove, not null.</param>
		/// <param name="removedItem">Reference to the removed item;
		/// equals the given item or is null if the set did not
		/// contain the given item.</param>
		/// <returns>True if the set was modified, false otherwise.</returns>
		public bool Remove(T item, out T removedItem)
		{
			if (_dict.TryGetValue(item, out removedItem))
			{
				_dict.Remove(item);
				return true; // set modified
			}

			return false; // set not modified
		}

		/// <summary>
		/// Check if the given item is contained in the set.
		/// </summary>
		/// <param name="item">The item to look up.</param>
		/// <param name="storedItem">Reference to the item in the set;
		/// equals the given item or is null if the set does not contain
		/// the given item.</param>
		/// <returns>True if item is in the set, false otherwise.</returns>
		public bool Contains(T item, out T storedItem)
		{
			return _dict.TryGetValue(item, out storedItem);
		}

		/// <summary>
		/// Check if the given key item is contained in the set
		/// and, if so, return the actually contained item.
		/// </summary>
		/// <param name="keyItem">The item to look up.</param>
		/// <param name="storedItem">Reference to the item in the set;
		/// equals the given item or is null if the set does not contain
		/// the given item.</param>
		/// <returns>True if item is in the set, false otherwise.</returns>
		public bool TryGetValue(T keyItem, out T storedItem)
		{
			return _dict.TryGetValue(keyItem, out storedItem);
		}

		public override string ToString()
		{
			return $"Set with {Count} elements";
		}
	}
}
