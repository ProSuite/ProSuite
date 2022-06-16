using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DdxEditor.Content.Blazor.View;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class TestParameterValueCollectionViewModel : ViewModelBase
{
	public TestParameterValueCollectionViewModel([NotNull] string name,
	                                             IList<ViewModelBase> values,
	                                             IViewModel observer) : base(name, observer)
	{
		Assert.ArgumentNotNullOrEmpty(name, nameof(name));
		Assert.ArgumentNotNull(values, nameof(values));

		Values = new List<ViewModelBase>(values);
		DisplayName = StringUtils.Concatenate(values, v => v.Value.ToString(), "; ");

		ComponentType = typeof(TestParameterValueCollectionBlazor);
		ComponentParameters.Add("ViewModel", this);
	}

	public string DisplayName { get; }

	public override object Value { get; set; }
}
