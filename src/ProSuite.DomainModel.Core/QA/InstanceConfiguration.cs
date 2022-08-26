using System;
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
		                                [CanBeNull] string description = "")
			: this(assignUuid: true)
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
		/// Gets the dataset referenced directly by the condition and optionally also the datasets
		/// referenced indirectly (and recursively) by any filters or transformers.
		/// </summary>
		/// <param name="includeReferencedProcessors">include IssueFilters and Transformers</param>
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
				else if (includeSourceDatasets)
				{
					foreach (Dataset referencedDataset in
					         datasetTestParameterValue.GetAllSourceDatasets())
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

		public abstract InstanceConfiguration CreateCopy();

		[NotNull]
		protected static string GetUuid([NotNull] string value)
		{
			// this fails if the string is not a valid guid:
			var guid = new Guid(value);

			return FormatUuid(guid);
		}

		[NotNull]
		protected static string GenerateUuid()
		{
			return FormatUuid(Guid.NewGuid());
		}

		[NotNull]
		protected static string FormatUuid(Guid guid)
		{
			// default format (no curly braces)
			return guid.ToString().ToUpper();
		}

		protected void CopyBaseProperties(InstanceConfiguration target)
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
