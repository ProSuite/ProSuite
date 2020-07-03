using System;
using System.Collections.Generic;

namespace ProSuite.Commons.DomainModels
{
	/// <summary>
	/// Methods to control transactions on domain objects
	/// </summary>
	public interface IDomainTransactionManager
	{
		void UseTransaction(Action procedure);

		void UseTransaction(ReattachStateOption reattachStateOption, Action procedure);

		void NewTransaction(Action procedure);

		void NewTransaction(ReattachStateOption reattachStateOption, Action procedure);

		void Reattach<T>(ICollection<T> collection) where T : class;

		void Reattach(Entity entity);

		void ReattachState(IDetachedState detachedState);

		void ReattachForUpdate(Entity entity);

		void Initialize<T>(ICollection<T> collection) where T : class;

		bool IsInitialized<T>(ICollection<T> collection) where T : class;
	}
}