using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	// todo daro change name to OpenIssueWorklistButton?
	[UsedImplicitly]
	internal class CreateIssueWorkListButton : OpenWorkListButtonBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async Task OnClickCore(WorkEnvironmentBase environment,
		                                          string path = null)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));

			await ProSuiteUtils.CreateWorkListAsync(environment);
		}

		protected override WorkEnvironmentBase CreateEnvironment(string path = null)
		{
			string gdbPath = BrowseGeodatabase();

			if (string.IsNullOrEmpty(gdbPath) || ! Directory.Exists(gdbPath))
			{
				return null;
			}

			return new IssueWorkListEnvironment(gdbPath);
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
