using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class DatabaseWorkEnvironment : WorkEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly string _workListName = "Issue_Work_List";
		private readonly string _templateLayer = "Selection Work List.lyrx";

		private readonly List<string> _issueFeatureClassNames = new List<string>
		                                                        {
			                                                        "IssueLines",
			                                                        "IssueMultiPatches",
			                                                        "IssuePoints", "IssuePolygons",
			                                                        "IssueRows"
		                                                        };
		private readonly string _path;

		public DatabaseWorkEnvironment()
		{
			const string title = "Select Existing Issue Geodatabase";
			var browseFilter = BrowseProjectFilter.GetFilter(DAML.Filter.esri_browseDialogFilters_geodatabases_file);

			_path = GetSelectedItemPath(title, ItemFilters.geodatabases, browseFilter);
		}

		protected override string GetWorkListName(IWorkListContext context)
		{
			return context.EnsureUniqueName(_workListName);
		}

		protected override IEnumerable<BasicFeatureLayer> GetLayers(Map map)
		{
			if (string.IsNullOrEmpty(_path))
			{
				yield break;
			}

			// todo daro: ensure layers are not already in map
			using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(_path, UriKind.Absolute))))
			{
				IEnumerable<string> featureClassNames =
					geodatabase.GetDefinitions<FeatureClassDefinition>()
					           .Select(definition => definition.GetName())
					           .Where(name => _issueFeatureClassNames.Contains(name));

				foreach (string featureClassName in featureClassNames)
				{
					using (var featureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
					{
						yield return LayerFactory.Instance.CreateFeatureLayer(
							featureClass, MapView.Active.Map, LayerPosition.AddToTop);
					}
				}
			}
		}

		// todo daro: to utils
		[CanBeNull]
		private static string GetSelectedItemPath(string title, string filter,
		                                          BrowseProjectFilter browseFilter)
		{
			var dialog = new OpenItemDialog()
			             {
				             BrowseFilter = browseFilter,
				             Filter = filter,
				             Title = title
			             };

			if (! dialog.ShowDialog().HasValue)
			{
				// todo daro: log?
				return string.Empty;
			}

			return dialog.Items.FirstOrDefault()?.Path;
		}

		protected override BasicFeatureLayer EnsureMapContainsLayerCore(BasicFeatureLayer featureLayer)
		{
			// we want every feature layer


			return featureLayer;
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
			string path = ConfigurationUtils.GetConfigFilePath(_templateLayer);

			return LayerUtils.CreateLayerDocument(path);
		}
	}
}
