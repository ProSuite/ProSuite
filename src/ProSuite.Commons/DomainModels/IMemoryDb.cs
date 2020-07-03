using System;
using System.Collections.Generic;

namespace ProSuite.Commons.DomainModels
{
	/// <summary>
	/// Interface for controlling an in-memory domain model store
	/// </summary>
	public interface IMemoryDb
	{
		T Add<T>(T entity) where T : Entity;

		void AddRange(IEnumerable<Entity> entities);

		IList<Entity> Entities(Type entityType);

		IList<Entity> GetAllEntities();
	}
}