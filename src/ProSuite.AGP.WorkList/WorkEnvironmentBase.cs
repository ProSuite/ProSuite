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

		[NotNull]
		public async Task<IWorkList> CreateWorkListAsync([NotNull] string uniqueName,
		                                                 [CanBeNull] string displayName = null)
		{
			Assert.ArgumentNotNullOrEmpty(uniqueName, nameof(uniqueName));

			if (! await TryPrepareSchemaCoreAsync())
			{
				return await Task.FromResult(default(IWorkList));
			}

			string fileName = string.IsNullOrEmpty(displayName) ? uniqueName : displayName;

			string definitionFilePath = GetDefinitionFile(fileName);

			IRepository stateRepository = CreateStateRepositoryCore(definitionFilePath, uniqueName);

			_msg.Debug($"Create work list state repository in {definitionFilePath}");

			// todo daro: dispose feature classes?
			FeatureClass[] featureClasses =
				await Task.WhenAll(GetFeatureClassesCore().Select(EnsureStatusFieldCoreAsync));

			IWorkItemRepository repository =
				CreateItemRepositoryCore(GetLayersCore(featureClasses), stateRepository);

			return CreateWorkListCore(repository, uniqueName, displayName);
		}

		private LayerDocument GetLayerDocument()
		{
			return GetLayerDocumentCore();
		}

		public FeatureLayer AddLayer([NotNull] IWorkList worklist, string path)
		{
			FeatureLayer worklistLayer =
				CreateWorklistLayer(worklist, path, GetContainer());

			worklistLayer.SetSelectable(false);

			LayerUtils.ApplyRenderer(worklistLayer, GetLayerDocument());

			return worklistLayer;
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
		public virtual IEnumerable<BasicFeatureLayer> LoadLayers()
		{
			return Enumerable.Empty<BasicFeatureLayer>();
		}

		protected abstract ILayerContainerEdit GetContainer();

		protected virtual async Task<bool> TryPrepareSchemaCoreAsync()
		{
			return await Task.FromResult(true);
		}

		protected abstract IEnumerable<BasicFeatureLayer> GetLayersCore(
			[NotNull] IEnumerable<FeatureClass> featureClasses);

		protected abstract IEnumerable<FeatureClass> GetFeatureClassesCore();

		protected abstract Task<FeatureClass> EnsureStatusFieldCoreAsync(
			[NotNull] FeatureClass featureClass);

		protected abstract IWorkList
			CreateWorkListCore([NotNull] IWorkItemRepository repository,
			                   [NotNull] string uniqueName,
			                   [CanBeNull] string displayName);

		protected abstract IRepository CreateStateRepositoryCore(string path, string workListName);

		// todo daro to IEnumerable<Table>
		protected abstract IWorkItemRepository CreateItemRepositoryCore(
			IEnumerable<BasicFeatureLayer> featureLayers, IRepository stateRepository);

		protected abstract LayerDocument GetLayerDocumentCore();

		protected static Type GetWorkListTypeCore<T>() where T : IWorkList
		{
			return typeof(T);
		}

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
					layerContainer,
					LayerPosition.AddToTop);
			}
			finally
			{
				datastore?.Dispose();
				table?.Dispose();
			}
		}
	}
}
