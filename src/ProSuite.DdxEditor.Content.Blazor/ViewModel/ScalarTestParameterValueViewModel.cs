using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class ScalarTestParameterValueViewModel : ViewModelBase
{
	public ScalarTestParameterValueViewModel([NotNull] TestParameter parameter,
	                                         [CanBeNull] object value,
	                                         [NotNull] IViewObserver observer) :
		base(parameter, observer)
	{
		Value = value ?? TestParameterTypeUtils.GetDefault(DataType);

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
				ComponentType = typeof(SwitchValueBlazor);
				break;
			default:
				throw new ArgumentOutOfRangeException($"Unkown {nameof(TestParameterType)}");
		}
	}
}
