using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
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
		public async Task<IWorkList> CreateWorkListAsync([NotNull] string definitionFilePath, [NotNull] string uniqueName)
		{
			Assert.ArgumentNotNullOrEmpty(definitionFilePath, nameof(definitionFilePath));
			Assert.ArgumentNotNullOrEmpty(uniqueName, nameof(uniqueName));

			Map map = MapView.Active.Map;

			if (! await TryPrepareSchemaCoreAsync())
			{
				return await Task.FromResult(default(IWorkList));
			}

			BasicFeatureLayer[] featureLayers = await Task.WhenAll(GetLayers(map).Select(EnsureStatusFieldCoreAsync));

			//string path = WorkListUtils.GetUri(definitionFilePath, uniqueName, FileSuffix).LocalPath;
			_msg.Debug($"Create work list state repository in {definitionFilePath}");

			IRepository stateRepository = CreateStateRepositoryCore(definitionFilePath, uniqueName);

			IWorkItemRepository repository = CreateItemRepositoryCore(featureLayers, stateRepository);

			return CreateWorkListCore(repository, uniqueName);
		}

		public LayerDocument GetLayerDocument()
		{
			return GetLayerDocumentCore();
		}

		protected virtual async Task<bool> TryPrepareSchemaCoreAsync()
		{
			return await Task.FromResult(true);
		}

		protected abstract IEnumerable<BasicFeatureLayer> GetLayers(Map map);

		// todo daro: revise purpose of this method
		protected abstract Task<BasicFeatureLayer> EnsureStatusFieldCoreAsync(BasicFeatureLayer featureLayer);

		protected abstract IWorkList CreateWorkListCore(IWorkItemRepository repository, string name);

		protected abstract IRepository CreateStateRepositoryCore(string path, string workListName);

		protected abstract IWorkItemRepository CreateItemRepositoryCore(IEnumerable<BasicFeatureLayer> featureLayers, IRepository stateRepository);

		protected abstract LayerDocument GetLayerDocumentCore();

		protected static Type GetWorkListTypeCore<T>() where T : IWorkList
		{
			return typeof(T);
		}
	}
}
