using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Core;

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

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private DataQualityCategory _category;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _url;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _notes;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _uuid;

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceConfiguration"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected InstanceConfiguration(bool assignUuid)
		{
			if (assignUuid)
			{
				Uuid = GenerateUuid();
			}
		}

		protected InstanceConfiguration([NotNull] string name,
		                                [CanBeNull] string description = "",
		                                bool assignUuid = true)
			: this(assignUuid)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

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
			set
			{
				_instanceDescriptor = value;
				InstanceDescriptorChanged();
			}
		}

		protected virtual void InstanceDescriptorChanged() { }

		[Required]
		public string Uuid
		{
			get { return _uuid; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));

				_uuid = GetUuid(value);
			}
		}

		[CanBeNull]
		public DataQualityCategory Category
		{
			get { return _category; }
			set { _category = value; }
		}

		[MaximumStringLength(2000)]
		public string Url
		{
			get { return _url; }
			set { _url = value; }
		}

		[MaximumStringLength(2000)]
		public string Notes
		{
			get { return _notes; }
			set { _notes = value; }
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
		[ValidName]
		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public abstract string TypeDisplayName { get; }

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

		public IEnumerable<TestParameterValue> GetDefinedParameterValues()
		{
			IInstanceInfo instanceInfo =
				Assert.NotNull(InstanceDescriptorUtils.GetInstanceInfo(InstanceDescriptor));

			IList<TestParameter> parameters = instanceInfo.Parameters;

			foreach (TestParameterValue parameterValue in ParameterValues)
			{
				if (parameters.Any(p => p.Name == parameterValue.TestParameterName))
				{
					yield return parameterValue;
				}
			}
		}

		/// <summary>
		/// Gets the dataset referenced directly by the condition and optionally also the datasets
		/// referenced indirectly (and recursively) by any filters or transformers.
		/// </summary>
		/// <param name="includeReferencedProcessors">include IssueFilters and Transformers</param>
		/// <param name="includeSourceDatasets">Recursively include datasets of transformers</param>
		/// <param name="excludeReferenceDatasets">Whether dataset parameters that are only used as
		/// reference data shall be excluded. Also, in case a transformer is used as input, it will
		/// also be excluded if used as reference data.</param>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<Dataset> GetDatasetParameterValues(
			bool includeReferencedProcessors = false,
			bool includeSourceDatasets = false,
			bool excludeReferenceDatasets = false)
		{
			foreach (TestParameterValue parameterValue in ParameterValues)
			{
				var datasetTestParameterValue = parameterValue as DatasetTestParameterValue;

				if (datasetTestParameterValue == null)
				{
					continue;
				}

				if (excludeReferenceDatasets && datasetTestParameterValue.UsedAsReferenceData)
				{
					continue;
				}

				Dataset dataset = datasetTestParameterValue.DatasetValue;

				if (dataset != null)
				{
					yield return dataset;
				}
				else if (includeSourceDatasets)
				{
					foreach (Dataset referencedDataset in
					         datasetTestParameterValue.GetAllSourceDatasets(
						         excludeReferenceDatasets))
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
				// Transformers (issue filters are provided by override)
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

		public abstract InstanceConfiguration CreateCopy();

		[NotNull]
		protected static string GetUuid([NotNull] string value)
		{
			// this fails if the string is not a valid guid:
			return InstanceConfigurationUtils.GenerateUuid(value);
		}

		[NotNull]
		protected static string GenerateUuid()
		{
			return InstanceConfigurationUtils.GenerateUuid();
		}

		protected void CopyBaseProperties([NotNull] InstanceConfiguration target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			target.Name = Name;

			target.Description = Description;
			target.Notes = Notes;
			target.Url = Url;

			foreach (TestParameterValue testParameterValue in ParameterValues)
			{
				target.AddParameterValue(testParameterValue.Clone());
			}

			target.Category = Category;
		}
	}
}
