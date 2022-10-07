using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.UI.QA.PropertyEditors
{
	public abstract class DatasetProperty<T> : ParameterProperty<T>
		where T : ParameterConfig
	{
		protected DatasetProperty([NotNull] T parameterConfig)
			: base(parameterConfig) { }

		protected override void InitTestAttributeValue()
		{
			string parameterName = GetAttributeName();

			var testParam = new TestParameter(parameterName, GetParameterType());

			TestParameterValue testValue = new DatasetTestParameterValue(testParam);

			GetParameterConfig().SetTestParameterValue(testValue);
		}

		protected abstract Type GetParameterType();
	}
}
