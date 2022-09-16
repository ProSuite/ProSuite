using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class ScalarTestParameterValueViewModel : ViewModelBase
{
	public ScalarTestParameterValueViewModel([NotNull] TestParameter parameter,
	                                         [CanBeNull] object value,
	                                         [NotNull] IInstanceConfigurationViewModel observer,
	                                         bool required) :
		base(parameter, value, observer, required)
	{
		ComponentParameters.Add("ViewModel", this);

		TestParameterType testParameterType = TestParameterTypeUtils.GetParameterType(DataType);

		switch (testParameterType)
		{
			case TestParameterType.String:
				ComponentType = typeof(StringValueBlazor);
				break;
			case TestParameterType.Integer:

				if (DataType.IsEnum)
				{
					ComponentParameters.Add("DataType", DataType);
					ComponentType = typeof(EnumTestParameterValueBlazor);
					break;
				}

				ComponentType = typeof(IntegerValueBlazor);
				break;
			case TestParameterType.Double:
				ComponentType = typeof(DoubleValueBlazor);
				break;
			// todo daro Blazor DateTime picker 
			case TestParameterType.DateTime:
			case TestParameterType.CustomScalar:
				throw new NotImplementedException($"{testParameterType} is not yet supported");
			case TestParameterType.Boolean:
				ComponentType = typeof(BooleanValueBlazor);
				break;
			default:
				throw new ArgumentOutOfRangeException($"Unkown {nameof(TestParameterType)}");
		}

		Validate();
	}

	protected override bool ValidateCore()
	{
		TestParameterType testParameterType = TestParameterTypeUtils.GetParameterType(DataType);
		switch (testParameterType)
		{
			case TestParameterType.String:
				return ! string.IsNullOrEmpty((string) Value) ||
				       ! string.IsNullOrWhiteSpace((string) Value);
			default:
				return base.ValidateCore();
		}
	}
}
