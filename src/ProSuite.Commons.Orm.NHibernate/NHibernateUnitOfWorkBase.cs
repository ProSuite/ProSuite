using System;
using NHibernate;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	public abstract class NHibernateUnitOfWorkBase
	{
		[UsedImplicitly]
		public ISessionProvider SessionManager { set; get; }

		[NotNull]
		protected ISession OpenSession(bool requireTransaction = false)
		{
			ISession session = null;

			try
			{
				session = SessionManager.CreateDisposableSession();

				if (requireTransaction)
				{
					AssertInTransaction(session);
				}

				return session;
			}
			catch
			{
				session?.Dispose();

				throw;
			}
		}

		protected static void AssertInTransaction(ISession session)
		{
			if (! session.Transaction.IsActive)
			{
				throw new InvalidOperationException(
					"Must be called within a database transaction");
			}
		}
	}
}
