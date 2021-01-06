using System;
using System.Reflection;
using NHibernate;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Orm.NHibernate
{
	public abstract class NHibernateUnitOfWorkBase
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

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
