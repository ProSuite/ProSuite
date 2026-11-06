namespace ProSuite.Commons.GeoDb
{
	/// <summary>
	/// The limits of a where clause that a database accepts, used to split long IN lists.
	/// </summary>
	public static class WhereClauseLimits
	{
		/// <summary>
		/// The limits for an unknown database: short, but accepted everywhere.
		/// </summary>
		public const int SafeMaximumLength = 2000;

		public const int SafeMaximumInValueCount = 1000;

		/// <summary>
		/// Gets the maximum length of a where clause and the maximum number of values in one
		/// IN list for a database type. Unknown types get the safe limits.
		/// </summary>
		public static void Get(WorkspaceDbType dbType,
		                       out int maximumWhereClauseLength,
		                       out int maximumInValueCount)
		{
			maximumWhereClauseLength = SafeMaximumLength;
			maximumInValueCount = SafeMaximumInValueCount;

			switch (dbType)
			{
				case WorkspaceDbType.FileGeodatabase:
					// these values have been found to work
					maximumWhereClauseLength = 200000;
					maximumInValueCount = 10000;
					return;

				case WorkspaceDbType.PersonalGeodatabase:
					maximumWhereClauseLength = 60000; // limit is approximately 64000
					maximumInValueCount = 1000; // not sure
					return;

				case WorkspaceDbType.MobileGeodatabase:
					maximumWhereClauseLength = 100000000; // one billion bytes
					maximumInValueCount = 10000; // no explicit limit
					return;

				case WorkspaceDbType.ArcSDEOracle:
					maximumWhereClauseLength = 60000; // not sure where the limit is
					maximumInValueCount = 1000; // documented (ORA-01795), also in 23ai/26ai
					return;

				case WorkspaceDbType.ArcSDESqlServer:
					maximumWhereClauseLength = 60000; // actual limit is higher
					maximumInValueCount = 1000; // limit may be higher
					return;

				case WorkspaceDbType.ArcSDEPostgreSQL:
					// apparently unlimited
					maximumWhereClauseLength = 200000;
					maximumInValueCount = 10000;
					return;

				default:
					// Unknown, a generic ArcSDE, Informix, DB2, file system, feature service
					return;
			}
		}
	}
}
