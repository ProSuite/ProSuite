namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Policy for dealing with association ends when objects on the 
	/// involved end are inserted as copies of existing or deleted objects.
	/// </summary>
	/// <remarks>This enumeration is mapped to the database, don't alter the integer values</remarks>
	public enum CopyPolicy
	{
		/// <summary>
		/// 
		/// </summary>
		DuplicateAssociation = 0,

		/// <summary>
		/// 
		/// </summary>
		DuplicateRelatedObjects = 1,

		/// <summary>
		/// 
		/// </summary>
		DeleteAssociation = 2
	}
}
