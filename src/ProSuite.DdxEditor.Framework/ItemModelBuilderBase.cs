using System;
using System.Collections;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework
{
	public abstract class ItemModelBuilderBase : IItemModelBuilder
	{
		private readonly IUnitOfWork _unitOfWork;

		// TODO: use more specific IDomainTransaction interface
		/// <summary>
		/// Initializes a new instance of the <see cref="ItemModelBuilderBase"/> class.
		/// </summary>
		/// <param name="unitOfWork">The unit of work.</param>
		protected ItemModelBuilderBase([NotNull] IUnitOfWork unitOfWork)
		{
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			_unitOfWork = unitOfWork;
		}

		#region IItemModelBuilder Members

		public IList<Item> GetRootItems()
		{
			var result = new List<Item>();

			CollectRootItems(result);

			return result;
		}

		#endregion

		public void FlushUnitOfWork()
		{
			_unitOfWork.Flush();
		}

		public void DiscardUnitOfWork()
		{
			_unitOfWork.Reset();
		}

		public void Reattach(params Entity[] entities)
		{
			Assert.ArgumentNotNull(entities, nameof(entities));

			_unitOfWork.Reattach(entities);
		}

		/// <summary>
		/// Executes a procedure in a transaction. If there already exists a 
		/// transaction, that transaction will be used instead of creating a
		/// new transaction.
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		/// <remarks>
		/// <ul>
		/// <li>Any pending changes that may be present in an already ongoing unit of work
		/// will be written to the database, even if the procedure only reads data.</li>
		/// <li>If the transaction fails, the unit of work is <b>not</b> discarded, as 
		/// this may have to be done by a caller (since the transaction context also
		/// may be inherited from a caller).</li>
		/// </ul></remarks>
		public void UseTransaction([NotNull] Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			_unitOfWork.UseTransaction(procedure);

			_unitOfWork.Commit();
		}

		/// <summary>
		/// Executes a function in a transaction and returns its result. If there already exists a 
		/// transaction, that transaction will be used instead of creating a
		/// new transaction.
		/// </summary>
		/// <param name="function">The function.</param>
		/// <remarks>
		/// <ul>
		/// <li>Any pending changes that may be present in an already ongoing unit of work
		/// will be written to the database, even if the procedure only reads data.</li>
		/// <li>If the transaction fails, the unit of work is <b>not</b> discarded, as 
		/// this may have to be done by a caller (since the transaction context also
		/// may be inherited from a caller).</li>
		/// </ul></remarks>
		public T UseTransaction<T>([NotNull] Func<T> function)
		{
			Assert.ArgumentNotNull(function, nameof(function));

			T result = _unitOfWork.UseTransaction(function);

			_unitOfWork.Commit();

			return result;
		}

		/// <summary>
		/// Execute a procedure in a read-only transaction, which is guaranteed to 
		/// not flush any pending changes from the session.
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		public void ReadOnlyTransaction([NotNull] Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			_unitOfWork.ReadOnlyTransaction(procedure);
		}

		/// <summary>
		/// Execute a function in a read-only transaction, which is guaranteed to 
		/// not flush any pending changes from the session.
		/// </summary>
		/// <param name="function">The function.</param>
		public T ReadOnlyTransaction<T>([NotNull] Func<T> function)
		{
			Assert.ArgumentNotNull(function, nameof(function));

			return _unitOfWork.ReadOnlyTransaction(function);
		}

		/// <summary>
		/// Executes a procedure in a new transaction. If there already exists a transaction,
		/// commit will fail. 
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		/// <remarks>If the transaction fails, the unit of work is discarded, i.e. all
		/// pending changes are lost. Make sure to only use this method when there are
		/// no existing pending changes.</remarks>
		public void NewTransaction([NotNull] Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			Func<bool> function = () =>
			{
				procedure();
				return true;
			};

			NewTransaction(function);
		}

		/// <summary>
		/// Executes a function in a new transaction and returns its result. 
		/// If there already exists a transaction, commit will fail. 
		/// </summary>
		/// <param name="function">The function.</param>
		/// <remarks>If the transaction fails, the unit of work is discarded, i.e. all
		/// pending changes are lost. Make sure to only use this method when there are
		/// no existing pending changes.</remarks>
		public T NewTransaction<T>([NotNull] Func<T> function)
		{
			Assert.ArgumentNotNull(function, nameof(function));

			// Work-around for new transaction manager - unitOfWork.Start always starts a transaction!

			bool sessionStarted = _unitOfWork.Started;

			try
			{
				if (sessionStarted)
				{
					if (_unitOfWork.HasChanges)
					{
						throw new InvalidOperationException(
							"There are existing changes in an existing transaction");
					}

					//_unitOfWork.Stop();
				}

				T result = function();

				_unitOfWork.Commit();

				return result;
				//return _unitOfWork.NewTransaction(function);
			}
			catch (Exception)
			{
				_unitOfWork.Reset();
				throw;
			}
			finally
			{
				if (sessionStarted && ! _unitOfWork.Started)
				{
					_unitOfWork.Start();
				}
			}
		}

		public void Initialize<T>([NotNull] ICollection<T> collection) where T : class
		{
			_unitOfWork.Initialize(collection);
		}

		public void Initialize([NotNull] ICollection collection)
		{
			_unitOfWork.Initialize(collection);
		}

		protected abstract void CollectRootItems([NotNull] IList<Item> rootItems);
	}
}
