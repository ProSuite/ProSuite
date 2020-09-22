using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Carto;

namespace ProSuite.AGP.Solution.WorkLists
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
			Dictionary<Geodatabase, List<Table>> tables = MapUtils.GetDistinctTables(featureLayers);

			// todo daro: state repository must not be null
			IRepository stateRepository = new XmlWorkItemStateRepository(@"C:\temp\selection_work_list.xml");
			return new IssueItemRepository(tables, stateRepository);
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository)
		{
			return new IssueWorkList(repository, _workListName);
		}
	}
}
