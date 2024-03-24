namespace ProSuite.Processing.Domain
{
	public interface IProcessingDebug
	{
		bool IsDebugging { get; }

		void Break();

		void Show(string message, object shape = null);

		void Clear(); // TODO drop and always clear when user continues after a break
	}
}
