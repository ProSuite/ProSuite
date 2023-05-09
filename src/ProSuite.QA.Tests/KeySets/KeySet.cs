using System;
using System.Collections;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.KeySets
{
	internal class KeySet<T> : IKeySet
	{
		private readonly HashSet<T> _keys;

		public KeySet()
		{
			_keys = new HashSet<T>();
		}

		#region Implementation of IKeySet

		public void Clear()
		{
			_keys.Clear();
		}

		public bool Add(object key)
		{
			T castKey = Cast(key);

			return _keys.Add(castKey);
		}

		public bool Contains(object key)
		{
			return _keys.Contains(Cast(key));
		}

		public int Count => _keys.Count;

		public bool Remove(object key)
		{
			return _keys.Remove(Cast(key));
		}

		#endregion

		protected virtual T Cast([NotNull] object key)
		{
			return (T) Convert.ChangeType(key, typeof(T));
		}

		public IEnumerator GetEnumerator()
		{
			return _keys.GetEnumerator();
		}
	}
}
