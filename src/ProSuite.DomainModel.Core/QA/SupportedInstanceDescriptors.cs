using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

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

		public bool AddDescriptor(InstanceDescriptor instanceDescriptor)
		{
			Assert.NotNull(instanceDescriptor, nameof(instanceDescriptor));

			bool added;
			switch (instanceDescriptor)
			{
				case TestDescriptor testDescriptor:
					added = TryAdd(_testDescriptors, testDescriptor);
					break;
				case IssueFilterDescriptor issueFilterDescriptor:
					added = TryAdd(_issueFilterDescriptors, issueFilterDescriptor);
					break;
				case TransformerDescriptor transformerDescriptor:
					added = TryAdd(_transformerDescriptors, transformerDescriptor);
					break;
				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported or null instance descriptor: {instanceDescriptor.Name}");
			}

			if (added)
			{
				InstanceDescriptorAdded(instanceDescriptor);
			}

			return added;
		}

		public bool Contains(string name)
		{
			return _testDescriptors.ContainsKey(name) ||
			       _transformerDescriptors.ContainsKey(name) ||
			       _issueFilterDescriptors.ContainsKey(name);
		}

		public int Count => _testDescriptors.Count +
		                    _transformerDescriptors?.Count ?? 0 +
		                    _issueFilterDescriptors?.Count ?? 0;

		#endregion

		/// <summary>
		/// Allows implementors to process the added instanceDescriptor. Typically, this is used to
		/// ensure the InstanceInfo property is cached with the appropriate, environment-specific
		/// implementation.
		/// </summary>
		/// <param name="instanceDescriptor"></param>
		protected virtual void InstanceDescriptorAdded(
			[NotNull] InstanceDescriptor instanceDescriptor) { }

		private static bool TryAdd<T>(IDictionary<string, T> toDictionary, T item)
			where T : InstanceDescriptor
		{
			//string canonicalName = item.GetCanonicalName();
			string descriptorName = item.Name;

			if (toDictionary.ContainsKey(descriptorName))
			{
				return false;
			}

			toDictionary.Add(descriptorName, item);

			return true;
		}
	}
}
