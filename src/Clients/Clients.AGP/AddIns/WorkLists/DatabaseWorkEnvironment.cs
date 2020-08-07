using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.DataModel;

namespace Clients.AGP.ProSuiteSolution.WorkLists
{
	public class DatabaseWorkEnvironment : WorkEnvironmentBase
	{
		const string _workListName = "Error Work List";

		protected override void ShowWorkListCore(IWorkList workList, LayerDocument layerTemplate)
		{
			WorkListsModule.Current.Show(workList, layerTemplate);
		}

		protected override IEnumerable<BasicFeatureLayer> GetLayers(Map map)
		{
			return map.GetLayersAsFlattenedList().OfType<BasicFeatureLayer>();
		}

		protected override BasicFeatureLayer EnsureMapContainsLayerCore(BasicFeatureLayer featureLayer)
		{
			// todo daro: determine layer identity
			throw new NotImplementedException();
		}

		protected override LayerDocument GetLayerDocumentCore()
		{
			throw new NotImplementedException();
		}

		protected override IWorkItemRepository CreateRepositoryCore(
			IEnumerable<BasicFeatureLayer> featureLayers)
		{
			IEnumerable<GdbTableIdentity> tables = MapUtils.GetDistinctTables(featureLayers, out IEnumerable<GdbWorkspaceIdentity> distinctWorkspaces);

			IEnumerable<IWorkspaceContext> workspaces = GetWorkspaceContexts(distinctWorkspaces);

			IWorkItemRepository repository = new ErrorItemRepository(workspaces);
			repository.RegisterDatasets(tables.ToList());

			return repository;
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository)
		{
			return new ErrorWorkList(repository, _workListName);
		}
	}
}
