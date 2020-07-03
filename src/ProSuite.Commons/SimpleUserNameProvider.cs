namespace ProSuite.Commons
{
	public class SimpleUserNameProvider : IUserNameProvider
	{
		public SimpleUserNameProvider(string displayName)
		{
			DisplayName = displayName;
		}

		public string DisplayName { get; private set; }
	}
}