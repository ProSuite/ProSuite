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

		// TODO DARO environment should be stateless. get state from module
		[CanBeNull]
		public string UniqueName { get; private set; }

		public abstract string FileSuffix { get; }

		[NotNull]
		public async Task<IWorkList> CreateWorkListAsync([NotNull] string homeFolderPath, [NotNull] string uniqueName)
		{
			Assert.ArgumentNotNullOrEmpty(homeFolderPath, nameof(homeFolderPath));
			Assert.ArgumentNotNullOrEmpty(uniqueName, nameof(uniqueName));

			Map map = MapView.Active.Map;

			if (! await TryPrepareSchemaCoreAsync())
			{
				return await Task.FromResult(default(IWorkList));
			}

			BasicFeatureLayer[] featureLayers = await Task.WhenAll(GetLayers(map).Select(EnsureStatusFieldCoreAsync));

			// create new name if worklist do not have one (stored in XML)
			//UniqueName = GetWorklistId() ?? GetWorkListName(context);
			UniqueName = uniqueName;

			string path = WorkListUtils.GetUri(homeFolderPath, uniqueName, FileSuffix).LocalPath;
			_msg.Debug($"Create work list state repository in {path}");

			IRepository stateRepository = CreateStateRepositoryCore(path, UniqueName);

			IWorkItemRepository repository = CreateItemRepositoryCore(featureLayers, stateRepository);

			return CreateWorkListCore(repository, UniqueName);
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
