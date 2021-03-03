namespace ProSuite.DomainModel.Core.Processing
{
	public interface IProcessingDebug
	{
		/// <summary>
		/// Return <c>true</c> iff in debug mode
		/// </summary>
		bool IsDebugging { get; }

		/// <summary>
		/// Enter break mode (suspend process execution) and wait for user to continue.
		/// </summary>
		void Break();

		/// <summary>
		/// Append <paramref name="message"/> and <paramref name="shape"/>
		/// to the list of items shown to the user while in break mode.
		/// Has no effect if <see cref="IsDebugging"/> is <c>false</c>.
		/// </summary>
		void Show(string message, object shape = null);

		/// <summary>
		/// Remove all items added via calls to Show().
		/// </summary>
		/// <remarks>
		/// Call this every once in a while to avoid hogging memory for large geometries.
		/// </remarks>
		// TODO Drop and clear implicitly whenever the user continues from break mode?
		void Clear();
	}
}
