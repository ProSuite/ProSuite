using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class TestParameterValueCollectionViewModel : ViewModelBase
{
	private List<ViewModelBase> _values;

	public TestParameterValueCollectionViewModel([NotNull] TestParameter parameter,
	                                             [NotNull] IList<ViewModelBase> values,
	                                             IViewModel observer) : base(parameter, observer)
	{
		Assert.ArgumentNotNull(values, nameof(values));

		_values = new List<ViewModelBase>(values);

		DisplayName =
			StringUtils.Concatenate(values, v => v.Value == null ? "<null>" : v.Value.ToString(), "; ");

		if (TestParameterTypeUtils.IsDatasetType(Parameter.Type))
		{
			ModelName = GetModelName(values);
		}

		ComponentType = typeof(TestParameterValueCollectionBlazor);
		ComponentParameters.Add("ViewModel", this);
	}

	public override List<ViewModelBase> Values
	{
		get => _values;
		set => SetProperty(ref _values, value);
	}

	public string DisplayName { get; }
	
	[CanBeNull]
	[UsedImplicitly]
	public string ModelName { get; }

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

	private static string GetModelName([NotNull] IEnumerable<ViewModelBase> viewModels)
	{
		var vms = viewModels.Cast<DatasetTestParameterValueViewModel>();

		return StringUtils.Concatenate(GetDistinctModelNames(vms), ", ");
	}

	private static IEnumerable<string> GetDistinctModelNames(
		[NotNull] IEnumerable<DatasetTestParameterValueViewModel> viewModels)
	{
		var modelNames = new SimpleSet<string>();

		foreach (DatasetTestParameterValueViewModel vm in viewModels)
		{
			if (vm.ModelName == null)
			{
				continue;
			}

			if (modelNames.TryAdd(vm.ModelName))
			{
				yield return vm.ModelName;
			}
		}
	}
}
