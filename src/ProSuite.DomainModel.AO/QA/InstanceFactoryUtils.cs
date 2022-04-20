using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public static class InstanceFactoryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Gets the row filter factory, sets the row filter configuration for it and initializes
		/// its  parameter values.
		/// </summary>
		/// <returns>RowFilterFactory or null.</returns>
		[CanBeNull]
		public static RowFilterFactory CreateRowFilterFactory(
			[NotNull] RowFilterConfiguration rowFilterConfiguration)
		{
			Assert.ArgumentNotNull(rowFilterConfiguration, nameof(rowFilterConfiguration));

			if (rowFilterConfiguration.RowFilterDescriptor == null)
			{
				return null;
			}

			RowFilterFactory factory =
				CreateRowFilterFactory(rowFilterConfiguration.RowFilterDescriptor);

			if (factory != null)
			{
				InitializeParameterValues(factory, rowFilterConfiguration.ParameterValues);
			}

			return factory;
		}

		/// <summary>
		/// Gets the transformer factory, sets the transformer configuration and initializes its 
		/// parameter values.
		/// </summary>
		/// <returns>RowFilterFactory or null.</returns>
		[CanBeNull]
		public static TransformerFactory CreateTransformerFactory(
			[NotNull] TransformerConfiguration transformerConfiguration)
		{
			Assert.ArgumentNotNull(transformerConfiguration, nameof(transformerConfiguration));

			if (transformerConfiguration.TransformerDescriptor == null)
			{
				return null;
			}

			TransformerFactory factory =
				CreateTransformerFactory(transformerConfiguration.TransformerDescriptor);

			if (factory != null)
			{
				InitializeParameterValues(factory, transformerConfiguration.ParameterValues);
			}

			return factory;
		}

		public static void InitializeParameterValues(
			[NotNull] InstanceFactory factory,
			[NotNull] IEnumerable<TestParameterValue> parameterValues)
		{
			Dictionary<string, TestParameter> parametersByName =
				factory.Parameters.ToDictionary(testParameter => testParameter.Name);

			foreach (TestParameterValue parameterValue in parameterValues)
			{
				TestParameter testParameter;
				if (parametersByName.TryGetValue(parameterValue.TestParameterName,
				                                 out testParameter))
				{
					parameterValue.DataType = testParameter.Type;
				}
				else
				{
					_msg.WarnFormat(
						"Test parameter value {0}: No parameter found in {1}. The constructor Id might be incorrect.",
						parameterValue.TestParameterName, factory);
				}
			}
		}

		private static RowFilterFactory CreateRowFilterFactory(
			[NotNull] RowFilterDescriptor rowFilterDescriptor)
		{
			ClassDescriptor classDescriptor = rowFilterDescriptor.Class;

			return classDescriptor != null
				       ? new RowFilterFactory(classDescriptor.AssemblyName,
				                              classDescriptor.TypeName,
				                              rowFilterDescriptor.ConstructorId)
				       : null;
		}

		private static TransformerFactory CreateTransformerFactory(
			[NotNull] TransformerDescriptor transformerDescriptor)
		{
			ClassDescriptor classDescriptor = transformerDescriptor.Class;

			return classDescriptor != null
				       ? new TransformerFactory(classDescriptor.AssemblyName,
				                                classDescriptor.TypeName,
				                                transformerDescriptor.ConstructorId)
				       : null;
		}
	}
}
