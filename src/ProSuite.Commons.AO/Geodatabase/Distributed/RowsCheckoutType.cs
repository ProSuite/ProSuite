namespace ProSuite.Commons.AO.Geodatabase.Distributed
{
	/// <summary>
	/// Defines the options for checking out rows for a dataset in a replica.
	/// </summary>
	public enum RowsCheckoutType
	{
		/// <summary>Check out the schema only.</summary>
		None,

		/// <summary>Check out all rows.</summary>
		All,

		/// <summary>Apply the filters defined by the <see cref="IDatasetReplicaFilter"/>
		/// implementation.</summary>
		Filter
	}
}
