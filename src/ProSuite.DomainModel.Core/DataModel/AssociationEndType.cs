namespace ProSuite.DomainModel.Core.DataModel
{
	public enum AssociationEndType
	{
		Unknown,
		OneToMany,
		ManyToOne,

		/// <summary>
		/// Destination end on 1:1 relationship
		/// </summary>
		OneToOneFK,

		/// <summary>
		/// Origin end on 1:1 relationship
		/// </summary>
		OneToOnePK,

		ManyToManyEnd1,
		ManyToManyEnd2
	}
}