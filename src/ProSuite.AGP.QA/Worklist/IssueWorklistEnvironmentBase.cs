using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

		protected override T GetLayerContainerCore<T>(MapView mapView)
		{
			var qaGroupLayerName = "QA";

			GroupLayer qaGroupLayer = mapView.Map.FindLayers(qaGroupLayerName)
			                                 .OfType<GroupLayer>().FirstOrDefault();

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

					datastoresByHandle.TryAdd(table.GetDatastore().Handle, table.GetDatastore());

					sourceClasses.Add(new DatabaseSourceClass(new GdbTableIdentity(table), schema,
					                                          attributeReader,
					                                          defaultDefinitionQuery));
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
