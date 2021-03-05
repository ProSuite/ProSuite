using NHibernate;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	/// <summary>
	/// Provides access to the (single) currently active NHibernate session and allows opening
	/// a new session.
	/// </summary>
	public interface ISessionProvider
	{
		/// <summary>
		/// Creates a new NHibernate session, or throws an exception if there is already a current
		/// session.
		/// </summary>
		/// <param name="withoutTransaction">If true, no transaction is begun.</param>
		/// <returns>The newly session from the NHibernate session factory.</returns>
		ISession OpenSession(bool withoutTransaction = false);

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
		ISession CreateDisposableSession();

		/// <summary>
		/// The currently active session, or null, if no session is currently open.
		/// </summary>
		[CanBeNull]
		ISession CurrentSession { get; }

		/// <summary>
		/// Whether this instance is configured with a valid session factory.
		/// </summary>
		bool Configured { get; }
	}
}
