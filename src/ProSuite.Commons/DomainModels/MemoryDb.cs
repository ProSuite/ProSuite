using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public class MemoryDb : IMemoryDb
	{
		[NotNull] private readonly IList<Entity> _allEntities = new List<Entity>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryDb"/> class.
		/// </summary>
		public MemoryDb() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryDb"/> class.
		/// </summary>
		/// <param name="entity">The entity.</param>
		public MemoryDb(Entity entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			Add(entity);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryDb"/> class.
		/// </summary>
		/// <param name="entities">The entities.</param>
		public MemoryDb(params Entity[] entities)
		{
			AddRange(entities);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryDb"/> class.
		/// </summary>
		/// <param name="entities">The entities.</param>
		public MemoryDb(IEnumerable<Entity> entities)
		{
			Assert.ArgumentNotNull(entities, nameof(entities));

			AddRange(entities);
		}

		#endregion

		public T Add<T>(T entity) where T : Entity
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			if (! _allEntities.Contains(entity))
			{
				_allEntities.Add(entity);
			}

			return entity;
		}

		public void AddRange(IEnumerable<Entity> entities)
		{
			Assert.ArgumentNotNull(entities, nameof(entities));

			foreach (Entity entity in entities)
			{
				Add(entity);
			}
		}

		public void AddRange<T>(IEnumerable<T> entities) where T : Entity
		{
			Assert.ArgumentNotNull(entities, nameof(entities));

			foreach (T entity in entities)
			{
				Add(entity);
			}
		}

		public IList<T> Entities<T>() where T : Entity
		{
			var result = new List<T>();

			foreach (Entity entity in _allEntities)
			{
				if (entity is T)
				{
					result.Add((T) entity);
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the list of all entities for a given entity type
		/// </summary>
		/// <param name="entityType"></param>
		/// <returns></returns>
		public IList<Entity> Entities(Type entityType)
		{
			Assert.ArgumentNotNull(entityType, nameof(entityType));

			var result = new List<Entity>();

			foreach (Entity entity in _allEntities)
			{
				if (entityType.IsInstanceOfType(entity))
				{
					result.Add(entity);
				}
			}

			return result;
		}

		/// <summary>
		/// Creates and returns a list of all entities in the in-memory ddx
		/// </summary>
		/// <returns></returns>
		public IList<Entity> GetAllEntities()
		{
			return _allEntities;
		}
	}
}