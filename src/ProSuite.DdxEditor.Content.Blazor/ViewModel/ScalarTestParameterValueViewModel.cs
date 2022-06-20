using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class ScalarTestParameterValueViewModel : ViewModelBase
{
	private object _value;

	public ScalarTestParameterValueViewModel([NotNull] TestParameter parameter,
	                                         [CanBeNull] object value,
	                                         [NotNull] IViewModel observer) :
		base(parameter, observer)
	{
		//Assert.ArgumentNotNull(value, nameof(value));
		//Assert.ArgumentNotNull(dataType, nameof(dataType));

		_value = value;

		ComponentParameters.Add("ViewModel", this);

		TestParameterType testParameterType = TestParameterTypeUtils.GetParameterType(DataType);

		switch (testParameterType)
		{
			case TestParameterType.String:
				ComponentType = typeof(StringValueBlazor);
				break;
			case TestParameterType.Integer:
			case TestParameterType.Double:
				ComponentType = typeof(NumericValueBlazor);
				break;
			//case TestParameterType.DateTime:
			//	break;
			case TestParameterType.Boolean:
				ComponentType = typeof(SwitchValueBlazor);
				break;
		}
	}

	public override object Value
	{
		get => _value;
		set => SetProperty(ref _value, value);
	}
}
