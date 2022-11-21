using System;
using NHibernate;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	public abstract class NHibernateService
	{
		[UsedImplicitly]
		public ISessionProvider SessionManager { set; get; }

		// TODO: Change signature:
		// - OpenDisposableSession() -> used in special cases, auth-service, etc.
		// - GetCurrentSession -> used in repos where a session is expected to exist
		//   because the call is wrapped inside a TransactionManager's transaction
		[NotNull]
		protected ISession OpenSession(bool requireTransaction = false)
		{
			Assert.NotNull(SessionManager, "No session provider");

			return SessionManager.CreateDisposableSession();
		}

		[CanBeNull]
		protected Version GetDatabaseSchemaVersion()
		{
			return SessionManager.KnownSchemaVersion;
		}

		protected static void AssertInTransaction(ISession session)
		{
			Assert.True(session.Transaction.IsActive, "No active transaction");
		}
	}
}
