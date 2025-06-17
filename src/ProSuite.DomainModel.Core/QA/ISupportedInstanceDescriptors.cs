using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// Encapsulates the resolution of the currently available instance descriptors by name.
	/// This could be the look-up of the instance descriptors as used in the DDX.
	/// This could be the look-up of the instance descriptors as used in some XML definition.
	/// This could be the look-up of the instance descriptors using their canonical name.
	/// Or some combination of the above methods.
	/// </summary>
	public interface ISupportedInstanceDescriptors
	{
		[CanBeNull]
		T GetInstanceDescriptor<T>([NotNull] string name) where T : InstanceDescriptor;

		[CanBeNull]
		TestDescriptor GetTestDescriptor([NotNull] string name);

		[CanBeNull]
		TransformerDescriptor GetTransformerDescriptor([NotNull] string name);

		[CanBeNull]
		IssueFilterDescriptor GetIssueFilterDescriptor([NotNull] string name);

		/// <summary>
		/// The number of known instance descriptors.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Adds the instance descriptor to the list of known instance descriptors using its
		/// canonical name.
		/// </summary>
		/// <param name="instanceDescriptor"></param>
		/// <returns></returns>
		bool AddDescriptor([NotNull] InstanceDescriptor instanceDescriptor);

		/// <summary>
		/// Whether the instance descriptor with the given name is contained in the supported 
		/// instance descriptors.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		bool Contains(string name);
	}
}
