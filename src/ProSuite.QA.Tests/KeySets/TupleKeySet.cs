using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.KeySets
{
	public class TupleKeySet : ITupleKeySet
	{
		[NotNull] private readonly HashSet<Tuple> _tuples = new HashSet<Tuple>();

		#region Implementation of IMultiKeySet

		public void Clear()
		{
			_tuples.Clear();
		}

		public bool Add(Tuple tuple)
		{
			return _tuples.Add(tuple);
		}

		public bool Contains(Tuple tuple)
		{
			return _tuples.Contains(tuple);
		}

		public int Count => _tuples.Count;

		public bool Remove(Tuple tuple)
		{
			return _tuples.Remove(tuple);
		}

		#endregion
	}
}
