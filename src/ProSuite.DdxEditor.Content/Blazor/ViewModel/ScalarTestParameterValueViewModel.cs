using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class ScalarTestParameterValueViewModel : ViewModelBase
{
	private object _value;

	public ScalarTestParameterValueViewModel([NotNull] string name,
	                                         [NotNull] object value,
	                                         [NotNull] Type dataType,
	                                         [NotNull] IViewModel observer) :
		base(name, observer)
	{
		Assert.ArgumentNotNull(value, nameof(value));
		Assert.ArgumentNotNull(dataType, nameof(dataType));

		_value = value;

		ComponentParameters.Add("ViewModel", this);

		TestParameterType testParameterType = TestParameterTypeUtils.GetParameterType(dataType);

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
