namespace ProSuite.Commons.AO.Geodatabase
{
	public enum WorkspaceComparison
	{
		/// <summary>
		/// An exact comparison of workspace references is made, including version name.
		/// </summary>
		Exact,

		/// <summary>
		/// Workspaces are compared without regard to the version or user credentials. They are
		/// considered equal if they point to the same database (or sde repository in case of arcsde)
		/// </summary>
		AnyUserAnyVersion,

		/// <summary>
		/// Workspaces must refer to the same version in the same database
		/// (or SDE repository in case of ArcSDE), but the credentials used
		/// to connect to the database are ignored.
		/// </summary>
		AnyUserSameVersion
	}
}
