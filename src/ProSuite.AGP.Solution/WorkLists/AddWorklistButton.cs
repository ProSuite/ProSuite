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
	internal class AddWorklistButton : OpenWorkListButtonBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async Task OnClickCore(WorkEnvironmentBase environment,
		                                          string path = null)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			await ProSuiteUtils.OpenWorkListAsync(environment, path);
		}

		protected override string GetWorklistPathCore()
		{
			var window = FrameworkApplication.ActiveWindow as IProjectWindow;

			return window?.SelectedItems.FirstOrDefault()?.Path;
		}

		protected override WorkEnvironmentBase CreateEnvironment(string path = null)
		{
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}

			_msg.Debug($"Open work list from file {path}");

			if (! File.Exists(path))
			{
				return null;
			}

			string extension = Path.GetExtension(path);

			if (string.Equals(extension, ".swl"))
			{
				return new InMemoryWorkEnvironment();
			}

			if (string.Equals(extension, ".iwl"))
			{
				string gdbPath = WorkListUtils.GetIssueGeodatabasePath(path);
				Assert.NotNull(gdbPath, "issue geodatabase does not exist");

				return new IssueWorkListEnvironment(gdbPath);
			}

			return null;
		}
	}
}
