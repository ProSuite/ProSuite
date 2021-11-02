using System.Threading.Tasks;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	// todo daro change name to OpenIssueWorkListButton?
	[UsedImplicitly]
	internal class CreateIssueWorkListButton : OpenWorkListButtonBase
	{
		protected override async Task OnClickCore(WorkEnvironmentBase environment)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));

			await ProSuiteUtils.OpenWorkListAsync(environment);
		}

		protected override WorkEnvironmentBase CreateEnvironment(string path = null)
		{
			return new IssueWorkListEnvironment();
		}
	}
}
