using ProSuite.DomainModel.Core.QA;

namespace ProSuite.Microservices.Server.AO.QA
{
	public interface ISupportedInstanceDescriptors
	{
		T GetInstanceDescriptor<T>(string name) where T : InstanceDescriptor;

		TestDescriptor GetTestDescriptor(string name);

		TransformerDescriptor GetTransformerDescriptor(string name);

		IssueFilterDescriptor GetIssueFilterDescriptor(string name);

		int Count { get; }
	}
}
