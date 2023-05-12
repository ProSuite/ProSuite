using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.Microservices.Client.QA
{
	/// <summary>
	/// Encapsulates the resolution of the currently available instance descriptors by name.
	/// This could be the look-up of the instance descriptors as used in the DDX.
	/// This could be the look-up of the instance descriptors using their canonical name.
	/// TODO: Consider supporting the look-up by canonical name as automatic fallback.
	/// TODO: Add methods for the look-up by type name including the old-name/new-name mapping
	/// </summary>
	public class SupportedInstanceDescriptors : ISupportedInstanceDescriptors
	{
		private readonly IDictionary<string, TestDescriptor> _testDescriptors;
		private readonly IDictionary<string, TransformerDescriptor> _transformerDescriptors;
		private readonly IDictionary<string, IssueFilterDescriptor> _issueFilterDescriptors;

		public SupportedInstanceDescriptors(
			IEnumerable<TestDescriptor> testDescriptors,
			IEnumerable<TransformerDescriptor> transformerDescriptors = null,
			IEnumerable<IssueFilterDescriptor> issueFilterDescriptors = null)
		{
			_testDescriptors = testDescriptors.ToDictionary(d => d.Name);

			_transformerDescriptors = transformerDescriptors?.ToDictionary(d => d.Name);
			_issueFilterDescriptors = issueFilterDescriptors?.ToDictionary(d => d.Name);
		}

		#region Implementation of ISupportedInstanceDescriptors

		public T GetInstanceDescriptor<T>(string name) where T : InstanceDescriptor
		{
			if (typeof(T) == typeof(TestDescriptor))
			{
				return (T) (InstanceDescriptor) GetTestDescriptor(name);
			}

			if (typeof(T) == typeof(TransformerDescriptor))
			{
				return (T) (InstanceDescriptor) GetTransformerDescriptor(name);
			}

			if (typeof(T) == typeof(IssueFilterDescriptor))
			{
				return (T) (InstanceDescriptor) GetIssueFilterDescriptor(name);
			}

			throw new ArgumentOutOfRangeException($"Unsupported type: {typeof(T)}");
		}

		public TestDescriptor GetTestDescriptor(string name)
		{
			_testDescriptors.TryGetValue(name, out TestDescriptor result);

			return result;
		}

		public TransformerDescriptor GetTransformerDescriptor(string name)
		{
			TransformerDescriptor result = null;
			_transformerDescriptors?.TryGetValue(name, out result);

			return result;
		}

		public IssueFilterDescriptor GetIssueFilterDescriptor(string name)
		{
			IssueFilterDescriptor result = null;
			_issueFilterDescriptors?.TryGetValue(name, out result);

			return result;
		}

		public int Count => _testDescriptors.Count +
		                    _transformerDescriptors?.Count ?? 0 +
		                    _issueFilterDescriptors?.Count ?? 0;

		#endregion
	}
}
