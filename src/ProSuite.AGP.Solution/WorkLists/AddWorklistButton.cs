using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class AddWorklistButton : OpenWorklistButtonBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async Task OnClickCore(WorkEnvironmentBase environment)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));

			await ProSuiteUtils.OpenWorklistAsync(environment);
		}

		protected override WorkEnvironmentBase CreateEnvironment(string path = null)
		{
			var window = FrameworkApplication.ActiveWindow as IProjectWindow;
			string selectedPath = window?.SelectedItems.FirstOrDefault()?.Path;

			if (string.IsNullOrEmpty(selectedPath))
			{
				return null;
			}

			_msg.Debug($"Open work list from file {selectedPath}");

			if (! File.Exists(selectedPath))
			{
				return null;
			}

			string extension = Path.GetExtension(selectedPath);

			if (string.Equals(extension, ".swl"))
			{
				return new InMemoryWorkEnvironment();
			}

			if (string.Equals(extension, ".iwl"))
			{
				string gdbPath = WorkListUtils.GetIssueGeodatabasePath(selectedPath);
				Assert.NotNull(gdbPath, "issue geodatabase does not exist");

				return new IssueWorklistEnvironment(gdbPath);
			}

			return null;
		}
	}
}
