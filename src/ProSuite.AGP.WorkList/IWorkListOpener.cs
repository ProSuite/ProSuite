using System.Threading.Tasks;

namespace ProSuite.AGP.WorkList
{
	public interface IWorkListOpener
	{
		Task OpenSelectionWorkListAsync();

		Task OpenIssueWorkListAsync(string issuesGdbPath = null,
		                            bool removeExisting = false);
	}
}
