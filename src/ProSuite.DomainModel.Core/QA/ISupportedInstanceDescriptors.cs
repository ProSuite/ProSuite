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

		int Count { get; }
	}
}
