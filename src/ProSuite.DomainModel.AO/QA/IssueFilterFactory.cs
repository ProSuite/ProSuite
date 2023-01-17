using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public class IssueFilterFactory : InstanceFactory
	{
		[UsedImplicitly] [NotNull] private Type _filterType;
		[UsedImplicitly] private int _constructorId;

		/// <summary>
		/// Initializes a new instance of the <see cref="IssueFilterFactory"/> class.
		/// </summary>
		public IssueFilterFactory([NotNull] Type type, int constructorId = 0)
		{
			Assert.ArgumentNotNull(type, nameof(type));
			InstanceUtils.AssertConstructorExists(type, constructorId);

			_filterType = type;
			_constructorId = constructorId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IssueFilterFactory"/> class.
		/// </summary>
		public IssueFilterFactory([NotNull] string assemblyName,
		                          [NotNull] string typeName,
		                          int constructorId = 0)
		{
			Assert.ArgumentNotNull(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNull(typeName, nameof(typeName));

			_filterType =
				InstanceUtils.LoadType(assemblyName, typeName, constructorId);

			_constructorId = constructorId;
		}

		[NotNull]
		public Type FilterType => _filterType;

		public override string TestDescription =>
			InstanceUtils.GetDescription(FilterType, _constructorId);

		[NotNull]
		public override string[] TestCategories => InstanceUtils.GetCategories(FilterType);

		public override string GetTestTypeDescription()
		{
			return FilterType.Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return InstanceUtils.CreateParameters(FilterType, _constructorId);
		}

		[NotNull]
		public IIssueFilter Create([NotNull] IOpenDataset datasetContext,
		                           [NotNull] IssueFilterConfiguration issueFilterConfiguration)
		{
			return Create(issueFilterConfiguration, datasetContext, Parameters,
			              CreateInstance<IIssueFilter>);
		}

		private T CreateInstance<T>(object[] args)
		{
			return InstanceUtils.CreateInstance<T>(FilterType, _constructorId, args);
		}

		public override string ToString()
		{
			return
				$"Instance {FilterType.Name} with parameters: {InstanceUtils.GetTestSignature(this)}";
		}
	}
}
