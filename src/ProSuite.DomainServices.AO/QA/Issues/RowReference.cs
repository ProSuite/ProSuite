namespace ProSuite.DomainServices.AO.QA.Issues
{
	public abstract class RowReference
	{
		public abstract int OID { get; }

		public abstract bool UsesOID { get; }

		public abstract object Key { get; }
	}
}
