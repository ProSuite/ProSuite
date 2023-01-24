using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class AlternateKeyRowReference : RowReference,
	                                        IEquatable<AlternateKeyRowReference>
	{
		[NotNull] private readonly object _key;

		public AlternateKeyRowReference([NotNull] object key)
		{
			Assert.ArgumentNotNull(key, nameof(key));

			_key = key;
		}

		public override long OID => -1;

		public override bool UsesOID => false;

		public override object Key => _key;

		public override string ToString()
		{
			return $"Key: {_key}";
		}

		public bool Equals(AlternateKeyRowReference other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return _key.Equals(other._key);
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

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((AlternateKeyRowReference) obj);
		}

		public override int GetHashCode()
		{
			return _key.GetHashCode();
		}
	}
}
