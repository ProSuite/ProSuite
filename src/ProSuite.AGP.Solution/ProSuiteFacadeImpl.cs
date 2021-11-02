using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution
{
	public class ProSuiteImpl : IProSuiteFacade
	{
		public async Task OpenSelectionWorkListAsync()
		{
			WorkEnvironmentBase environment = new InMemoryWorkEnvironment();

			await OpenWorkList(environment);
		}

		public async Task OpenIssueWorkListAsync(string issuesGdbPath)
		{
			WorkEnvironmentBase environment = string.IsNullOrEmpty(issuesGdbPath)
				                                  ? new IssueWorkListEnvironment()
				                                  : new IssueWorkListEnvironment(issuesGdbPath);

			await OpenWorkList(environment);
		}

		public static async Task OpenWorkList([NotNull] WorkEnvironmentBase environment)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));

			string name = WorkListsModule.Current.EnsureUniqueName();
			Assert.NotNullOrEmpty(name);

			// todo daro use BackgroundTask?
			await QueuedTask.Run(
				() => WorkListsModule.Current.CreateWorkListAsync(environment, name));

			WorkListsModule.Current.ShowView(name);
		}
	}
}
