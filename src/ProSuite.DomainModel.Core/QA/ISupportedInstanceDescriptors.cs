using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	public interface ISupportedInstanceDescriptors
	{
		T GetInstanceDescriptor<T>(string name) where T : InstanceDescriptor;

		TestDescriptor GetTestDescriptor(string name);

		TransformerDescriptor GetTransformerDescriptor(string name);

		IssueFilterDescriptor GetIssueFilterDescriptor(string name);

		int Count { get; }

		bool AddDescriptor([NotNull] InstanceDescriptor instanceDescriptor);
	}
}
