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
	public class TransformerFactory : InstanceFactory
	{
		[UsedImplicitly] [NotNull] private Type _transformerType;
		[UsedImplicitly] private int _constructorId;

		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterFactory"/> class.
		/// </summary>
		public TransformerFactory([NotNull] Type type, int constructorId = 0)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			_transformerType = type;
			_constructorId = constructorId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RowFilterFactory"/> class.
		/// </summary>
		public TransformerFactory([NotNull] string assemblyName,
		                          [NotNull] string typeName,
		                          int constructorId = 0)
		{
			Assert.ArgumentNotNull(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNull(typeName, nameof(typeName));

			_transformerType =
				InstanceUtils.LoadType(assemblyName, typeName, constructorId);
			_constructorId = constructorId;
		}

		public Type TransformerType => _transformerType;

		#region ParameterizedInstanceFactory overrides

		[NotNull]
		public override string[] TestCategories => ReflectionUtils.GetCategories(GetType());

		public override string GetTestDescription()
		{
			ConstructorInfo ctor = TransformerType.GetConstructors()[_constructorId];

			return InstanceUtils.GetDescription(ctor);
		}

		public override string GetTestTypeDescription()
		{
			return TransformerType.Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return InstanceUtils.CreateParameters(TransformerType, _constructorId);
		}

		#endregion

		[NotNull]
		public ITableTransformer Create([NotNull] IOpenDataset datasetContext,
		                                [NotNull] TransformerConfiguration transformerConfiguration)
		{
			return Create(transformerConfiguration, datasetContext, Parameters,
			              CreateInstance<ITableTransformer>);
		}

		private T CreateInstance<T>(object[] args)
		{
			return InstanceUtils.CreateInstance<T>(
				TransformerType, _constructorId, args);
		}
	}
}