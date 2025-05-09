namespace ProSuite.Commons.GeoDb
{
	public enum DbConnectionType
	{
		Null = 0,

		/// <summary>
		/// File geodatabase.
		/// </summary>
		FileGeodatabase = 1,

		/// <summary>
		/// Folder containing shapefiles.
		/// </summary>
		FileSystemWorkspace = 2,

		/// <summary>
		/// SDE Connection file.
		/// </summary>
		DatabaseConnectionFile = 3,

		/// <summary>
		/// Database connection properties.
		/// </summary>
		DatabaseConnectionProperties = 4,

		/// <summary>
		/// SQLite database.
		/// </summary>
		SQLite = 5,

		/// <summary>
		/// Service connection properties.
		/// </summary>
		ServiceConnectionProperties = 6,

		/// <summary>
		/// Unsupported or deprecated connection type.
		/// </summary>
		Other = 7
	}
}
