using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
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

		protected override string FileSuffix => ".iwl";

		protected override string GetDisplayName()
		{
			return WorkListItemDatastore.SuggestWorkListName();
		}

		public Geometry AreaOfInterest { get; set; }

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

#if ARCGISPRO_GREATER_3_2
			qaGroupLayer.SetShowLayerAtAllScales(true);
#endif

			// Expected behaviour:
			// - They should be re-nameable by the user.
			// - They should be deletable by the user (in which case a new layer should be re-added)
			// - If the layer is moved outside the group a new layer should be added. Only layers within the
			//   sub-group are considered to be part of the work list.
			string groupName = GetDisplayName(); // _workListItemDatastore.SuggestWorkListGroupName();
			if (groupName != null)
			{
				GroupLayer workListGroupLayer = qaGroupLayer.FindLayers(groupName)
				                                            .OfType<GroupLayer>().FirstOrDefault();

				if (workListGroupLayer == null)
				{
					_msg.DebugFormat("Creating new group layer {0}", groupName);
					workListGroupLayer =
						LayerFactory.Instance.CreateGroupLayer(qaGroupLayer, 0, groupName);

#if ARCGISPRO_GREATER_3_2
					workListGroupLayer.SetShowLayerAtAllScales(true);
#endif
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

		protected override async Task<IWorkItemRepository> CreateItemRepositoryCoreAsync(
			IWorkItemStateRepository stateRepository)
		{
			var tables = await PrepareReferencedTables();

			var sourceClassDefinitions = new List<DbStatusSourceClassDefinition>(tables.Count);

			Stopwatch watch = Stopwatch.StartNew();

			// TODO: Make attribute reader more generic, use AttributeRoles
			Attributes[] attributes = new[]
			                          {
				                          Attributes.QualityConditionName,
				                          Attributes.IssueCodeDescription,
				                          Attributes.InvolvedObjects,
				                          Attributes.IssueSeverity,
				                          Attributes.IssueCode,
				                          Attributes.IssueDescription,
				                          Attributes.IssueType
			                          };

			foreach (Table table in tables)
			{
				string defaultDefinitionQuery = GetDefaultDefinitionQuery(table);

				TableDefinition tableDefinition = table.GetDefinition();

				WorkListStatusSchema statusSchema =
					WorkListItemDatastore.CreateStatusSchema(tableDefinition);

				IAttributeReader attributeReader =
					WorkListItemDatastore.CreateAttributeReader(tableDefinition, attributes);

				var sourceClassDef =
					new DbStatusSourceClassDefinition(table, defaultDefinitionQuery, statusSchema)
					{
						AttributeReader = attributeReader
					};

				sourceClassDefinitions.Add(sourceClassDef);
			}

			var result = new DbStatusWorkItemRepository(sourceClassDefinitions, stateRepository);

			_msg.DebugStopTiming(watch, "Created revision work item repository");

			return result;
		}
	}
}
