using System.Threading.Tasks;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	// todo daro change name to OpenIssueWorklistButton?
	[UsedImplicitly]
	internal class CreateIssueWorkListButton : OpenWorkListButtonBase
	{
		protected override async Task OnClickCore(WorkEnvironmentBase environment,
		                                          string path = null)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));

			await ProSuiteUtils.CreateWorkListAsync(environment);
		}

		protected override WorkEnvironmentBase CreateEnvironment(string path = null)
		{
			return new IssueWorkListEnvironment();
		}
	}
}
