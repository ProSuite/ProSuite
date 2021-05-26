using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public static class InstanceFactoryUtils
	{
		/// <summary>
		/// Gets the row filter factory, sets the row filter configuration for it and initializes the 
		/// parameter values for it.
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

		public static void InitializeParameterValues(
			[NotNull] ParameterizedInstanceFactory factory,
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
	}
}
