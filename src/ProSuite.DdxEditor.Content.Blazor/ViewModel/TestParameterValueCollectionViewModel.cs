using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class TestParameterValueCollectionViewModel : ViewModelBase, IDataGridViewModel
{
	private IList<ViewModelBase> _values;

	public TestParameterValueCollectionViewModel([NotNull] TestParameter parameter,
	                                             [NotNull] IList<ViewModelBase> values,
	                                             IInstanceConfigurationViewModel observer) : base(
		parameter, null, observer)
	{
		Assert.ArgumentNotNull(values, nameof(values));

		_values = values;

		IsDatasetType = TestParameterTypeUtils.IsDatasetType(DataType);

		string displayName = StringUtils.Concatenate(values, v =>
		{
			if (v.Value == null)
			{
				return TestParameterTypeUtils.GetDefault(DataType)?.ToString();
			}

			if (IsDatasetType)
			{
				var datasetViewModel = (DatasetTestParameterValueViewModel)v.Value;
				return datasetViewModel.GetDisplayName();
			}

			return v.Value.ToString();
		}, "; ");

		DisplayName = $"[{displayName}]";

		ComponentType = typeof(TestParameterValueCollectionBlazor);
		ComponentParameters.Add("ViewModel", this);

		InsertDummyRow();
	}

	public bool IsDatasetType { get; }

	public string DisplayName { get; }
	
	public IList<ViewModelBase> Values
	{
		get => _values;
		set => SetProperty(ref _values, value);
	}

	[NotNull]
	public ViewModelBase InsertDefaultRow()
	{
		int? position = _values.Count - 1;

		TestParameterValue emptyTestParameterValue =
			TestParameterTypeUtils.GetEmptyParameterValue(Parameter);

		ViewModelBase row;

		if (TestParameterTypeUtils.IsDatasetType(DataType))
		{
			var testParameterValue = emptyTestParameterValue as DatasetTestParameterValue;
			Assert.NotNull(testParameterValue);

			row = DatasetTestParameterValueViewModel.CreateInstance(
				Parameter, testParameterValue, Observer);
			Insert(row, position);
		}
		else
		{
			var testParameterValue = emptyTestParameterValue as ScalarTestParameterValue;
			Assert.NotNull(testParameterValue);

			row = new ScalarTestParameterValueViewModel(Parameter, testParameterValue.GetValue(),
			                                            Observer, Parameter.IsConstructorParameter);
			Insert(row, position);
		}

		return row;
	}

	public void Remove([NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(row, nameof(row));

		Assert.True(_values.Remove(row), $"cannot remove {row}");

		OnPropertyChanged(nameof(Values));
	}

	public void MoveUp([NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(row, nameof(row));

		Assert.NotNull(Values);
		int index = Values.IndexOf(row);

		if (index is -1 or 0)
		{
			return;
		}

		Assert.True(Values.Remove(row), $"cannot remove {row}");
		Values.Insert(index - 1, row);

		OnPropertyChanged(nameof(Values));
	}

	public void MoveDown([NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(row, nameof(row));

		Assert.NotNull(Values);

		int index = Values.IndexOf(row);

		if (index == -1 || index == Values.Count - 1)
		{
			// selected row is not in this collection view model
			return;
		}

		Assert.True(Values.Remove(row), $"cannot remove {row}");
		Values.Insert(index + 1, row);

		OnPropertyChanged(nameof(Values));
	}

	private void Insert([NotNull] ViewModelBase row, int? index = null)
	{
		Assert.ArgumentNotNull(row, nameof(row));

		int i = index is > -1 ? index.Value : _values.Count;

		InsertCore(row, i);

		OnPropertyChanged(nameof(Values));
	}

	private void InsertDummyRow()
	{
		ViewModelBase row = new DummyTestParameterValueViewModel(Parameter, Observer);
		int index = _values.Count;

		InsertCore(row, index);
	}

	private void InsertCore(ViewModelBase row, int index)
	{
		if (index > -1)
		{
			_values.Insert(index, row);
		}
		else
		{
			_values.Add(row);
		}
	}

	private static string GetModelName([NotNull] IEnumerable<ViewModelBase> viewModels)
	{
		IEnumerable<DatasetTestParameterValueViewModel> vms =
			viewModels.Cast<DatasetTestParameterValueViewModel>();

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
