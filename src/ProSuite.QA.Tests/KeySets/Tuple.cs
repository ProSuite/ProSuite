using System;
using System.Collections;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests.KeySets
{
	public class Tuple : IEquatable<Tuple>
	{
		[NotNull] private readonly object[] _keys;

		public Tuple(params object[] values) : this((ICollection) values) { }

		public Tuple([NotNull] ICollection values)
		{
			Assert.ArgumentNotNull(values, nameof(values));

			_keys = new object[values.Count];

			values.CopyTo(_keys, 0);
		}

		[NotNull]
		public IEnumerable Keys => _keys;

		public bool IsEmpty => _keys.Length == 0;

		public bool IsNull
		{
			get { return _keys.All(key => key == null || key is DBNull); }
		}

		public override string ToString()
		{
			return StringUtils.Concatenate(_keys, FormatValue, ", ");
		}

		public bool Equals(Tuple other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (_keys.Length != other._keys.Length)
			{
				return false;
			}

			for (int i = 0; i < _keys.Length; i++)
			{
				if (! Equals(_keys[i], other._keys[i]))
				{
					return false;
				}
			}

			return true;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != typeof(Tuple))
			{
				return false;
			}

			return Equals((Tuple) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int res = 0;
				foreach (object key in _keys)
				{
					res = (res * 397) ^ (key == null
						                     ? 11
						                     : key.GetHashCode());
				}

				return res;
			}
		}

		[NotNull]
		private static string FormatValue([CanBeNull] object value)
		{
			return value == null || value is DBNull
				       ? "<null>"
				       : value.ToString();
		}
	}
}
