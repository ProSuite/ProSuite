namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkListContext
	{
		string EnsureUniqueName(string workListName);

		string GetPath(string workListName);
	}
}
