using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Application.Configuration;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.GP;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class DatabaseWorkEnvironment : WorkEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly string _workListName = "Issue_Work_List";
		private readonly string _templateLayer = "Selection Work List.lyrx";
		private readonly string _domainName = "CORRECTION_STATUS_CD";

		private readonly List<string> _issueFeatureClassNames = new List<string>
		                                                        {
			                                                        "IssueLines",
			                                                        "IssueMultiPatches",
			                                                        "IssuePoints", "IssuePolygons",
			                                                        "IssueRows"
		                                                        };
		[CanBeNull] private string _path;

		public DatabaseWorkEnvironment() : this(BrowseGeodatabase())
		{
		}

		[CanBeNull]
		private static string BrowseGeodatabase()
		{
			const string title = "Select Existing Issue Geodatabase";
			var browseFilter =
				BrowseProjectFilter.GetFilter(DAML.Filter.esri_browseDialogFilters_geodatabases_file);

			return GetSelectedItemPath(title, ItemFilters.geodatabases, browseFilter);
		}

		public DatabaseWorkEnvironment(string path)
		{
			_path = path;
		}

		protected override async Task<bool> TryPrepareSchemaCoreAsync()
		{
			if (_path == null)
			{
				return await base.TryPrepareSchemaCoreAsync();
			}

			await Task.WhenAll(
				GeoprocessingUtils.CreateDomainAsync(_path, _domainName, "Correction status for work list"),
				GeoprocessingUtils.AddCodedValueToDomainAsync(_path, _domainName, 100, "Not Corrected"),
				GeoprocessingUtils.AddCodedValueToDomainAsync(_path, _domainName, 200, "Corrected"));

			return true;
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
			var dialog = new OpenItemDialog
			             {
				             BrowseFilter = browseFilter,
				             Filter = filter,
				             Title = title
			             };

			if (dialog.ShowDialog().HasValue && dialog.Items.ToList().Count > 0)
			{
				return dialog.Items.FirstOrDefault()?.Path;
			}

			_msg.Info("No Issue Geodatabase selected");
			return null;
		}

		protected override async Task<BasicFeatureLayer> EnsureStatusFieldCoreAsync(
			BasicFeatureLayer featureLayer)
		{
			const string fieldName = "STATUS";

			Task<bool> addField =
				GeoprocessingUtils.AddFieldAsync(featureLayer.Name, fieldName, "Status",
				                                 esriFieldType.esriFieldTypeInteger, null, null,
				                                 null, true, false, _domainName);

			Task<bool> assignDefaultValue =
				GeoprocessingUtils.AssignDefaultToFieldAsync(featureLayer.Name, fieldName, 100);

			await Task.WhenAll(addField, assignDefaultValue);

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
