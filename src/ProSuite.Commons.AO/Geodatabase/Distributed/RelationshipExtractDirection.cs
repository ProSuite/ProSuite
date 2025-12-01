namespace ProSuite.Commons.AO.Geodatabase.Distributed
{
	public enum RelationshipExtractDirection
	{
		/// <summary>Process the relationship class from origin to destination.</summary>
		Forward,

		/// <summary>Process the relationship class from destination to origin.</summary>
		Backward,

		/// <summary>Do not process the relationship class.</summary>
		None
	}
}
