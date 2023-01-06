using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public class TestParameterDatasetValidator : ITestParameterDatasetValidator
	{
		#region Implementation of ITestParameterDatasetValidator

		public bool IsValidForParameter(Dataset dataset, TestParameter testParameter,
		                                out string message)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			TestParameterType parameterType =
				TestParameterTypeUtils.GetParameterType(testParameter.Type);

			bool result = TestParameterTypeUtils.IsValidDataset(parameterType, dataset);

			message = ! result
				          ? "Invalid dataset for test parameter type " +
				            $"{Enum.GetName(typeof(TestParameterType), parameterType)}: {dataset} ({testParameter.Name})"
				          : null;

			return result;
		}

		#endregion
	}
}
