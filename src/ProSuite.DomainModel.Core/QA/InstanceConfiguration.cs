using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public abstract class InstanceConfiguration : VersionedEntityWithMetadata,
	                                              INamed, IAnnotated
	{
		#region Persisted fields

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private readonly IList<TestParameterValue>
			_parameterValues = new List<TestParameterValue>();

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _name;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _description;

		[UsedImplicitly] private InstanceDescriptor _instanceDescriptor;

		#endregion

		protected InstanceConfiguration() { }

		protected InstanceConfiguration([NotNull] string name,
		                                [CanBeNull] string description = "")
		{
			Name = name;
			Description = description;
		}

		[NotNull]
		public IList<TestParameterValue> ParameterValues =>
			new ReadOnlyList<TestParameterValue>(_parameterValues);

		[Required]
		public virtual InstanceDescriptor InstanceDescriptor
		{
			get => _instanceDescriptor;
			set => _instanceDescriptor = value;
		}

		#region INamed, IAnnotated members

		[MaximumStringLength(2000)]
		public string Description
		{
			get => _description;
			set => _description = value;
		}

		[Required]
		[MaximumStringLength(200)]
		public string Name
		{
			get => _name;
			set => _name = value;
		}

		#endregion

		public TestParameterValue AddParameterValue([NotNull] TestParameterValue parameterValue)
		{
			Assert.ArgumentNotNull(parameterValue, nameof(parameterValue));

			_parameterValues.Add(parameterValue);

			return parameterValue;
		}

		public void RemoveParameterValue([NotNull] TestParameterValue parameterValue)
		{
			_parameterValues.Remove(parameterValue);
		}

		public void ClearParameterValues()
		{
			_parameterValues.Clear();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="includeReferencedProcessors">include RowFilters, IssueFilters and Transformers</param>
		/// <param name="includeSourceDatasets">include Transformers of dataset sources</param>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<Dataset> GetDatasetParameterValues(
			bool includeReferencedProcessors = false, bool includeSourceDatasets = false)
		{
			foreach (TestParameterValue parameterValue in ParameterValues)
			{
				var datasetTestParameterValue = parameterValue as DatasetTestParameterValue;

				if (datasetTestParameterValue == null)
				{
					continue;
				}

				Dataset dataset = datasetTestParameterValue.DatasetValue;

				if (dataset != null)
				{
					yield return dataset;
				}
				else if (includeSourceDatasets && datasetTestParameterValue.ValueSource != null)
				{
					foreach (Dataset referencedDataset in
					         datasetTestParameterValue.ValueSource.GetDatasetParameterValues(
						         includeSourceDatasets: true))
					{
						yield return referencedDataset;
					}
				}
			}

			if (includeReferencedProcessors)
			{
				foreach (var referencedDataset in EnumReferencedDatasetParameterValues())
				{
					yield return referencedDataset;
				}
			}
		}

		protected virtual IEnumerable<Dataset> EnumReferencedDatasetParameterValues()
		{
			foreach (TestParameterValue parameterValue in ParameterValues)
			{
				var datasetTestParameterValue = parameterValue as DatasetTestParameterValue;

				// Row filters
				if (datasetTestParameterValue?.RowFilterConfigurations != null)
				{
					foreach (var rowFilterConfiguration in
					         datasetTestParameterValue.RowFilterConfigurations)
					{
						foreach (Dataset referencedDataset in
						         rowFilterConfiguration.GetDatasetParameterValues(
							         includeReferencedProcessors: true))
						{
							yield return referencedDataset;
						}
					}
				}

				// Transformers
				if (parameterValue.ValueSource != null)
				{
					foreach (Dataset referencedDataset in
					         parameterValue.ValueSource.GetDatasetParameterValues(
						         includeReferencedProcessors: true))
					{
						yield return referencedDataset;
					}
				}
			}
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var instanceConfiguration = obj as InstanceConfiguration;
			if (instanceConfiguration == null)
			{
				return false;
			}

			if (! Equals(Name, instanceConfiguration.Name))
			{
				return false;
			}

			if (! Equals(InstanceDescriptor, instanceConfiguration.InstanceDescriptor))
			{
				return false;
			}

			// NOTE: comparison on parameter values (count, values) omitted (TOP-3665)

			return true;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = InstanceDescriptor.GetHashCode();
				result = (result * 397) ^ Name.GetHashCode();

				return result;
			}
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
