using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using NHibernate;
using NHibernate.Cfg;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Orm.NHibernate
{
	/// <summary>
	/// Session provider implementation that directly uses the NHibernate session factory
	/// and directly begins a transaction when opened.
	/// </summary>
	[UsedImplicitly]
	public class SessionProvider : ISessionProvider
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		// Session factory is thread-safe and global (maintains 2nd level cache)
		private static ISessionFactory _sessionFactory;

		private readonly string _sessionFactoryErrorMessage = "Session factory is null";

		// The session is NOT thread safe
		private ThreadLocal<ISession> _currentSession;

		public SessionProvider() { }

		public SessionProvider(INHConfigurationBuilder configBuilder)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			try
			{
				Configuration configuration = configBuilder.GetConfiguration();

				_sessionFactory = configuration.BuildSessionFactory();

				_msg.DebugStopTiming(
					watch, "Successfully configured NHibernate and created session factory.");
			}
			catch (Exception e)
			{
				// Do not throw - in some applications the DDX is optional (such as field admin).
				_msg.Debug("Failed to create NHibernate session factory.", e);

				_sessionFactoryErrorMessage += ! string.IsNullOrEmpty(e.Message)
					                               ? $": {e.Message}"
					                               : ": Unknown error";
			}
		}

		public ISession OpenSession(bool withoutTransaction)
		{
			Assert.NotNull(_sessionFactory, _sessionFactoryErrorMessage);

			if (CurrentSession != null && CurrentSession.IsOpen)
			{
				throw new InvalidOperationException(
					"Cannot open a new session when there is an existing one");
			}

			_currentSession.Value = _sessionFactory.OpenSession();

			if (! withoutTransaction)
			{
				_currentSession.Value.BeginTransaction();
			}

			return CurrentSession;
		}

		/// <summary>
		/// Creates a session wrapper that commits on Dispose() if it was the first to create
		/// the NHibernate session. This allows for 'nested' transactions where the outer-most
		/// does the commit while the inner sessions piggy-back onto the same underlying NH-session.
		/// Hence, the commit-on-dispose is done if
		/// - The session wrapper was the first to be created
		/// - The session's <see cref="ISession.DefaultReadOnly"/> is false
		/// - The transaction is still active
		/// </summary>
		/// <returns></returns>
		public ISession CreateDisposableSession()
		{
			ISession nHibernateSession;

			bool isOutermost;

			if (CurrentSession == null)
			{
				nHibernateSession = OpenSession(false);
				isOutermost = true;
			}
			else
			{
				// Use existing session an make sure it is not disposed (by wrapping it)
				nHibernateSession = CurrentSession;
				isOutermost = false;
			}

			return new SessionWrapper(nHibernateSession, isOutermost);
		}

		public ISession CurrentSession
		{
			get
			{
				if (_currentSession == null)
				{
					_currentSession = new ThreadLocal<ISession>(() => null);
				}

				if (! _currentSession.IsValueCreated)
				{
					return null;
				}

				if (_currentSession.Value != null && ! _currentSession.Value.IsOpen)
				{
					_currentSession.Value = null;
				}

				return _currentSession.Value;
			}
		}

		public bool Configured => _sessionFactory != null;

		/// <summary>
		/// An idea for a pattern that could be used e.g. by a domain transaction manager.
		/// </summary>
		/// <param name="proc"></param>
		/// <param name="withoutTransaction"></param>
		public void WithOpenSession(Action<ISession> proc,
		                            bool withoutTransaction = false)
		{
			ISession openedSession = null;
			if (CurrentSession == null)
			{
				openedSession = OpenSession(withoutTransaction);
			}

			ISession session = openedSession ?? CurrentSession;

			try
			{
				proc(session);
			}
			finally
			{
				openedSession?.Dispose();
			}
		}
	}
}
