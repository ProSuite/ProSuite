using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public abstract class WorkEnvironmentBase
	{
		[CanBeNull]
		public string UniqueName { get; private set; }

		[NotNull]
		public async Task<IWorkList> CreateWorkListAsync([NotNull] IWorkListContext context)
		{
			Assert.ArgumentNotNull(context, nameof(context));

			Map map = MapView.Active.Map;

			if (! await TryPrepareSchemaCoreAsync())
			{
				return await Task.FromResult(default(IWorkList));
			}

			BasicFeatureLayer[] featureLayers = await Task.WhenAll(GetLayers(map).Select(EnsureStatusFieldCoreAsync));
			
			UniqueName = GetWorkListName(context);

			IRepository stateRepository = CreateStateRepositoryCore(context.GetPath(UniqueName), UniqueName);

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

		protected abstract string GetWorkListName(IWorkListContext context);

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
