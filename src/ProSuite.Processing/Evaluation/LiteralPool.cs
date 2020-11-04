using System;
using System.Collections.Generic;

namespace ProSuite.Processing.Evaluation
{
	public class LiteralPool
	{
		private object[] _pool;
		private IDictionary<object, int> _dict;

		public LiteralPool()
		{
			_pool = null;
			_dict = new Dictionary<object, int>();
		}

		public int Count
		{
			get { return _pool?.Length ?? _dict.Count; }
		}

		//public bool IsCommitted
		//{
		//    get { return _dict == null; }
		//}

		public int Put(object value)
		{
			if (_dict == null)
			{
				throw new InvalidOperationException("Cannot Put() after Commit()");
			}

			int index;
			if (_dict.TryGetValue(value, out index))
			{
				return index;
			}

			index = _dict.Count;
			_dict.Add(value, index);

			return index;
		}

		public void Commit()
		{
			if (_dict == null)
			{
				throw new InvalidOperationException("Already committed");
			}

			_pool = new object[_dict.Count];

			foreach (var pair in _dict)
			{
				int index = pair.Value;
				object value = pair.Key;

				_pool[index] = value;
			}

			_dict = null;
		}

		public object Get(int index)
		{
			if (_pool == null)
			{
				throw new InvalidOperationException("Must Commit() before Get()");
			}

			return _pool[index];
		}
	}
}
