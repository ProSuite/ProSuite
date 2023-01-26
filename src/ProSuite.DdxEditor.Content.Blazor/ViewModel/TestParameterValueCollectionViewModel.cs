using System;
using System.Collections.Generic;
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
	public TestParameterValueCollectionViewModel([NotNull] TestParameter parameter,
	                                             [NotNull] IList<ViewModelBase> values,
	                                             IInstanceConfigurationViewModel observer,
	                                             bool required) : base(
		parameter, values, observer, required)
	{
		Assert.ArgumentNotNull(values, nameof(values));

		IsDatasetType = TestParameterTypeUtils.IsDatasetType(DataType);

		ComponentType = typeof(TestParameterValueCollectionBlazor);
		ComponentParameters.Add("ViewModel", this);

		InsertDummyRow();
	}

	public bool IsDatasetType { get; }

	[NotNull]
	public string DisplayName => GetDisplayName(Values);

	public IList<ViewModelBase> Values => (IList<ViewModelBase>) Value;

	[NotNull]
	private string GetDisplayName(IEnumerable<ViewModelBase> values)
	{
		return $"[{StringUtils.Concatenate(GetNames(values), "; ")}]";
	}

	private IEnumerable<string> GetNames(IEnumerable<ViewModelBase> values)
	{
		foreach (ViewModelBase v in values)
		{
			if (v.Value == null)
			{
				yield return TestParameterTypeUtils.GetDefault(DataType)?.ToString();
			}

			else if (v is DummyTestParameterValueViewModel)
			{
				continue;
			}

			else if (DataType.IsEnum)
			{
				yield return Enum.GetName(DataType, v.Value);
			}

			else if (IsDatasetType)
			{
				yield return ((DatasetTestParameterValueViewModel) v).GetDisplayName(false);
			}
			else
			{
				yield return Assert.NotNull(v.Value).ToString();
			}
		}
	}

	[NotNull]
	public ViewModelBase InsertDefaultRow()
	{
		int? position = Values.Count - 1;

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

	public void Remove([NotNull] ViewModelBase row, [CanBeNull] out ViewModelBase newSelectedRow)
	{
		Assert.ArgumentNotNull(row, nameof(row));
		Assert.True(Values.Count > 0, "grid has no rows");

		// the last row is always a dummy row
		int rowsCount = Values.Count - 1;
		int indexOfLast = rowsCount - 1;

		int index = Values.IndexOf(row);
		if (index < indexOfLast)
		{
			// many rows and not the last?
			newSelectedRow = Values[index + 1];
		}
		else if (rowsCount == 1)
		{
			// only one row?
			newSelectedRow = null;
		}
		else
		{
			// the last row?
			newSelectedRow = Values[rowsCount - 2];
		}

		Assert.True(Values.Remove(row), $"cannot remove {row}");
		
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

		int i = index is > -1 ? index.Value : Values.Count;

		InsertCore(row, i);

		OnPropertyChanged(nameof(Values));
	}

	private void InsertDummyRow()
	{
		ViewModelBase row = new DummyTestParameterValueViewModel(Parameter, Observer);
		int index = Values.Count;

		InsertCore(row, index);
	}

	private void InsertCore(ViewModelBase row, int index)
	{
		if (index > -1)
		{
			Values.Insert(index, row);
		}
		else
		{
			Values.Add(row);
		}
	}
}
