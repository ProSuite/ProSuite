using System;

namespace ProSuite.AGP.WorkList.Domain;

// todo daro use GdbRowIdentity
public class OIDRowReference : RowReference, IEquatable<OIDRowReference>
{
	private readonly int _oid;

	public OIDRowReference(int oid, bool hasGeometry)
	{
		HasGeometry = hasGeometry;
		_oid = oid;
	}

	public override int OID => _oid;

	public override bool UsesOID => true;

	public override object Key => _oid;

	public override bool HasGeometry { get; }

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
		return _oid;
	}
}
