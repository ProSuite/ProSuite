using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.WorkList
{
	public interface IWorkListObserver
	{
		void WorkListAdded(IWorkList workList);

		void WorkListRemoved(IWorkList workList);

		void WorkListModified(IWorkList workList);
	}

	/// <summary>
	/// Administration of all worklists
	/// Pro event subscription and relaying
	/// Single source of truth for the UI
	/// </summary>
	public class WorkListCentral
	{
		private readonly WorkListRegistry _registry;
		private readonly IList<IWorkListObserver> _observers;

		public WorkListCentral()
		{
			_registry = WorkListRegistry.Instance;
			_observers = new List<IWorkListObserver>();
		}

		/* The UI may (un)register itself */

		public void RegsiterObserver(IWorkListObserver observer)
		{
			_observers.Add(observer);
		}

		public void UnregisterObserver(IWorkListObserver observer)
		{
			_observers.Remove(observer);
		}

		public IWorkList GetWorkList(string name)
		{
			return _registry.Get(name);
		}

		public IEnumerable<IWorkList> GetAllLists()
		{
			return _registry.GetAll();
		}

		public void SetWorkList(IWorkList workList)
		{
			_registry.Add(workList);

			// TODO find and show layer, create if missing

			foreach (var observer in _observers)
			{
				observer.WorkListAdded(workList);
			}
		}

		public void ShowWorkList(IWorkList workList)
		{
			// TODO find and show layer, create it if missing
		}

		public void HideWorkList(IWorkList workList)
		{
			// TODO find and hide layer
		}

		public void DestroyWorkList(IWorkList workList)
		{
			// TODO find and remove layer(s) in all maps

			_registry.Remove(workList);

			workList.Dispose();
		}

		// On project save: persist state to .aprx (all work lists and central state)
		// On project load: load state from .aprx (central state and all work lists), notify UI
		// On features created/modified/deleted: update lists where necessary, notify UI
		// On undo/redo: update lists where necessary, notify UI
	}
}
