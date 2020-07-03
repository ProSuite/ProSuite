namespace ProSuite.Commons.Ado
{
	public enum AdoTransactionLevel
	{
		NoTransaction,
		Default,
		ReadUncommitted,
		ReadCommitted,
		RepeatableRead,
		Serializable,
		Snapshot
	}
}
