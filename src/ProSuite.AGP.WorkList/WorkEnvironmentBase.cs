using System;
using System.Collections.Generic;
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
		public abstract string DisplayName { get; }

		[NotNull]
		public async Task<IWorkList> CreateWorkListAsync([NotNull] string uniqueName,
		                                                 [CanBeNull] string displayName = null)
		{
			Assert.ArgumentNotNullOrEmpty(uniqueName, nameof(uniqueName));

			if (! await TryPrepareSchemaCoreAsync())
			{
				return await Task.FromResult(default(IWorkList));
			}

			BasicFeatureLayer[] featureLayers =
				await Task.WhenAll(GetLayersCore().Select(EnsureStatusFieldCoreAsync));

			string fileName = string.IsNullOrEmpty(displayName) ? DisplayName : displayName;

			string definitionFilePath = GetDefinitionFile(fileName).LocalPath;

			//string path = WorkListUtils.GetUri(definitionFilePath, uniqueName, FileSuffix).LocalPath;
			_msg.Debug($"Create work list state repository in {definitionFilePath}");

			IRepository stateRepository = CreateStateRepositoryCore(definitionFilePath, uniqueName);

			IWorkItemRepository repository =
				CreateItemRepositoryCore(featureLayers, stateRepository);

			return CreateWorkListCore(repository, uniqueName, fileName);
		}

		public LayerDocument GetLayerDocument()
		{
			return GetLayerDocumentCore();
		}

		/// <summary>
		/// Loads associated layers if there are any.
		/// </summary>
		public virtual IEnumerable<BasicFeatureLayer> LoadLayers()
		{
			return Enumerable.Empty<BasicFeatureLayer>();
		}

		protected abstract ILayerContainerEdit GetContainer();

		protected virtual async Task<bool> TryPrepareSchemaCoreAsync()
		{
			return await Task.FromResult(true);
		}

		protected abstract IEnumerable<BasicFeatureLayer> GetLayersCore();

		// todo daro: revise purpose of this method
		protected abstract Task<BasicFeatureLayer> EnsureStatusFieldCoreAsync(
			BasicFeatureLayer featureLayer);

		protected abstract IWorkList
			CreateWorkListCore([NotNull] IWorkItemRepository repository,
			                   [NotNull] string uniqueName,
			                   [CanBeNull] string displayName = null);

		protected abstract IRepository CreateStateRepositoryCore(string path, string workListName);

		protected abstract IWorkItemRepository CreateItemRepositoryCore(
			IEnumerable<BasicFeatureLayer> featureLayers, IRepository stateRepository);

		protected abstract LayerDocument GetLayerDocumentCore();

		protected static Type GetWorkListTypeCore<T>() where T : IWorkList
		{
			return typeof(T);
		}

		public FeatureLayer AddLayer([NotNull] IWorkList worklist)
		{
			FeatureLayer worklistLayer =
				CreateWorklistLayer(worklist,
				                    GetDefinitionFile(worklist.DisplayName),
				                    GetContainer());

			LayerUtils.SetLayerSelectability(worklistLayer, false);

			LayerUtils.ApplyRenderer(worklistLayer, GetLayerDocument());

			return worklistLayer;
		}

		[NotNull]
		private static FeatureLayer CreateWorklistLayer(
			[NotNull] IWorkList worklist,
			[NotNull] Uri dataSource,
			[NotNull] ILayerContainerEdit layerContainer)
		{
			PluginDatastore datastore = null;
			Table table = null;

			try
			{
				datastore = WorkListUtils.GetPluginDatastore(dataSource);
				
				table = datastore.OpenTable(worklist.Name);
				Assert.NotNull(table);

				return LayerFactory.Instance.CreateFeatureLayer((FeatureClass) table,
				                                                layerContainer,
				                                                LayerPosition.AddToTop,
				                                                worklist.DisplayName);
			}
			finally
			{
				datastore?.Dispose();
				table?.Dispose();
			}
		}

		public Uri GetDefinitionFile(IWorkList worklist)
		{
			return GetDefinitionFile(worklist.DisplayName);
		}
		
		public Uri GetDefinitionFile(string worklistName)
		{
			return WorkListUtils.GetDatasource(
				Project.Current.HomeFolderPath, worklistName, FileSuffix);
		}
	}
}
