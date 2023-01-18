using System;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class OIDRowReference : RowReference, IEquatable<OIDRowReference>
	{
		private readonly long _oid;

		public OIDRowReference(long oid)
		{
			_oid = oid;
		}

		public override long OID => _oid;

		public override bool UsesOID => true;

		public override object Key => _oid;

		public override string ToString()
		{
			return $"Oid: {_oid}";
		}

		public bool Equals(OIDRowReference other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return _oid == other._oid;
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

			return Equals((OIDRowReference) obj);
		}

		public override int GetHashCode()
		{
			return _oid.GetHashCode();
		}
	}
}
