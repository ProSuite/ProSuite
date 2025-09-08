using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Stat;
using NHibernate.Type;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Orm.NHibernate
{
	/// <summary>
	/// Session wrapper that emulates the same behaviour as the Castle transaction manager
	/// that allows for committing the transaction and closing the session by disposing
	/// the session object.
	/// </summary>
	public class SessionWrapper : ISession
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly ISession _inner;
		private readonly ITransaction _transaction;

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionWrapper"/> class.
		/// </summary>
		/// <param name="inner">The actual nhibernate session</param>
		/// <param name="isOutermost">Whether this is a completely new session or not.
		/// If not, we just piggy-back on top of it.</param>
		public SessionWrapper(ISession inner, bool isOutermost)
		{
			IsOutermost = isOutermost;

			_inner = inner;

			if (isOutermost)
			{
				_transaction = _inner.BeginTransaction();
				_msg.Debug("Started new NH session and transaction");
			}
		}

		/// <summary>
		/// Whether this session is the outer-most transaction that corresponds with the actual 
		/// nHibernate transaction.
		/// </summary>
		public bool IsOutermost { get; }

		public void Dispose()
		{
			if (_transaction == null)
			{
				// No op! The outer session/transaction should be able to live on!
			}
			else
			{
				try
				{
					if (_transaction.IsActive)
					{
						try
						{
							if (! DefaultReadOnly)
							{
								_transaction.Commit();
								_msg.Debug("Committed NH transaction");
							}
						}
						catch (Exception e)
						{
							_msg.Warn("Error committing transaction", e);
							_inner.Clear();
							// Note: We don't throw the exception to avoid masking the original exception.
						}
					}
					else
					{
						_msg.Debug("Transaction is already inactive - probably rolled back");
					}
				}
				finally
				{
					_transaction.Dispose();

					_inner.Dispose();

					_msg.Debug("Closed NH transaction");
				}
			}
		}

		public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.FlushAsync(cancellationToken);
		}

		public Task<bool> IsDirtyAsync(
			CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.IsDirtyAsync(cancellationToken);
		}

		public Task EvictAsync(object obj,
		                       CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.EvictAsync(obj, cancellationToken);
		}

		public Task<object> LoadAsync(Type theType, object id, LockMode lockMode,
		                              CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.LoadAsync(theType, id, lockMode, cancellationToken);
		}

		public Task<object> LoadAsync(string entityName, object id, LockMode lockMode,
		                              CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.LoadAsync(entityName, id, lockMode, cancellationToken);
		}

		public Task<object> LoadAsync(Type theType, object id,
		                              CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.LoadAsync(theType, id, cancellationToken);
		}

		public Task<T> LoadAsync<T>(object id, LockMode lockMode,
		                            CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.LoadAsync<T>(id, lockMode, cancellationToken);
		}

		public Task<T> LoadAsync<T>(object id,
		                            CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.LoadAsync<T>(id, cancellationToken);
		}

		public Task<object> LoadAsync(string entityName, object id,
		                              CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.LoadAsync(entityName, id, cancellationToken);
		}

		public Task LoadAsync(object obj, object id,
		                      CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.LoadAsync(obj, id, cancellationToken);
		}

		public Task ReplicateAsync(object obj, ReplicationMode replicationMode,
		                           CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.ReplicateAsync(obj, replicationMode, cancellationToken);
		}

		public Task ReplicateAsync(string entityName, object obj, ReplicationMode replicationMode,
		                           CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.ReplicateAsync(entityName, obj, replicationMode, cancellationToken);
		}

		public Task<object> SaveAsync(object obj,
		                              CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.SaveAsync(obj, cancellationToken);
		}

		public Task SaveAsync(object obj, object id,
		                      CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.SaveAsync(obj, id, cancellationToken);
		}

		public Task<object> SaveAsync(string entityName, object obj,
		                              CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.SaveAsync(entityName, obj, cancellationToken);
		}

		public Task SaveAsync(string entityName, object obj, object id,
		                      CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.SaveAsync(entityName, obj, id, cancellationToken);
		}

		public Task SaveOrUpdateAsync(object obj,
		                              CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.SaveOrUpdateAsync(obj, cancellationToken);
		}

		public Task SaveOrUpdateAsync(string entityName, object obj,
		                              CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.SaveOrUpdateAsync(entityName, obj, cancellationToken);
		}

		public Task SaveOrUpdateAsync(string entityName, object obj, object id,
		                              CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.SaveOrUpdateAsync(entityName, obj, id, cancellationToken);
		}

		public Task UpdateAsync(object obj,
		                        CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.UpdateAsync(obj, cancellationToken);
		}

		public Task UpdateAsync(object obj, object id,
		                        CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.UpdateAsync(obj, id, cancellationToken);
		}

		public Task UpdateAsync(string entityName, object obj,
		                        CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.UpdateAsync(entityName, obj, cancellationToken);
		}

		public Task UpdateAsync(string entityName, object obj, object id,
		                        CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.UpdateAsync(entityName, obj, id, cancellationToken);
		}

		public Task<object> MergeAsync(object obj,
		                               CancellationToken cancellationToken =
			                               new CancellationToken())
		{
			return _inner.MergeAsync(obj, cancellationToken);
		}

		public Task<object> MergeAsync(string entityName, object obj,
		                               CancellationToken cancellationToken =
			                               new CancellationToken())
		{
			return _inner.MergeAsync(entityName, obj, cancellationToken);
		}

		public Task<T> MergeAsync<T>(
			T entity, CancellationToken cancellationToken = new CancellationToken()) where T : class
		{
			return _inner.MergeAsync(entity, cancellationToken);
		}

		public Task<T> MergeAsync<T>(string entityName, T entity,
		                             CancellationToken cancellationToken = new CancellationToken())
			where T : class
		{
			return _inner.MergeAsync(entityName, entity, cancellationToken);
		}

		public Task PersistAsync(object obj,
		                         CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.PersistAsync(obj, cancellationToken);
		}

		public Task PersistAsync(string entityName, object obj,
		                         CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.PersistAsync(entityName, obj, cancellationToken);
		}

		public Task DeleteAsync(object obj,
		                        CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.DeleteAsync(obj, cancellationToken);
		}

		public Task DeleteAsync(string entityName, object obj,
		                        CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.DeleteAsync(entityName, obj, cancellationToken);
		}

		public Task<int> DeleteAsync(string query,
		                             CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.DeleteAsync(query, cancellationToken);
		}

		public Task<int> DeleteAsync(string query, object value, IType type,
		                             CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.DeleteAsync(query, value, type, cancellationToken);
		}

		public Task<int> DeleteAsync(string query, object[] values, IType[] types,
		                             CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.DeleteAsync(query, values, types, cancellationToken);
		}

		public Task LockAsync(object obj, LockMode lockMode,
		                      CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.LockAsync(obj, lockMode, cancellationToken);
		}

		public Task LockAsync(string entityName, object obj, LockMode lockMode,
		                      CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.LockAsync(entityName, obj, lockMode, cancellationToken);
		}

		public Task RefreshAsync(object obj,
		                         CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.RefreshAsync(obj, cancellationToken);
		}

		public Task RefreshAsync(object obj, LockMode lockMode,
		                         CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.RefreshAsync(obj, lockMode, cancellationToken);
		}

		public Task<IQuery> CreateFilterAsync(object collection, string queryString,
		                                      CancellationToken cancellationToken =
			                                      new CancellationToken())
		{
			return _inner.CreateFilterAsync(collection, queryString, cancellationToken);
		}

		public Task<object> GetAsync(Type clazz, object id,
		                             CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.GetAsync(clazz, id, cancellationToken);
		}

		public Task<object> GetAsync(Type clazz, object id, LockMode lockMode,
		                             CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.GetAsync(clazz, id, lockMode, cancellationToken);
		}

		public Task<object> GetAsync(string entityName, object id,
		                             CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.GetAsync(entityName, id, cancellationToken);
		}

		public Task<T> GetAsync<T>(object id,
		                           CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.GetAsync<T>(id, cancellationToken);
		}

		public Task<T> GetAsync<T>(object id, LockMode lockMode,
		                           CancellationToken cancellationToken = new CancellationToken())
		{
			return _inner.GetAsync<T>(id, lockMode, cancellationToken);
		}

		public Task<string> GetEntityNameAsync(object obj,
		                                       CancellationToken cancellationToken =
			                                       new CancellationToken())
		{
			return _inner.GetEntityNameAsync(obj, cancellationToken);
		}

		public ISharedSessionBuilder SessionWithOptions()
		{
			return _inner.SessionWithOptions();
		}

		public void Flush()
		{
			_inner.Flush();
		}

		public DbConnection Disconnect()
		{
			return _inner.Disconnect();
		}

		public void Reconnect()
		{
			_inner.Reconnect();
		}

		public void Reconnect(DbConnection connection)
		{
			_inner.Reconnect(connection);
		}

		public DbConnection Close()
		{
			return _inner.Close();
		}

		public void CancelQuery()
		{
			_inner.CancelQuery();
		}

		public bool IsDirty()
		{
			return _inner.IsDirty();
		}

		public bool IsReadOnly(object entityOrProxy)
		{
			return _inner.IsReadOnly(entityOrProxy);
		}

		public void SetReadOnly(object entityOrProxy, bool readOnly)
		{
			_inner.SetReadOnly(entityOrProxy, readOnly);
		}

		public object GetIdentifier(object obj)
		{
			return _inner.GetIdentifier(obj);
		}

		public bool Contains(object obj)
		{
			return _inner.Contains(obj);
		}

		public void Evict(object obj)
		{
			_inner.Evict(obj);
		}

		public object Load(Type theType, object id, LockMode lockMode)
		{
			return _inner.Load(theType, id, lockMode);
		}

		public object Load(string entityName, object id, LockMode lockMode)
		{
			return _inner.Load(entityName, id, lockMode);
		}

		public object Load(Type theType, object id)
		{
			return _inner.Load(theType, id);
		}

		public T Load<T>(object id, LockMode lockMode)
		{
			return _inner.Load<T>(id, lockMode);
		}

		public T Load<T>(object id)
		{
			return _inner.Load<T>(id);
		}

		public object Load(string entityName, object id)
		{
			return _inner.Load(entityName, id);
		}

		public void Load(object obj, object id)
		{
			_inner.Load(obj, id);
		}

		public void Replicate(object obj, ReplicationMode replicationMode)
		{
			_inner.Replicate(obj, replicationMode);
		}

		public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
		{
			_inner.Replicate(entityName, obj, replicationMode);
		}

		public object Save(object obj)
		{
			return _inner.Save(obj);
		}

		public void Save(object obj, object id)
		{
			_inner.Save(obj, id);
		}

		public object Save(string entityName, object obj)
		{
			return _inner.Save(entityName, obj);
		}

		public void Save(string entityName, object obj, object id)
		{
			_inner.Save(entityName, obj, id);
		}

		public void SaveOrUpdate(object obj)
		{
			_inner.SaveOrUpdate(obj);
		}

		public void SaveOrUpdate(string entityName, object obj)
		{
			_inner.SaveOrUpdate(entityName, obj);
		}

		public void SaveOrUpdate(string entityName, object obj, object id)
		{
			_inner.SaveOrUpdate(entityName, obj, id);
		}

		public void Update(object obj)
		{
			_inner.Update(obj);
		}

		public void Update(object obj, object id)
		{
			_inner.Update(obj, id);
		}

		public void Update(string entityName, object obj)
		{
			_inner.Update(entityName, obj);
		}

		public void Update(string entityName, object obj, object id)
		{
			_inner.Update(entityName, obj, id);
		}

		public object Merge(object obj)
		{
			return _inner.Merge(obj);
		}

		public object Merge(string entityName, object obj)
		{
			return _inner.Merge(entityName, obj);
		}

		public T Merge<T>(T entity) where T : class
		{
			return _inner.Merge(entity);
		}

		public T Merge<T>(string entityName, T entity) where T : class
		{
			return _inner.Merge(entityName, entity);
		}

		public void Persist(object obj)
		{
			_inner.Persist(obj);
		}

		public void Persist(string entityName, object obj)
		{
			_inner.Persist(entityName, obj);
		}

		public void Delete(object obj)
		{
			_inner.Delete(obj);
		}

		public void Delete(string entityName, object obj)
		{
			_inner.Delete(entityName, obj);
		}

		public int Delete(string query)
		{
			return _inner.Delete(query);
		}

		public int Delete(string query, object value, IType type)
		{
			return _inner.Delete(query, value, type);
		}

		public int Delete(string query, object[] values, IType[] types)
		{
			return _inner.Delete(query, values, types);
		}

		public void Lock(object obj, LockMode lockMode)
		{
			_inner.Lock(obj, lockMode);
		}

		public void Lock(string entityName, object obj, LockMode lockMode)
		{
			_inner.Lock(entityName, obj, lockMode);
		}

		public void Refresh(object obj)
		{
			_inner.Refresh(obj);
		}

		public void Refresh(object obj, LockMode lockMode)
		{
			_inner.Refresh(obj, lockMode);
		}

		public LockMode GetCurrentLockMode(object obj)
		{
			return _inner.GetCurrentLockMode(obj);
		}

		public ITransaction BeginTransaction()
		{
			return _inner.BeginTransaction();
		}

		public ITransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			return _inner.BeginTransaction(isolationLevel);
		}

		public void JoinTransaction()
		{
			_inner.JoinTransaction();
		}

		public ICriteria CreateCriteria<T>() where T : class
		{
			return _inner.CreateCriteria<T>();
		}

		public ICriteria CreateCriteria<T>(string alias) where T : class
		{
			return _inner.CreateCriteria<T>(alias);
		}

		public ICriteria CreateCriteria(Type persistentClass)
		{
			return _inner.CreateCriteria(persistentClass);
		}

		public ICriteria CreateCriteria(Type persistentClass, string alias)
		{
			return _inner.CreateCriteria(persistentClass, alias);
		}

		public ICriteria CreateCriteria(string entityName)
		{
			return _inner.CreateCriteria(entityName);
		}

		public ICriteria CreateCriteria(string entityName, string alias)
		{
			return _inner.CreateCriteria(entityName, alias);
		}

		public IQueryOver<T, T> QueryOver<T>() where T : class
		{
			return _inner.QueryOver<T>();
		}

		public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
		{
			return _inner.QueryOver(alias);
		}

		public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
		{
			return _inner.QueryOver<T>(entityName);
		}

		public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias)
			where T : class
		{
			return _inner.QueryOver(entityName, alias);
		}

		public IQuery CreateQuery(string queryString)
		{
			return _inner.CreateQuery(queryString);
		}

		public IQuery CreateFilter(object collection, string queryString)
		{
			return _inner.CreateFilter(collection, queryString);
		}

		public IQuery GetNamedQuery(string queryName)
		{
			return _inner.GetNamedQuery(queryName);
		}

		public ISQLQuery CreateSQLQuery(string queryString)
		{
			return _inner.CreateSQLQuery(queryString);
		}

		public void Clear()
		{
			_inner.Clear();
		}

		public object Get(Type clazz, object id)
		{
			return _inner.Get(clazz, id);
		}

		public object Get(Type clazz, object id, LockMode lockMode)
		{
			return _inner.Get(clazz, id, lockMode);
		}

		public object Get(string entityName, object id)
		{
			return _inner.Get(entityName, id);
		}

		public T Get<T>(object id)
		{
			return _inner.Get<T>(id);
		}

		public T Get<T>(object id, LockMode lockMode)
		{
			return _inner.Get<T>(id, lockMode);
		}

		public string GetEntityName(object obj)
		{
			return _inner.GetEntityName(obj);
		}

		public IFilter EnableFilter(string filterName)
		{
			return _inner.EnableFilter(filterName);
		}

		public IFilter GetEnabledFilter(string filterName)
		{
			return _inner.GetEnabledFilter(filterName);
		}

		public void DisableFilter(string filterName)
		{
			_inner.DisableFilter(filterName);
		}

		[Obsolete]
		public IMultiQuery CreateMultiQuery()
		{
			return _inner.CreateMultiQuery();
		}

		public ISession SetBatchSize(int batchSize)
		{
			return _inner.SetBatchSize(batchSize);
		}

		public ISessionImplementor GetSessionImplementation()
		{
			return _inner.GetSessionImplementation();
		}

		[Obsolete]
		public IMultiCriteria CreateMultiCriteria()
		{
			return _inner.CreateMultiCriteria();
		}

		[Obsolete]
		public ISession GetSession(EntityMode entityMode)
		{
			return _inner.GetSession(entityMode);
		}

		public IQueryable<T> Query<T>()
		{
			return _inner.Query<T>();
		}

		public IQueryable<T> Query<T>(string entityName)
		{
			return _inner.Query<T>(entityName);
		}

		public FlushMode FlushMode
		{
			get => _inner.FlushMode;
			set => _inner.FlushMode = value;
		}

		public CacheMode CacheMode
		{
			get => _inner.CacheMode;
			set => _inner.CacheMode = value;
		}

		public ISessionFactory SessionFactory => _inner.SessionFactory;

		public DbConnection Connection => _inner.Connection;

		public bool IsOpen => _inner.IsOpen;

		public bool IsConnected => _inner.IsConnected;

		public bool DefaultReadOnly
		{
			get => _inner.DefaultReadOnly;
			set => _inner.DefaultReadOnly = value;
		}

		public ITransaction Transaction => _inner.Transaction;

		public ISessionStatistics Statistics => _inner.Statistics;
	}
}
