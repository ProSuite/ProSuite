using System;
using System.Collections;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	/// <summary>
	/// Interface for controlling a "unit of work" (Fowler PEAA), i.e. 
	/// a set of related changes to the domain model. Also provides access
	/// to transactionally writing back those changes.
	/// </summary>
	public interface IUnitOfWork
	{
		void Start();

		bool Started { get; }

		void Stop();

		void Flush();

		void Reset();

		bool HasChanges { get; }

		void Persist(params Entity[] entities);

		/// <summary>
		/// Executes a procedure in a new transaction, but does not commit 
		/// the transaction, instead the transaction is rolled back after the procedure.
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		void ReadOnlyTransaction([NotNull] Action procedure);

		/// <summary>
		/// Executes a function in a new transaction and returns its result, but does not commit 
		/// the transaction, instead the transaction is rolled back after the procedure.
		/// </summary>
		/// <param name="function">The function.</param>
		T ReadOnlyTransaction<T>([NotNull] Func<T> function);

		/// <summary>
		/// Executes a procedure in a new transaction, but does not commit 
		/// the transaction, instead the transaction is rolled back after the procedure.
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		/// <param name="entities">The entities that need to be reattached to 
		/// the session.</param>
		void ReadOnlyTransaction([NotNull] Action procedure, params Entity[] entities);

		void ReadOnlyTransaction([CanBeNull] IDetachedState detachedState,
		                         [NotNull] Action procedure);

		/// <summary>
		/// Executes a procedure in a transaction, reusing an existing 
		/// transaction if it exists.
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		void UseTransaction([NotNull] Action procedure);

		/// <summary>
		/// Executes a function in a transaction, reusing an existing 
		/// transaction if it exists, and returns the function result
		/// </summary>
		/// <param name="function">The function.</param>
		T UseTransaction<T>([NotNull] Func<T> function);

		/// <summary>
		/// Executes a procedure in a transaction, reusing an existing 
		/// transaction if it exists.
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		/// <param name="entities">The entities that need to be reattached to 
		/// the session for the transaction to succeed. The entities are expected
		/// not to have been modified outside the session (those changes would not 
		/// be flushed as part of the transaction). To attach modified entites for 
		/// update, use <see cref="ReattachForUpdate"></see> within the procedure.</param>
		void UseTransaction([NotNull] Action procedure, params Entity[] entities);

		void UseTransaction([CanBeNull] IDetachedState detachedState,
		                    [NotNull] Action procedure);

		/// <summary>
		/// Executes a procedure in a new transaction.
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		void NewTransaction([NotNull] Action procedure);

		/// <summary>
		/// Executes a function in a new transaction and returns its result
		/// </summary>
		/// <param name="function">The procedure.</param>
		T NewTransaction<T>([NotNull] Func<T> function);

		/// <summary>
		/// Executes a procedure in a new transaction.
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		/// <param name="entities">The entities that need to be reattached to 
		/// the session for the transaction to succeed. The entities are expected
		/// not to have been modified outside the session (those changes would not 
		/// be flushed as part of the transaction). To attach modified entites for 
		/// update, use <see cref="ReattachForUpdate"></see> within the procedure.</param>
		void NewTransaction([NotNull] Action procedure, params Entity[] entities);

		void NewTransaction([CanBeNull] IDetachedState detachedState,
		                    [NotNull] Action procedure);

		void Reattach<T>([NotNull] ICollection<T> collection) where T : class;

		void Reattach(params Entity[] entities);

		void ReattachForUpdate(params Entity[] entities);

		void ReattachWithVersionCheck(params Entity[] entities);

		void LockForUpdate(params Entity[] entities);

		void Detach([NotNull] Entity entity);

		bool IsInitialized<T>([NotNull] ICollection<T> collection) where T : class;

		void Initialize<T>([NotNull] ICollection<T> collection) where T : class;

		void Initialize([NotNull] ICollection collection);

		void Initialize([NotNull] Entity entity);

		void Commit();

		Entity Merge(Entity entity);
	}
}