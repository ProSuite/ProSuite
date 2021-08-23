using System.Threading.Tasks;

namespace ProSuite.Commons.AGP
{
	public interface IProSuiteFacade
	{
		Task OpenSelectionWorklistAsync();

		Task OpenIssueWorklistAsync(string issuesGdbPath = null);
	}
}
