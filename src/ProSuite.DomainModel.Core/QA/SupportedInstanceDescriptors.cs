using System;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.DomainModel.Core.QA
{
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

		public bool FallBackToCanonicalName { get; set; }

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
			if (! _testDescriptors.TryGetValue(name, out TestDescriptor result) &&
			    FallBackToCanonicalName)
			{
				TestDescriptor fallback =
					_testDescriptors.Values.FirstOrDefault(d => name == d.GetCanonicalName());

				if (fallback != null)
				{
					return fallback;
				}
			}

			return result;
		}

		public TransformerDescriptor GetTransformerDescriptor(string name)
		{
			TransformerDescriptor result = null;
			if (_transformerDescriptors?.TryGetValue(name, out result) == false &&
			    FallBackToCanonicalName)
			{
				TransformerDescriptor fallback =
					_transformerDescriptors.Values.FirstOrDefault(
						d => name == d.GetCanonicalName());

				if (fallback != null)
				{
					return fallback;
				}
			}

			return result;
		}

		public IssueFilterDescriptor GetIssueFilterDescriptor(string name)
		{
			IssueFilterDescriptor result = null;
			if (_issueFilterDescriptors?.TryGetValue(name, out result) == false &&
			    FallBackToCanonicalName)
			{
				IssueFilterDescriptor fallback =
					_issueFilterDescriptors.Values.FirstOrDefault(
						d => name == d.GetCanonicalName());

				if (fallback != null)
				{
					return fallback;
				}
			}

			return result;
		}

		public int Count => _testDescriptors.Count +
		                    _transformerDescriptors?.Count ?? 0 +
		                    _issueFilterDescriptors?.Count ?? 0;

		#endregion
	}
}
