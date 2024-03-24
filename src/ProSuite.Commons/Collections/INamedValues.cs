namespace ProSuite.Commons.Collections
{
	public interface INamedValues
	{
		/// <returns>true iff the given <paramref name="name"/> exists</returns>
		bool Exists(string name);

		/// <returns>the value for the given <paramref name="name"/>,
		/// or <c>null</c> if there is no such name</returns>
		object GetValue(string name);
	}
}
