namespace ProSuite.Commons.DomainModels
{
	public interface IEntityTest
	{
		/// <summary>
		/// Sets the id of the entity. Only use this for unit testing, when wanting to 
		/// simulate a persistent entity without having to persist it or when using an ID
		/// mapping with Generator 'Assigned'
		/// </summary>
		/// <remarks>This should never be used in non-unit-test-code. 
		/// Altering the database identity of an instance is not valid in nhibernate.</remarks>
		/// <param name="id">The id.</param>
		void SetId(int id);
	}
}
