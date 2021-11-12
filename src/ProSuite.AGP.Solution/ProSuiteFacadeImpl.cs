using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution
{
	public class ProSuiteImpl : IProSuiteFacade
	{
		// todo daro: there is a confusion between OpenWorklist() and CreateWorklist(). In ProSuiteSolution it was always
		// OpenWorklist() (I think). OpenWorklist could also mean opening an existing work list. CreateWorklist is more precise
		public async Task OpenSelectionWorklistAsync()
		{
			WorkEnvironmentBase environment = new InMemoryWorkEnvironment();

			await CreateWorklist(environment);
		}

		public async Task OpenIssueWorklistAsync(string issuesGdbPath)
		{
			WorkEnvironmentBase environment = string.IsNullOrEmpty(issuesGdbPath)
				                                  ? new IssueWorkListEnvironment()
				                                  : new IssueWorkListEnvironment(issuesGdbPath);

			await CreateWorklist(environment);
		}

		public static async Task CreateWorklist([NotNull] WorkEnvironmentBase environment)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));

			string name = WorkListsModule.EnsureUniqueName();
			Assert.NotNullOrEmpty(name);

			// todo daro use BackgroundTask?
			IWorkList workList =
				await QueuedTask.Run(
					() => WorkListsModule.Current.CreateWorkListAsync(environment, name));

			WorkListsModule.Current.ShowView(workList);
		}

		public static async Task OpenWorklist([NotNull] WorkEnvironmentBase environment,
		                                      [NotNull] string path)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			IWorkList worklist =
				await QueuedTask.Run(() => WorkListsModule.Current.ShowWorklist(environment, path));

			// don't add associated layers if name is null
			if (worklist == null)
			{
				return;
			}

			WorkListsModule.Current.ShowView(worklist);
		}
	}
}
