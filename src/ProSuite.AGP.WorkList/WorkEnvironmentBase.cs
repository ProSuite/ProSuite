using System;
using System.Collections.Generic;
using System.Diagnostics;
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

			IWorkItemStateRepository stateRepository =
				CreateStateRepositoryCore(definitionFilePath, uniqueName);

			_msg.DebugStopTiming(watch, "Created work list state repository in {0}",
			                     definitionFilePath);

			// todo daro: dispose feature classes?
			IList<Table> tables = await PrepareReferencedTables();

			LoadAssociatedLayers();

			IWorkList result = CreateWorkListCore(
				CreateItemRepositoryCore(tables, stateRepository),
				uniqueName, displayName);

			_msg.DebugFormat("Created work list {0}", uniqueName);

			return result;
		}

		protected virtual Task<IList<Table>> PrepareReferencedTables()
		{
			IList<Table> result = new List<Table>();
			return Task.FromResult(result);
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
			var renderer = LayerUtils.GetRenderer(templateLayer, worklistLayer);
			if (renderer != null)
			{
				worklistLayer.SetRenderer(renderer);
			}
			//else: no compatible renderer found in layer file
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

		protected abstract T GetContainerCore<T>() where T : class;

		protected virtual async Task<bool> TryPrepareSchemaCoreAsync()
		{
			return await Task.FromResult(true);
		}

		protected abstract IWorkList CreateWorkListCore([NotNull] IWorkItemRepository repository,
		                                                [NotNull] string uniqueName,
		                                                [CanBeNull] string displayName);

		protected abstract IWorkItemStateRepository CreateStateRepositoryCore(
			string path, string workListName);

		// todo daro to IEnumerable<Table>
		protected abstract IWorkItemRepository CreateItemRepositoryCore(
			IList<Table> tables, IWorkItemStateRepository stateRepository);

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

			return LayerUtils.OpenLayerDocument(filePath);
		}

		#endregion
	}
}
