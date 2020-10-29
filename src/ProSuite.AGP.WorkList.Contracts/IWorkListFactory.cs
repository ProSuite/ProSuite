namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkListFactory
	{
		string Name { get; }

		IWorkList Get();
	}
}