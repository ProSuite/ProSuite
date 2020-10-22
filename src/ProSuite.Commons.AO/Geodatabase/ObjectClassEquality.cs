namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Definition of equality among object classes.
	/// </summary>
	public enum ObjectClassEquality
	{
		/// <summary>
		/// No two feature classes are considered equal.
		/// </summary>
		DontEquate,

		/// <summary>
		/// Two feature classes are considered equal
		/// if they represent the same database table (in any version).
		/// </summary>
		SameTableAnyVersion,

		/// <summary>
		/// Two feature classes are considered equal
		/// if they represent the same database table in the same version.
		/// </summary>
		SameTableSameVersion,

		/// <summary>
		/// Two feature classes are considered equal
		/// if they have the same name.
		/// </summary>
		SameDatasetName,

		/// <summary>
		/// Two feature classes are considered equal
		/// if they are the same instance (reference equality).
		/// </summary>
		SameInstance
	}
}
