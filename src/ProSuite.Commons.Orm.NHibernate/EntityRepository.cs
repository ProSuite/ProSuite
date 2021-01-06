using NHibernate;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	[UsedImplicitly]
	public class EntityRepository : NHibernateRepository<Entity>, IEntityRepository
	{
		/// <summary>
		/// Return the persistent instance of the entity class with the given identifier,
		/// or null if there is no such persistent instance. (If the instance, or a proxy
		/// for the instance, is already associated with the session,
		/// return that instance or proxy.)
		/// </summary>
		/// <param name="id">The entity identifier value.</param>
		/// <returns>A persistent instance or null.</returns>
		public Entity Get<T>(int id)
		{
			using (ISession session = OpenSession(true))
			{
				return session.Get(typeof(T), id) as Entity;
			}
		}
	}
}
