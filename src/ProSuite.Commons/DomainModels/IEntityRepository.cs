namespace ProSuite.Commons.DomainModels
{
	public interface IEntityRepository : IRepository<Entity>
	{
		/// <summary>
		/// Gets the specified id.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		Entity Get<T>(int id);
	}
}