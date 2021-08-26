using System.Linq;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA;
using ProSuite.AGP.QA.Worklist;
using ProSuite.Application.Configuration;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class IssueWorklistEnvironment : IssueWorklistEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly string _templateLayer = "Selection Work List.lyrx";

		public IssueWorklistEnvironment([CanBeNull] string path) : base(path) { }

		public IssueWorklistEnvironment() : base(BrowseGeodatabase()) { }

		protected override LayerDocument GetLayerDocumentCore()
		{
			string path = ConfigurationUtils.GetConfigFilePath(_templateLayer);

			return LayerUtils.CreateLayerDocument(path);
		}

		[CanBeNull]
		private static string BrowseGeodatabase()
		{
			const string title = "Select Existing Issue Geodatabase";
			BrowseProjectFilter browseFilter =
				BrowseProjectFilter.GetFilter(
					DAML.Filter.esri_browseDialogFilters_geodatabases_file);

			return GetSelectedItemPath(title, ItemFilters.geodatabases, browseFilter);
		}

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
	}
}
