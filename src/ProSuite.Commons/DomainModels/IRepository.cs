using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	/// <summary>
	/// Base interface for domain model object repositories
	/// (Fowler PEAA, Evans DDD)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRepository<T> where T : Entity
	{
		/// <summary>
		/// Remove a persistent instance from the datastore. 
		/// </summary>
		/// <param name="entity">The instance to be removed.</param>
		/// <remarks>The argument may be an instance associated wit hthe receiving ISession 
		/// or a transient instance with an identifier associated with existing 
		/// persistent state.</remarks>
		void Delete([NotNull] T entity);

		/// <summary>
		/// Return the persistent instance of the entity class with the given identifier, 
		/// or null if there is no such persistent instance. (If the instance, or a proxy 
		/// for the instance, is already associated with the session, 
		/// return that instance or proxy.) 
		/// </summary>
		/// <param name="id">The entity identifier value.</param>
		/// <returns>A persistent instance or null.</returns>
		[CanBeNull]
		T Get(int id);

		[NotNull]
		IList<T> GetAll();

		void Refresh([NotNull] T entity);

		void Save([NotNull] T entity);
	}
}