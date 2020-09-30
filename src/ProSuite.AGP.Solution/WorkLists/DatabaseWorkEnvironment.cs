using System;
using System.Collections.Generic;
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
	class DatabaseWorkEnvironment : WorkEnvironmentBase
	{
		protected override string GetWorkListName(IWorkListContext context)
		{
			throw new NotImplementedException();
		}

		protected override IEnumerable<BasicFeatureLayer> GetLayers(Map map)
		{
			throw new NotImplementedException();
		}

		protected override BasicFeatureLayer EnsureMapContainsLayerCore(BasicFeatureLayer featureLayer)
		{
			throw new NotImplementedException();
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository, string name)
		{
			return new IssueWorkList(repository, name);
		}

		protected override IRepository CreateStateRepositoryCore(string path, string workListName)
		{
			Type type = GetWorkListTypeCore<IssueWorkList>();

			return new XmlWorkItemStateRepository(path, workListName, type);
		}

		protected override IWorkItemRepository CreateItemRepositoryCore(IEnumerable<BasicFeatureLayer> featureLayers, IRepository stateRepository)
		{
			Dictionary<Geodatabase, List<Table>> tables = MapUtils.GetDistinctTables(featureLayers);

			return new IssueItemRepository(tables, stateRepository);
		}

		protected override LayerDocument GetLayerDocumentCore()
		{
			throw new NotImplementedException();
		}
	}
}
