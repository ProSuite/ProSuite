using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NHibernate;
using NHibernate.Collection;
using NHibernate.Proxy;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Orm.NHibernate
{
	[UsedImplicitly]
	public class NHibernateUnitOfWork : NHibernateUnitOfWorkBase, IUnitOfWork
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NHibernateUnitOfWork"/> class.
		/// </summary>
		/// <param name="sessionManager">The session manager.</param>
		public NHibernateUnitOfWork([NotNull] ISessionProvider sessionManager)
		{
			Assert.ArgumentNotNull(sessionManager, nameof(sessionManager));

			SessionManager = sessionManager;
		}

		#endregion

		#region IUnitOfWork Members

		public bool HasChanges
		{
			get
			{
				using (ISession session = OpenSession())
				{
					AssertRequiredContext(session,
					                      RequiredContext.LongSessionOrTransaction);

					return session.IsDirty();
				}
			}
		}

		public void Start()
		{
			if (SessionManager.CurrentSession != null)
			{
				throw new InvalidOperationException("Session already started");
			}

			OpenSession();
		}

		public bool Started => SessionManager.CurrentSession != null;

		public void Stop()
		{
			if (SessionManager.CurrentSession == null)
			{
				throw new InvalidOperationException("Session not started");
			}

			SessionManager.CurrentSession.Close();
		}

		public void Reattach(params Entity[] entities)
		{
			using (ISession session = OpenSession())
			{
				AssertSessionContextDefined(session);

				ReattachCore(session, entities);
			}
		}

		public void Reattach<T>(ICollection<T> collection) where T : class
		{
			ICollection<T> innerCollection = GetInnerCollection(collection);

			if (! NHibernateUtil.IsInitialized(innerCollection))
			{
				return;
			}

			using (ISession session = OpenSession())
			{
				AssertSessionContextDefined(session);

				ReattachCore(session, innerCollection);
			}
		}

		public void ReattachForUpdate(params Entity[] entities)
		{
			using (ISession session = OpenSession())
			{
				AssertSessionContextDefined(session);

				foreach (Entity entity in entities)
				{
					if (session.Contains(entity))
					{
						// already part of the session; Update not needed
						// (if the entity was modified while attached to the
						// session) or too late now. In the latter case the
						// Update would have had to be done with the initial
						// reattachment of the entity.
					}
					else
					{
						session.Update(entity);
					}
				}
			}
		}

		public void ReattachWithVersionCheck(params Entity[] entities)
		{
			const bool requireTransaction = true;
			using (ISession session = OpenSession(requireTransaction))
			{
				foreach (Entity entity in entities)
				{
					session.Lock(entity, LockMode.Read);
				}
			}
		}

		public void LockForUpdate(params Entity[] entities)
		{
			const bool requireTransaction = true;
			using (ISession session = OpenSession(requireTransaction))
			{
				foreach (Entity entity in entities)
				{
					var detachedState = entity as IDetachedState;
					if (detachedState != null)
					{
						detachedState.ReattachState(this);
					}
					else
					{
						session.Lock(entity, LockMode.Upgrade);
					}
				}
			}
		}

		public void Detach(Entity entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			using (ISession session = OpenSession())
			{
				session.Evict(entity);
			}
		}

		public bool IsInitialized<T>(ICollection<T> collection) where T : class
		{
			return NHibernateUtil.IsInitialized(GetInnerCollection(collection));
		}

		public void Initialize<T>(ICollection<T> collection) where T : class
		{
			ICollection inner = (ICollection) GetInnerCollection(collection);

			Initialize(inner);
		}

		public void Initialize(ICollection collection)
		{
			if (! (collection is IPersistentCollection))
			{
				throw new ArgumentException(
					@"Not a persistent collection, cannot initialize",
					nameof(collection));
			}

			using (ISession session = OpenSession())
			{
				if (NHibernateUtil.IsInitialized(collection))
				{
					return;
				}

				AssertInTransaction(session);

				NHibernateUtil.Initialize(collection);
			}
		}

		public void Initialize(Entity entity)
		{
			if (! (entity is INHibernateProxy))
			{
				return;
			}

			using (ISession session = OpenSession())
			{
				if (NHibernateUtil.IsInitialized(entity))
				{
					return;
				}

				AssertInTransaction(session);

				session.Lock(entity, LockMode.None);

				NHibernateUtil.Initialize(entity);
			}
		}

		public void Commit()
		{
			using (ISession session = OpenSession(requireTransaction: true))
			{
				session.Transaction.Commit();

				// Restart straight away
				session.BeginTransaction();
			}
		}

		public Entity Merge(Entity entity)
		{
			if (! NHibernateUtil.IsInitialized(entity))
			{
				return entity;
			}

			using (ISession session = OpenSession(requireTransaction: true))
			{
				return session.Merge(entity);
			}
		}

		public void Flush()
		{
			using (ISession session = OpenSession(requireTransaction: true))
			{
				session.Flush();
			}
		}

		public void Reset()
		{
			using (ISession session = OpenSession())
			{
				session.Clear();
			}
		}

		public virtual void Persist(params Entity[] entities)
		{
			Do(delegate(ISession session)
			{
				foreach (Entity entity in entities)
				{
					session.Save(entity);
				}
			}, RequiredContext.Transaction);
		}

		public virtual void UseTransaction(Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			Do(delegate { procedure(); }, RequiredContext.Transaction);
		}

		public virtual T UseTransaction<T>(Func<T> function)
		{
			Assert.ArgumentNotNull(function, nameof(function));

			T result = default(T);

			Do(delegate { result = function(); }, RequiredContext.Transaction);

			return result;
		}

		public virtual void UseTransaction(Action procedure, params Entity[] entities)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			Do(delegate { procedure(); }, RequiredContext.Transaction, entities);
		}

		public void UseTransaction(IDetachedState detachedState, Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			Do(delegate { procedure(); }, RequiredContext.Transaction, detachedState);
		}

		public virtual T ReadOnlyTransaction<T>(Func<T> function)
		{
			Assert.ArgumentNotNull(function, nameof(function));

			T result = default(T);

			DoReadOnlyTransaction(delegate { result = function(); });

			return result;
		}

		public virtual void ReadOnlyTransaction(Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			DoReadOnlyTransaction(delegate { procedure(); });
		}

		public virtual void ReadOnlyTransaction(Action procedure, params Entity[] entities)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			DoReadOnlyTransaction(delegate { procedure(); }, entities);
		}

		public void ReadOnlyTransaction(IDetachedState detachedState, Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			DoReadOnlyTransaction(delegate { procedure(); }, detachedState);
		}

		public virtual T NewTransaction<T>(Func<T> procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			AssertNoExistingTransaction();

			T result = default(T);

			Do(delegate { result = procedure(); }, RequiredContext.Transaction);

			return result;
		}

		public virtual void NewTransaction(Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			AssertNoExistingTransaction();

			Do(delegate { procedure(); }, RequiredContext.Transaction);
		}

		public virtual void NewTransaction(Action procedure, params Entity[] entities)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			AssertNoExistingTransaction();

			Do(delegate { procedure(); }, RequiredContext.Transaction, entities);
		}

		public void NewTransaction(IDetachedState detachedState, Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			AssertNoExistingTransaction();

			Do(delegate { procedure(); }, RequiredContext.Transaction, detachedState);
		}

		#endregion

		[NotNull]
		private static ICollection<T> GetInnerCollection<T>(
			[NotNull] ICollection<T> collection)
			where T : class
		{
			var readOnlyList = collection as ReadOnlyList<T>;
			if (readOnlyList != null)
			{
				return readOnlyList.Inner;
			}

			if (collection is ReadOnlyCollection<T>)
			{
				throw new ArgumentException(
					@"Cannot retrieve wrapped persistent collection from ReadOnlyCollection; " +
					@"use ReadOnlyList instead", nameof(collection));
			}

			return collection;
		}

		private void AssertNoExistingTransaction()
		{
			if (SessionManager.CurrentSession != null &&
			    SessionManager.CurrentSession.Transaction.IsActive)
			{
				throw new InvalidOperationException(
					"There is already an active transaction");
			}
		}

		private void Do([NotNull] Action<ISession> procedure,
		                RequiredContext requiredContext,
		                params Entity[] entities)
		{
			Do(procedure, requiredContext, GetAsDetachedState(entities));
		}

		private void Do([NotNull] Action<ISession> procedure,
		                RequiredContext requiredContext,
		                [CanBeNull] IDetachedState detachedState)
		{
			using (ISession session = OpenSession())
			{
				AssertRequiredContext(session, requiredContext);

				detachedState?.ReattachState(this);

				try
				{
					procedure(session);
				}
				catch (Exception e)
				{
					// Roll back, if this is the outermost transaction
					if (session is SessionWrapper sessionWrapper &&
					    sessionWrapper.IsOutermost)
					{
						_msg.Debug("Rolling back nHibernate transaction due to exception.", e);

						session.Transaction.Rollback();
					}

					throw;
				}
			}
		}

		private void DoReadOnlyTransaction([NotNull] Action<ISession> procedure,
		                                   params Entity[] entities)
		{
			DoReadOnlyTransaction(procedure, GetAsDetachedState(entities));
		}

		private void DoReadOnlyTransaction([NotNull] Action<ISession> procedure,
		                                   [CanBeNull] IDetachedState detachedState)
		{
			using (ISession session = OpenSession())
			{
				// Do not set session.DefaultReadOnly = true;
				// otherwise the loaded entities remain readonly forever
				// -> this probably makes sense but would be a change in behaviour

				AssertRequiredContext(session, RequiredContext.Transaction);

				FlushMode origFlushMode = session.FlushMode;

				session.FlushMode = FlushMode.Manual;
				try
				{
					detachedState?.ReattachState(this);

					procedure(session);
				}
				finally
				{
					session.FlushMode = origFlushMode;
					//session.DefaultReadOnly = false;
					session.Transaction.Rollback();
					session.BeginTransaction();
				}
			}
		}

		[CanBeNull]
		private static IDetachedState GetAsDetachedState(params Entity[] entities)
		{
			if (entities == null || entities.Length <= 0)
			{
				return null;
			}

			return new DetachedState(entities);
		}

		private static void ReattachCore([NotNull] ISession session,
		                                 [NotNull] IEnumerable objects)
		{
			foreach (object entity in objects)
			{
				// allow passing null for simple reattachment
				// (allows null check to be skipped in calling code)
				if (entity != null)
				{
					session.Lock(entity, LockMode.None);
				}
			}
		}

		private void AssertRequiredContext([NotNull] ISession session,
		                                   RequiredContext requiredContext)
		{
			switch (requiredContext)
			{
				case RequiredContext.Transaction:
					AssertInTransaction(session);
					break;

				case RequiredContext.LongSessionOrTransaction:
					AssertSessionContextDefined(session);
					break;
			}
		}

		// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
		private void AssertSessionContextDefined([NotNull] ISession session)
		{
			if (SessionManager.CurrentSession == null && ! session.Transaction.IsActive)
			{
				throw new InvalidOperationException(
					"Invalid call outside of either a started unit of work or a transactional context");
			}
		}

		#region Nested type: RequiredContext

		private enum RequiredContext
		{
			LongSessionOrTransaction,
			Transaction
		}

		#endregion
	}
}
