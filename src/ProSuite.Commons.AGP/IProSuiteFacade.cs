using System.Threading.Tasks;

namespace ProSuite.Commons.AGP
{
	public interface IProSuiteFacade
	{
		Task OpenSelectionWorkListAsync();

		Task OpenIssueWorkListAsync(string issuesGdbPath = null,
		                            bool removeExisting = false);
	}
}
