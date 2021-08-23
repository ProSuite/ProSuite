using System.Threading.Tasks;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	// todo daro change name to OpenSelectionWorklistButton?
	[UsedImplicitly]
	internal class CreateSelectionWorkListButton : OpenWorklistButtonBase
	{
		protected override async Task OnClickCore(WorkEnvironmentBase environment)
		{
			await ProSuiteUtils.OpenWorklistAsync(environment);
		}

		protected override WorkEnvironmentBase CreateEnvironment(string path = null)
		{
			return new InMemoryWorkEnvironment();
		}
	}
}
