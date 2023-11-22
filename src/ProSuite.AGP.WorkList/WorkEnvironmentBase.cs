using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	public abstract class WorkEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public abstract string FileSuffix { get; }

		[NotNull]
		public async Task<IWorkList> CreateWorkListAsync([NotNull] string uniqueName,
		                                                 [CanBeNull] string displayName = null)
		{
			Assert.ArgumentNotNullOrEmpty(uniqueName, nameof(uniqueName));

			if (! await TryPrepareSchemaCoreAsync())
			{
				return await Task.FromResult(default(IWorkList));
			}

			Stopwatch watch = Stopwatch.StartNew();

			string fileName = string.IsNullOrEmpty(displayName) ? uniqueName : displayName;

			string definitionFilePath = GetDefinitionFile(fileName);

			IRepository stateRepository = CreateStateRepositoryCore(definitionFilePath, uniqueName);

			_msg.DebugStopTiming(watch, "Created work list state repository in {0}",
			                     definitionFilePath);

			// todo daro: dispose feature classes?
			Table[] tables = await Task.WhenAll(GetTablesCore().Select(EnsureStatusFieldCoreAsync));

			AddToMapCore(tables);

			IWorkList result = CreateWorkListCore(
				CreateItemRepositoryCore(tables, stateRepository),
				uniqueName, displayName);

			_msg.DebugFormat("Created work list {0}", uniqueName);

			return result;
		}

		public void AddLayer([NotNull] IWorkList worklist, string path)
		{
			//Create the work list layer with basic properties and connect to datasource
			FeatureLayer worklistLayer =
				CreateWorklistLayer(worklist, path, GetContainerCore<ILayerContainerEdit>());

			//Set some hard-coded properties
			worklistLayer.SetScaleSymbols(false);
			worklistLayer.SetSelectable(false);
			worklistLayer.SetSnappable(false);

			//Set renderer based on symbology from template layer
			LayerDocument templateLayer = GetWorkListSymbologyTemplateLayer();
			LayerUtils.ApplyRenderer(worklistLayer, templateLayer);
		}

		public string GetDefinitionFile([NotNull] string worklistDisplayName)
		{
			Assert.ArgumentNotNullOrEmpty(worklistDisplayName, nameof(worklistDisplayName));

			return WorkListUtils.GetDatasource(
				Project.Current.HomeFolderPath, worklistDisplayName, FileSuffix);
		}

		/// <summary>
		/// Loads associated layers if there are any.
		/// </summary>
		public virtual void LoadAssociatedLayers() { }

		public virtual void RemoveAssociatedLayers() { }

		protected abstract void AddToMapCore(IEnumerable<Table> tables);

		protected abstract T GetContainerCore<T>() where T : class;

		protected virtual async Task<bool> TryPrepareSchemaCoreAsync()
		{
			return await Task.FromResult(true);
		}

		protected abstract IEnumerable<Table> GetTablesCore();

		protected abstract Task<Table> EnsureStatusFieldCoreAsync([NotNull] Table table);

		protected abstract IWorkList CreateWorkListCore([NotNull] IWorkItemRepository repository,
		                                                [NotNull] string uniqueName,
		                                                [CanBeNull] string displayName);

		protected abstract IRepository CreateStateRepositoryCore(string path, string workListName);

		// todo daro to IEnumerable<Table>
		protected abstract IWorkItemRepository CreateItemRepositoryCore(
			IEnumerable<Table> tables, IRepository stateRepository);

		protected abstract string GetWorkListSymbologyTemplateLayerPath();

		protected static Type GetWorkListTypeCore<T>() where T : IWorkList
		{
			return typeof(T);
		}

		#region Private

		[NotNull]
		private static FeatureLayer CreateWorklistLayer(
			[NotNull] IWorkList worklist,
			[NotNull] string path,
			[NotNull] ILayerContainerEdit layerContainer)
		{
			PluginDatastore datastore = null;
			Table table = null;

			try
			{
				datastore = WorkListUtils.GetPluginDatastore(new Uri(path, UriKind.Absolute));

				table = datastore.OpenTable(worklist.Name);
				Assert.NotNull(table);

				return LayerFactory.Instance.CreateLayer<FeatureLayer>(
					WorkListUtils.CreateLayerParams((FeatureClass) table, worklist.DisplayName),
					layerContainer);
			}
			finally
			{
				datastore?.Dispose();
				table?.Dispose();
			}
		}

		private LayerDocument GetWorkListSymbologyTemplateLayer()
		{
			string filePath = GetWorkListSymbologyTemplateLayerPath();

			_msg.DebugFormat("Using work list symbology template layer from {0}", filePath);

			return LayerUtils.CreateLayerDocument(filePath);
		}

		#endregion
	}
}
