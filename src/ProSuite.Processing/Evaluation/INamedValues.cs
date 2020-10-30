namespace ProSuite.Processing.Evaluation
{
	public interface INamedValues
	{
		/// <summary>
		/// Returns <c>true</c> iff the given <paramref name="name"/> exists.
		/// </summary>
		bool Exists(string name);

		/// <summary>
		/// Returns the value for the given <paramref name="name"/>,
		/// or <c>null</c> if there is no such name.
		/// </summary>
		object GetValue(string name);
	}
}
