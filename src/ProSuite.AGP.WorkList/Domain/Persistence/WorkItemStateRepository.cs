using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Domain.Persistence
{
	public abstract class WorkItemStateRepository<TState, TDefinition> : IWorkItemStateRepository
		where TState : IWorkItemState
		where TDefinition : IWorkListDefinition<TState>
	{
		private readonly IMsg _msg = Msg.ForCurrentClass();

		protected string Name { get; }
		protected string DisplayName { get; }
		protected Type Type { get; }

		public string WorkListDefinitionFilePath { get; set; }

		private readonly IDictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>>
			_workspaces = new Dictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>>();

		protected WorkItemStateRepository(string name, string displayName, Type type,
		                                  int? currentItemIndex)
		{
			Name = name;
			DisplayName = displayName;
			Type = type;
			CurrentIndex = currentItemIndex;

			ReadStatesByRow();
		}

		protected IDictionary<GdbObjectReference, TState> StatesByRow { get; set; }

		public virtual void Rename(string name) { }

		public int? CurrentIndex { get; set; }

		public void Refresh(IWorkItem item)
		{
			TState state = Lookup(item);

			if (state == null)
			{
				return;
			}

			item.Visited = state.Visited;

			RefreshCore(item, state);
		}

		public void UpdateState([NotNull] IWorkItem item)
		{
			TState state = Lookup(item);

			if (state == null)
			{
				// create new state if it doesn't exist
				state = CreateState(item);

				var gdbObjectReference =
					new GdbObjectReference(item.UniqueTableId, item.ObjectID);

				StatesByRow.Add(gdbObjectReference, state);
			}

			GdbTableIdentity table = item.GdbRowProxy.Table;
			GdbWorkspaceIdentity workspace = table.Workspace;

			if (_workspaces.TryGetValue(workspace, out SimpleSet<GdbTableIdentity> tables))
			{
				tables.TryAdd(table);
			}
			else
			{
				_workspaces.Add(workspace, new SimpleSet<GdbTableIdentity> { table });
			}

			state.Visited = item.Visited;

			UpdateStateCore(state, item);
		}

		public void Commit(IList<ISourceClass> sourceClasses, Envelope extent)
		{
			Assert.NotNull(StatesByRow,
			               "Work item states have never been read, failed to read or have already been discarded");

			if (_workspaces.Count == 0 && StatesByRow.Count > 0)
			{
				_msg.Debug(
					$"{Name}: Invalid work list (one or more referenced tables could not be loaded) will not be stored.");
				return;
			}

			TDefinition workListDefinition =
				CreateDefinition(_workspaces, sourceClasses, StatesByRow.Values,
				                 extent);

			Store(workListDefinition);
		}

		protected abstract void Store(TDefinition definition);

		protected abstract TDefinition CreateDefinition(
			IDictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> tablesByWorkspace,
			IList<ISourceClass> sourceClasses,
			IEnumerable<TState> states,
			Envelope extent);

		protected abstract TState CreateState(IWorkItem item);

		protected void ReadStatesByRow()
		{
			ReadStatesByRowCore();
		}

		protected virtual void ReadStatesByRowCore()
		{
			StatesByRow = new Dictionary<GdbObjectReference, TState>();
		}

		protected virtual void RefreshCore([NotNull] IWorkItem item, [NotNull] TState state) { }

		protected virtual void UpdateStateCore(TState state, IWorkItem item) { }

		[CanBeNull]
		private TState Lookup([NotNull] IWorkItem item)
		{
			// NOTE: The look-up by Index (the OID is just a incrementing number) is incorrect if the
			// rows change the order or some are deleted or if the items are re-read in the same session.
			// -> Look-up must be by GdbObjectReference rather than by Item Id.

			var objectReference = new GdbObjectReference(item.UniqueTableId, item.ObjectID);

			if (! StatesByRow.TryGetValue(objectReference, out TState volatileState))
			{
				return default;
			}

			return volatileState;
		}
	}
}
