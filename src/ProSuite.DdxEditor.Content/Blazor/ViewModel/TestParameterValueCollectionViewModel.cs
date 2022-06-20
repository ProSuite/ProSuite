using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class TestParameterValueCollectionViewModel : ViewModelBase
{
	private List<ViewModelBase> _values;

	public TestParameterValueCollectionViewModel([NotNull] TestParameter parameter,
	                                             Type dataType,
	                                             IList<ViewModelBase> values,
	                                             IViewModel observer) : base(parameter, observer)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));
		Assert.ArgumentNotNull(values, nameof(values));

		_values = new List<ViewModelBase>(values);
		DisplayName = StringUtils.Concatenate(values, v => v.Value.ToString(), "; ");

		ComponentType = typeof(TestParameterValueCollectionBlazor);
		ComponentParameters.Add("ViewModel", this);
	}

	public override List<ViewModelBase> Values
	{
		get => _values;
		set => SetProperty(ref _values, value);
	}

	public string DisplayName { get; }

	public override object Value { get; set; }

	public void Insert(ViewModelBase row)
	{
		_values.Add(row);
		OnPropertyChanged(nameof(Values));
	}

	public void Remove(ViewModelBase row)
	{
		Assert.True(_values.Remove(row), $"cannot remove {row}");

		OnPropertyChanged(nameof(Values));
	}
}
