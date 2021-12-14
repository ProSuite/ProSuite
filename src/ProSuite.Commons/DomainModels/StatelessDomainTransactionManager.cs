using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public class StatelessDomainTransactionManager : IDomainTransactionManager
	{
		[NotNull] private readonly IUnitOfWork _unitOfWork;

		/// <summary>
		/// Initializes a new instance of the <see cref="StatelessDomainTransactionManager"/> class.
		/// </summary>
		public StatelessDomainTransactionManager([NotNull] IUnitOfWork unitOfWork)
		{
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			_unitOfWork = unitOfWork;
		}

		public void UseTransaction(Action procedure)
		{
			_unitOfWork.UseTransaction(procedure);
		}

		public void UseTransaction(ReattachStateOption reattachStateOption, Action procedure)
		{
			_unitOfWork.UseTransaction(procedure);
		}

		public void NewTransaction(Action procedure)
		{
			_unitOfWork.NewTransaction(procedure);
		}

		public void NewTransaction(ReattachStateOption reattachStateOption, Action procedure)
		{
			_unitOfWork.NewTransaction(procedure);
		}

		public T ReadOnlyTransaction<T>(Func<T> function)
		{
			return _unitOfWork.ReadOnlyTransaction(function);
		}

		public void Reattach<T>(ICollection<T> collection) where T : class
		{
			_unitOfWork.Reattach(collection);
		}

		public void Reattach(Entity entity)
		{
			_unitOfWork.Reattach(entity);
		}

		public void ReattachState(IDetachedState detachedState)
		{
			Assert.ArgumentNotNull(detachedState, nameof(detachedState));

			detachedState.ReattachState(_unitOfWork);
		}

		public void ReattachForUpdate(Entity entity)
		{
			_unitOfWork.ReattachForUpdate(entity);
		}

		public void Initialize<T>(ICollection<T> collection) where T : class
		{
			_unitOfWork.Initialize(collection);
		}

		public bool IsInitialized<T>(ICollection<T> collection) where T : class
		{
			return _unitOfWork.IsInitialized(collection);
		}
	}
}
