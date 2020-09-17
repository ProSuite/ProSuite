namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// The cardinality types of associations
	/// </summary>
	/// <remarks>Values are referenced in data dictionary, don't change</remarks>
	public enum AssociationCardinality
	{
		Unknown = 0,
		OneToOne = 1,
		OneToMany = 2,
		ManyToMany = 3
	}
}