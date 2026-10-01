using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.QA.WorkList
{
	public abstract class IssueWorkListEnvironmentBase : DbWorkListEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _qaGroupLayerName = "QA";

		// TODO: (daro) create different Environments for IssueWorkList and ErrorWorkList and...
		protected IssueWorkListEnvironmentBase(
			[NotNull] IWorkListItemDatastore workListItemDatastore) :
			base(workListItemDatastore) { }

		protected override string FileSuffix => ".iwl";

		public override string GetDisplayName()
		{
			return WorkListItemDatastore.SuggestWorkListName();
		}

		protected override string SuggestWorkListLayerName()
		{
			return "Issue Work List";
		}

		public override void RemoveAssociatedLayers(MapView mapView)
		{
			RemoveFromMapCore(mapView, GetTablesCore());
		}

		/// <summary>
		/// Loads the work list group layer containing the work list layer and the issue layers
		/// in a single step. In an edit session, Pro synchronizes the editing templates of all
		/// open maps for every added map member and for most property changes of a layer.
		/// Adding the layers one by one and changing their properties afterwards results in
		/// dozens of template synchronizations that block the application for a long time.
		/// </summary>
		public override void LoadLayers(MapView mapView, IWorkList worklist,
		                                string workListDefinitionFilePath)
		{
			Map map = mapView.Map;

			GroupLayer qaGroupLayer = FindQaGroupLayer(map);

			string groupName = GetDisplayName();

			if (groupName == null ||
			    qaGroupLayer?.FindLayers(groupName).OfType<GroupLayer>().Any() == true)
			{
				// The work list group layer exists already, add the missing layers into it:
				base.LoadLayers(mapView, worklist, workListDefinitionFilePath);
				return;
			}

			_msg.DebugFormat("Creating group layer {0} with work list and issue layers", groupName);

			var layerDefinitions = new List<CIMDefinition>();
			var tableDefinitions = new List<CIMDefinition>();

			// The issue layers on top, the work list layer at the bottom:
			foreach (Table table in GetTablesCore().OrderBy(GetDrawingOrder))
			{
				CIMDefinition definition = CreateAssociatedMapMemberDefinition(table, worklist);

				if (definition is CIMStandaloneTable)
				{
					tableDefinitions.Add(definition);
				}
				else
				{
					layerDefinitions.Add(definition);
				}
			}

			layerDefinitions.Add(
				CreateWorkListLayerDefinition(worklist, workListDefinitionFilePath));

			CIMGroupLayer workListGroupLayer = CreateGroupLayerDefinition(
				groupName,
				layerDefinitions.Select(definition => definition.URI).ToList(),
				tableDefinitions.Select(definition => definition.URI).ToList());

			layerDefinitions.Add(workListGroupLayer);

			CIMBaseLayer topLayer = workListGroupLayer;
			ILayerContainerEdit layerContainer = qaGroupLayer;

			if (qaGroupLayer == null)
			{
				CIMGroupLayer qaGroupLayerDefinition =
					CreateGroupLayerDefinition(_qaGroupLayerName, new[] { workListGroupLayer.URI });

				layerDefinitions.Add(qaGroupLayerDefinition);

				topLayer = qaGroupLayerDefinition;
				layerContainer = map;
			}

			CreateLayer(topLayer, layerDefinitions, tableDefinitions, layerContainer);
		}

		protected override T GetLayerContainerCore<T>(MapView mapView)
		{
			var qaGroupLayerName = _qaGroupLayerName;

			GroupLayer qaGroupLayer = FindQaGroupLayer(mapView.Map);

			if (qaGroupLayer == null)
			{
				_msg.DebugFormat("Creating new group layer {0}", qaGroupLayerName);
				qaGroupLayer = LayerFactory.Instance.CreateGroupLayer(
					mapView.Map, 0, qaGroupLayerName);
			}

#if ARCGISPRO_GREATER_3_2
			qaGroupLayer.SetShowLayerAtAllScales(true);
#endif

			// Expected behaviour:
			// - They should be re-nameable by the user.
			// - They should be deletable by the user (in which case a new layer should be re-added)
			// - If the layer is moved outside the group a new layer should be added. Only layers within the
			//   sub-group are considered to be part of the work list.
			string groupName = GetDisplayName();

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

		[CanBeNull]
		private static GroupLayer FindQaGroupLayer([NotNull] Map map)
		{
			return map.FindLayers(_qaGroupLayerName).OfType<GroupLayer>().FirstOrDefault();
		}

		// TODO: (daro) drop!
		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository,
		                                                string uniqueName,
		                                                string displayName)
		{
			throw new NotImplementedException("This should not happen!");
			//return new IssueWorkList(repository, AreaOfInterest, uniqueName, displayName);
		}

		protected override async Task<IWorkItemRepository> CreateItemRepositoryCoreAsync(
			IWorkItemStateRepository stateRepository)
		{
			DbStatusWorkItemRepository result;

			var watch = Stopwatch.StartNew();

			try
			{
				IList<Table> tables = await PrepareReferencedTables();

				IList<ISourceClass> sourceClasses = new List<ISourceClass>(tables.Count);

				// TODO: Make attribute reader more generic, use AttributeRoles
				var attributes = new[]
				                 {
					                 Attributes.QualityConditionName,
					                 Attributes.IssueCodeDescription,
					                 Attributes.InvolvedObjects,
					                 Attributes.IssueSeverity,
					                 Attributes.IssueCode,
					                 Attributes.IssueDescription,
					                 Attributes.IssueType
				                 };

				var datastoresByHandle = new Dictionary<IntPtr, Datastore>();

				foreach (Table table in tables)
				{
					string defaultDefinitionQuery = GetDefaultDefinitionQuery(table);

					TableDefinition tableDefinition = table.GetDefinition();

					DbSourceClassSchema schema =
						WorkListItemDatastore.CreateStatusSchema(tableDefinition);

					IAttributeReader attributeReader =
						WorkListItemDatastore.CreateAttributeReader(tableDefinition, attributes);

					Datastore datastore = table.GetDatastore();

					datastoresByHandle.TryAdd(datastore.Handle, datastore);

					sourceClasses.Add(
						new DatabaseSourceClass(new GdbTableIdentity(table), schema,
						                        attributeReader, defaultDefinitionQuery,
						                        WorkspaceUtils.GetWorkspaceDbType(datastore)));
				}

				if (datastoresByHandle.Count == 0)
				{
					throw new InvalidOperationException(
						"No valid source classes found for the work list's tables.");
				}

				Assert.True(datastoresByHandle.Count == 1,
				            "Multiple geodatabases are referenced by the work list's source classes.");

				var geodatabase = (Geodatabase) datastoresByHandle.First().Value;
				result = new DbStatusWorkItemRepository(sourceClasses, stateRepository,
				                                        WorkspaceUtils.GetCatalogPath(geodatabase));
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
				throw;
			}
			finally
			{
				_msg.DebugStopTiming(watch, "Created issue work item repository");
			}

			return result;
		}
	}
}
