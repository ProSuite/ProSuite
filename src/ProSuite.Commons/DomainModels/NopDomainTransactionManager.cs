using System;
using System.Collections.Generic;

namespace ProSuite.Commons.DomainModels
{
	public class NopDomainTransactionManager : IDomainTransactionManager
	{
		public void UseTransaction(Action procedure)
		{
			procedure();
		}

		public void UseTransaction(ReattachStateOption reattachStateOption, Action procedure)
		{
			procedure();
		}

		public void NewTransaction(Action procedure)
		{
			procedure();
		}

		public void NewTransaction(ReattachStateOption reattachStateOption, Action procedure)
		{
			procedure();
		}

		public T ReadOnlyTransaction<T>(Func<T> function)
		{
			return function();
		}

		public void Reattach<T>(ICollection<T> collection) where T : class { }

		public void Reattach(Entity entity) { }

		public void ReattachState(IDetachedState detachedState) { }

		public void ReattachForUpdate(Entity entity) { }

		public void Initialize<T>(ICollection<T> collection) where T : class { }

		public bool IsInitialized<T>(ICollection<T> collection) where T : class
		{
			return true;
		}
	}
}
