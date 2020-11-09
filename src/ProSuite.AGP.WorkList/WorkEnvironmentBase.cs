using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;

namespace ProSuite.AGP.WorkList
{
	public abstract class WorkEnvironmentBase
	{
		public string UniqueName { get; private set; }

		public IWorkList CreateWorkList(IWorkListContext context)
		{
			Map map = MapView.Active.Map;

			IEnumerable<BasicFeatureLayer> featureLayers = GetLayers(map).Select(EnsureMapContainsLayerCore);

			UniqueName = GetWorkListName(context);

			IRepository stateRepository = CreateStateRepositoryCore(context.GetPath(UniqueName), UniqueName);

			IWorkItemRepository repository = CreateItemRepositoryCore(featureLayers, stateRepository);

			// todo daro: dispose work list to free memory !!!!
			IWorkList workList = CreateWorkListCore(repository, UniqueName);
			
			return workList;
		}

		public LayerDocument GetLayerDocument()
		{
			return GetLayerDocumentCore();
		}

		protected abstract string GetWorkListName(IWorkListContext context);

		protected abstract IEnumerable<BasicFeatureLayer> GetLayers(Map map);

		// todo daro: revise purpose of this method
		protected abstract BasicFeatureLayer EnsureMapContainsLayerCore(BasicFeatureLayer featureLayer);

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
