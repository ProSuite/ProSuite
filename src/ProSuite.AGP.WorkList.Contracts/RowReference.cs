namespace ProSuite.AGP.WorkList.Domain;

// todo daro why a base class?
public abstract class RowReference
{
	// TODO: Use long!
	public abstract int OID { get; }

	public abstract bool UsesOID { get; }

	public abstract object Key { get; }

	public abstract bool HasGeometry { get; }
}
