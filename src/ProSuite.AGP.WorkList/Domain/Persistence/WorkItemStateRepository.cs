using System.Collections.Generic;
using System.Linq;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain.Persistence
{
	public abstract class WorkItemStateRepository<TState, TDefinition> : IRepository
		where TState : IWorkItemState
		where TDefinition : IWorkListDefinition<TState>
	{
		private List<TState> _states;
		private List<int> _oids;

		private IEnumerable<TState> States
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
			TState state = Lookup(item);

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

		protected abstract void Store(TDefinition definition);

		protected abstract TDefinition CreateDefinition(List<TState> states);

		protected abstract TState CreateState(IWorkItem item);

		protected abstract List<TState> ReadStates();

		protected virtual IWorkItem RefreshCore([NotNull] IWorkItem item, [NotNull] TState state)
		{
			return item;
		}

		protected virtual void UpdateCore(TState state, IWorkItem item) { }

		[CanBeNull]
		private TState Lookup([NotNull] IWorkItem item)
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
			TState result = _states[index];
			return result;
		}

		private void Update([NotNull] IWorkItem item)
		{
			TState state = Lookup(item);

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
