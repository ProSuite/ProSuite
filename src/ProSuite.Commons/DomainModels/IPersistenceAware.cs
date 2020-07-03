namespace ProSuite.Commons.DomainModels
{
	/// <summary>
	/// Interface for declaring a nhibernate entity class to be persistence aware.
	/// If an entity implements this interface <b>and</b> there is a nhibernate
	/// interceptor configured for the session factory which makes use of that interface
	/// (like the NHibernateInterceptor in ProSuite.Commons.Orm.NHibernate), 
	/// then the entity is notified prior to the individual persistence operations.
	/// </summary>
	public interface IPersistenceAware
	{
		/// <summary>
		/// Called when an entity is about to be first saved to the database.
		/// </summary>
		void OnCreate();

		/// <summary>
		/// Called when an entity is about to be updated in the database.
		/// </summary>
		void OnUpdate();

		/// <summary>
		/// Called when an entity is about to be deleted in the database.
		/// </summary>
		void OnDelete();
	}
}
