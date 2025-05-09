namespace ProSuite.DomainModel.Core.Geodatabase
{
	/// <summary>
	/// The type of database
	/// </summary>
	/// <remarks>Persisted, don't change existing ids</remarks>
	public enum DatabaseType
	{
		Oracle10 = 0,
		SqlServer = 1,
		PostgreSQL = 2,
		Oracle9 = 3,
		Oracle = 4,
		Oracle11 = 5
	}
}
