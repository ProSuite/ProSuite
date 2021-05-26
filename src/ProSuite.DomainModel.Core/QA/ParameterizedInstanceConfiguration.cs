using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	public abstract class ParameterizedInstanceConfiguration : VersionedEntityWithMetadata,
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

		#endregion

		protected ParameterizedInstanceConfiguration() { }

		protected ParameterizedInstanceConfiguration([NotNull] string name,
		                                             [CanBeNull] string description = "")
		{
			Name = name;
			Description = description;
		}

		[NotNull]
		public IList<TestParameterValue> ParameterValues =>
			new ReadOnlyList<TestParameterValue>(_parameterValues);

		protected abstract InstanceDescriptor InstanceDescriptor { get; }

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

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var instanceConfiguration = obj as ParameterizedInstanceConfiguration;
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
