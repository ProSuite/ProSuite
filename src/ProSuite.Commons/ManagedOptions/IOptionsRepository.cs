namespace ProSuite.Commons.ManagedOptions
{
	public interface IOptionsRepository<TOptions> where TOptions : class
	{
		TOptions GetOptions();

		void Update(TOptions options);

		string GetStorageLocationMessage();
	}
}
