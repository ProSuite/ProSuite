using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class TestParameterValueCollectionViewModel : ViewModelBase
{
	private List<ViewModelBase> _values;

	public TestParameterValueCollectionViewModel([NotNull] TestParameter parameter,
	                                             [NotNull] IList<ViewModelBase> values,
	                                             IViewObserver observer) : base(parameter, observer)
	{
		Assert.ArgumentNotNull(values, nameof(values));

		_values = new List<ViewModelBase>(values);

		string displayName = StringUtils.Concatenate(values, v =>
		{
			if (v.Value == null)
			{
				return TestParameterTypeUtils.GetDefault(DataType)?.ToString();
			}
			else
			{
				return v.Value.ToString();
			}
		}, "; ");

		DisplayName = $"[{displayName}]";

		IsDatasetType = TestParameterTypeUtils.IsDatasetType(DataType);

		if (IsDatasetType)
		{
			ModelName = GetModelName(values);
		}

		ComponentType = typeof(TestParameterValueCollectionBlazor);
		ComponentParameters.Add("ViewModel", this);
	}

	public bool IsDatasetType { get; }

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
		int index = _values.Count;

		if (index > -1)
		{
			_values.Insert(index, row);
		}
		else
		{
			_values.Add(row);
		}
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
