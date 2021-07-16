using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	/// <summary>
	/// Factory for IRowFilter instances.
	/// </summary>
	public class RowFilterFactory : InstanceFactory
	{
		[UsedImplicitly] [NotNull] private Type _filterType;
		[UsedImplicitly] private int _constructorId;

		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterFactory"/> class.
		/// </summary>
		public RowFilterFactory([NotNull] Type type, int constructorId = 0)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			_filterType = type;
			_constructorId = constructorId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterFactory"/> class.
		/// </summary>
		public RowFilterFactory([NotNull] string assemblyName,
		                        [NotNull] string typeName,
		                        int constructorId = 0)
		{
			Assert.ArgumentNotNull(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNull(typeName, nameof(typeName));

			_filterType =
				InstanceUtils.LoadType(assemblyName, typeName, constructorId);

			_constructorId = constructorId;
		}

		public Type FilterType => _filterType;

		#region ParameterizedInstanceFactory overrides

		[NotNull]
		public override string[] TestCategories => ReflectionUtils.GetCategories(GetType());

		public override string GetTestDescription()
		{
			ConstructorInfo ctor = FilterType.GetConstructors()[_constructorId];

			return InstanceUtils.GetDescription(ctor);
		}

		public override string GetTestTypeDescription()
		{
			return FilterType.Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return InstanceUtils.CreateParameters(FilterType, _constructorId);
		}

		#endregion

		[NotNull]
		public IRowFilter Create([NotNull] IOpenDataset datasetContext,
		                         [NotNull] RowFilterConfiguration rowFilterConfiguration)
		{
			return Create(rowFilterConfiguration, datasetContext, Parameters,
			              CreateInstance<IRowFilter>);
		}

		private T CreateInstance<T>(object[] args)
		{
			return InstanceUtils.CreateInstance<T>(FilterType, _constructorId, args);
		}
	}
}
