using System.Collections.Generic;
using System.Linq;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Trial
{
	public abstract class Repository<T, T0> : IRepository<T> where T : IWorkItemState
	                                                         where T0 : IWorkListDefinition<T>
	{
		private List<T> _states;
		private List<int> _oids;

		private IEnumerable<T> States
		{
			get
			{
				if (_states == null)
				{
					_states = ReadStates();
				}

				return _states;
			}
		}

		private List<int> Oids
		{
			get
			{
				if (_oids == null)
				{
					// todo daro: only store changed (visited, done) states for lookup?
					//_oids = States.Where(item => item.Visited).Select(item => item.OID).OrderBy(oid => oid).ToList();
					//_oids = States.Select(item => item.OID).OrderBy(oid => oid).ToList();
					Refresh();
				}
				return _oids;
			}
		}

		public IWorkItem Refresh(IWorkItem item)
		{
			T state = Lookup(item);

			if (state == null)
			{
				return item;
			}

			item.Visited = state.Visited;

			return RefreshCore(item, state);
		}

		public void UpdateVolatileState(IEnumerable<IWorkItem> items)
		{
			foreach (IWorkItem item in items)
			{
				Update(item);
			}

			Refresh();
		}

		public void Commit()
		{
			Store(CreateDefinition(_states));

			Refresh();
		}

		public void Discard()
		{
			Invalidate();
		}

		protected abstract void Store(T0 definition);

		protected abstract T0 CreateDefinition(List<T> states);

		protected abstract T CreateState(IWorkItem item);

		protected abstract List<T> ReadStates();

		protected virtual IWorkItem RefreshCore([NotNull] IWorkItem item, [NotNull] T state)
		{
			return item;
		}

		protected virtual void UpdateCore(T state, IWorkItem item) { }

		[CanBeNull]
		private T Lookup([NotNull] IWorkItem item)
		{
			if (Oids == null)
			{
				return default;
			}

			int index = Oids.BinarySearch(item.OID);
			if (index < 0)
			{
				return default;
			}

			// todo daro: inline
			T result = _states[index];
			return result;
		}

		private void Update([NotNull] IWorkItem item)
		{
			T state = Lookup(item);

			if (state == null)
			{
				// todo daro: revise
				// create new state if it doesn't exist
				state = CreateState(item);
				_states.Add(state);
			}

			//if (Modified(item))
			//{
			//	state.Visited = item.Visited;
			//	_states.Add(state);
			//}

			state.Visited = item.Visited;

			UpdateCore(state, item);
		}

		private void Refresh()
		{
			_oids = States.Select(item => item.OID).OrderBy(oid => oid).ToList();
		}

		private void Invalidate()
		{
			_states = null;
			_oids = null;
		}
	}
}
