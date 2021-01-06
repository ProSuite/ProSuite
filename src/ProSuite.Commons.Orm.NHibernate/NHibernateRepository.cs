using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Dialect;
using NHibernate.Metadata;
using NHibernate.Transform;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	/// <summary>
	/// base class for nhibernate repositories
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class NHibernateRepository<T> : NHibernateService, IRepository<T>
		where T : Entity
	{
		/// <summary>
		/// The nHibernate entity name as defined in the mapping.
		/// </summary>
		public virtual string MappedEntityName => null;

		#region IRepository<T> Members

		public T Get(int id)
		{
			using (ISession session = OpenSession())
			{
				AssertInTransaction(session);

				return MappedEntityName != null
					       ? session.Get(MappedEntityName, id) as T
					       : session.Get(typeof(T), id) as T;
			}
		}

		public IList<T> GetAll()
		{
			using (ISession session = OpenSession())
			{
				AssertInTransaction(session);

				ICriteria criteria = CreateCriteria(session);

				criteria.SetResultTransformer(new DistinctRootEntityResultTransformer());

				ConfigureGetAllCriteria(criteria);

				return criteria.List<T>();
			}
		}

		/// <summary>
		/// Re-read the state of the given instance from the underlying database. 
		/// </summary>
		/// <param name="entity">A persistent instance.</param>
		public void Refresh(T entity)
		{
			using (ISession session = OpenSession(true))
			{
				Assert.True(IsPersistent(entity),
				            "Entity not yet persisted, can't refresh");

				if (! session.Contains(entity))
				{
					session.Lock(entity, LockMode.None);
				}

				session.Refresh(entity);
			}
		}

		public void Save(T entity)
		{
			using (ISession session = OpenSession())
			{
				if (MappedEntityName != null)
				{
					session.SaveOrUpdate(MappedEntityName, entity);
				}
				else
				{
					session.SaveOrUpdate(entity);
				}
			}
		}

		public void Delete(T entity)
		{
			using (ISession session = OpenSession())
			{
				if (MappedEntityName != null)
				{
					session.Delete(MappedEntityName, entity);
				}
				else
				{
					session.Delete(entity);
				}
			}
		}

		#endregion

		protected virtual void ConfigureGetAllCriteria([NotNull] ICriteria criteria)
		{
			// no further configuration by default
		}

		[CanBeNull]
		protected T GetUniqueResult([NotNull] string propertyName,
		                            [CanBeNull] object value,
		                            bool ignoreCase)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			using (ISession session = OpenSession(true))
			{
				ICriteria criteria = CreateEqualityCriteria(session, propertyName,
				                                            value, ignoreCase);

				return criteria.UniqueResult<T>();
			}
		}

		[NotNull]
		protected IList<T> Get([NotNull] string propertyName,
		                       [CanBeNull] object value,
		                       bool ignoreCase)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			using (ISession session = OpenSession(true))
			{
				ICriteria criteria = CreateEqualityCriteria(session, propertyName,
				                                            value, ignoreCase);

				return criteria.List<T>();
			}
		}

		/// <summary>
		/// Gets the nhibernate class metadata for the entity type
		/// </summary>
		/// <returns></returns>
		protected IClassMetadata GetClassMetadata()
		{
			using (ISession session = OpenSession())
			{
				return MappedEntityName != null
					       ? session.SessionFactory.GetClassMetadata(MappedEntityName)
					       : session.SessionFactory.GetClassMetadata(typeof(T));
			}
		}

		[NotNull]
		protected static object GetHqlLiteral(bool booleanValue, [NotNull] ISession session)
		{
			return IsPostgreSql(session)
				       ? (object) booleanValue
				       : booleanValue
					       ? 1
					       : 0;
		}

		[NotNull]
		protected IList<S> GetAllCore<S>() where S : T
		{
			using (ISession session = OpenSession(true))
			{
				AssertInTransaction(session);

				ICriteria criteria = session.CreateCriteria(typeof(S));

				return criteria.SetResultTransformer(new DistinctRootEntityResultTransformer())
				               .List<S>();
			}
		}

		protected static bool IsPersistent([NotNull] Entity instance)
		{
			return instance.IsPersistent;
		}

		/// <summary>
		/// Gets the entity ids for a collection of entities.
		/// </summary>
		/// <typeparam name="E"></typeparam>
		/// <param name="entities">The entities.</param>
		/// <returns></returns>
		[NotNull]
		protected static IList<int> GetEntityIds<E>([NotNull] ICollection<E> entities)
			where E : Entity
		{
			Assert.ArgumentNotNull(entities, nameof(entities));

			var result = new List<int>(entities.Count);

			result.AddRange(entities.Select(entity => entity.Id));

			return result;
		}

		[NotNull]
		private ICriteria CreateCriteria([NotNull] ISession session)
		{
			ICriteria criteria =
				MappedEntityName != null
					? session.CreateCriteria(MappedEntityName)
					: session.CreateCriteria(typeof(T));

			return criteria;
		}

		[NotNull]
		private ICriteria CreateEqualityCriteria([NotNull] ISession session,
		                                         [NotNull] string propertyName,
		                                         [CanBeNull] object value,
		                                         bool ignoreCase)
		{
			Assert.ArgumentNotNull(session, nameof(session));
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			ICriteria criteria = CreateCriteria(session);

			return criteria.Add(GetEqualityExpression(propertyName, value, ignoreCase));
		}

		[NotNull]
		protected static SimpleExpression GetEqualityExpression(
			[NotNull] string propertyName,
			[CanBeNull] object value)
		{
			const bool ignoreCase = false;
			return GetEqualityExpression(propertyName, value, ignoreCase);
		}

		[NotNull]
		protected static SimpleExpression GetEqualityExpression(
			[NotNull] string propertyName,
			[CanBeNull] object value,
			bool ignoreCase)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			SimpleExpression expression = Restrictions.Eq(propertyName, value);

			if (ignoreCase)
			{
				expression.IgnoreCase();
			}

			return expression;
		}

		private static bool IsPostgreSql([NotNull] ISession session)
		{
			global::NHibernate.Dialect.Dialect dialect = GetDialect(session);
			return dialect is PostgreSQLDialect;
		}

		public static global::NHibernate.Dialect.Dialect GetDialect(ISession session)
		{
			global::NHibernate.Dialect.Dialect dialect =
				session.GetSessionImplementation().Factory.Dialect;
			return dialect;
		}
	}
}
