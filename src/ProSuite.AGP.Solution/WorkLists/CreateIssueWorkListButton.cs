using System.Threading.Tasks;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	// todo daro change name to OpenIssueWorklistButton?
	[UsedImplicitly]
	internal class CreateIssueWorkListButton : OpenWorklistButtonBase
	{
		protected override async Task OnClickCore(WorkEnvironmentBase environment)
		{
			await ProSuiteUtils.OpenWorklistAsync(environment);
		}

		protected override WorkEnvironmentBase CreateEnvironment(string path = null)
		{
			return new IssueWorklistEnvironment();
		}
	}
}
