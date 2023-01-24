namespace ProSuite.DomainServices.AO.QA.Issues
{
	public abstract class RowReference
	{
		public abstract long OID { get; }

		public abstract bool UsesOID { get; }

		public abstract object Key { get; }
	}
}
