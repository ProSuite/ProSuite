using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.QA.WorkList
{
	public abstract class IssueWorkListEnvironmentBase : DbWorkListEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected IssueWorkListEnvironmentBase(
			[CanBeNull] IWorkListItemDatastore workListItemDatastore)
			: base(workListItemDatastore) { }

		protected IssueWorkListEnvironmentBase([CanBeNull] string path)
			: base(new FileGdbIssueWorkListItemDatastore(path)) { }

		public override string FileSuffix => ".iwl";

		public Geometry AreaOfInterest { get; set; }

		protected override string SuggestWorkListName()
		{
			return WorkListItemDatastore.SuggestWorkListName();
		}

		protected override string SuggestWorkListLayerName()
		{
			return "Issue Work List";
		}

		public override void RemoveAssociatedLayers()
		{
			RemoveFromMapCore(GetTablesCore());
		}

		protected override T GetLayerContainerCore<T>()
		{
			var qaGroupLayerName = "QA";

			GroupLayer qaGroupLayer = MapView.Active.Map.FindLayers(qaGroupLayerName)
			                                 .OfType<GroupLayer>().FirstOrDefault();

			if (qaGroupLayer == null)
			{
				_msg.DebugFormat("Creating new group layer {0}", qaGroupLayerName);
				qaGroupLayer = LayerFactory.Instance.CreateGroupLayer(
					MapView.Active.Map, 0, qaGroupLayerName);
			}

			// Expected behaviour:
			// - They should be re-nameable by the user.
			// - They should be deletable by the user (in which case a new layer should be re-added)
			// - If the layer is moved outside the group a new layer should be added. Only layers within the
			//   sub-group are considered to be part of the work list.
			string groupName = DisplayName; // _workListItemDatastore.SuggestWorkListGroupName();
			if (groupName != null)
			{
				GroupLayer workListGroupLayer = qaGroupLayer.FindLayers(groupName)
				                                            .OfType<GroupLayer>().FirstOrDefault();

				if (workListGroupLayer == null)
				{
					_msg.DebugFormat("Creating new group layer {0}", groupName);
					workListGroupLayer =
						LayerFactory.Instance.CreateGroupLayer(qaGroupLayer, 0, groupName);
				}

				return workListGroupLayer as T;
			}

			return qaGroupLayer as T;
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository,
		                                                string uniqueName,
		                                                string displayName)
		{
			return new IssueWorkList(repository, uniqueName, AreaOfInterest, displayName);
		}

		protected override IWorkItemStateRepository CreateStateRepositoryCore(
			string path, string workListName)
		{
			Type type = GetWorkListTypeCore<IssueWorkList>();

			return new XmlWorkItemStateRepository(path, workListName, type);
		}

		protected override IWorkItemRepository CreateItemRepositoryCore(
			IList<Table> tables, IWorkItemStateRepository stateRepository)
		{
			Stopwatch watch = Stopwatch.StartNew();

			var sourceClasses = new List<Tuple<Table, string>>();

			foreach (Table table in tables)
			{
				string defaultDefinitionQuery = GetDefaultDefinitionQuery(table);

				sourceClasses.Add(Tuple.Create(table, defaultDefinitionQuery));
			}

			var result =
				new IssueItemRepository(sourceClasses, stateRepository, WorkListItemDatastore);

			_msg.DebugStopTiming(watch, "Created issue work item repository");

			return result;
		}
	}
}
