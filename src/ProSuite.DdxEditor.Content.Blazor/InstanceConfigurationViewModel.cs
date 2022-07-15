using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor;

// todo daro implement IDisposable
public class InstanceConfigurationViewModel<T> : Observable, IInstanceConfigurationViewModel
	where T : InstanceConfiguration
{
	[NotNull] private readonly EntityItem<T, T> _item;
	private Dictionary<TestParameter, IList<ViewModelBase>> _rowsByParameter;
	private Dictionary<TestParameter, ViewModelBase> _topLevelRowsByParameter;
	private IList<ViewModelBase> _rows;

	// todo daro InstanceConfiguration?
	public InstanceConfigurationViewModel([NotNull] EntityItem<T, T> item,
	                                      [NotNull] ITestParameterDatasetProvider datasetProvider,
	                                      [NotNull]
	                                      IRowFilterConfigurationProvider rowFilterProvider)
	{
		Assert.ArgumentNotNull(item, nameof(item));
		Assert.ArgumentNotNull(datasetProvider, nameof(datasetProvider));

		_item = item;

		DatasetProvider = datasetProvider;
		RowFilterProvider = rowFilterProvider;

		InstanceConfiguration = Assert.NotNull(_item.GetEntity());
	}

	public IList<ViewModelBase> Rows
	{
		get => _rows;
		set => SetProperty(ref _rows, value);
	}

	[NotNull]
	public InstanceConfiguration InstanceConfiguration { get; }

	[NotNull]
	public ITestParameterDatasetProvider DatasetProvider { get; }

	[NotNull]
	public IRowFilterConfigurationProvider RowFilterProvider { get; }

	public void NotifyChanged(bool dirty)
	{
		_item.NotifyChanged();
	}

	public void BindTo([NotNull] InstanceConfiguration qualityCondition)
	{
		Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

		if (_rowsByParameter != null && _rowsByParameter?.Count > 0)
		{
			UnwireEvents(_rowsByParameter.Values.SelectMany(row => row));
		}

		if (_topLevelRowsByParameter != null && _topLevelRowsByParameter?.Count > 0)
		{
			UnwireEvents(_topLevelRowsByParameter.Values
			                                     .OfType<TestParameterValueCollectionViewModel>());
		}

		_rowsByParameter = CreateRows(qualityCondition);

		WireEvents(_rowsByParameter.Values.SelectMany(row => row));

		_topLevelRowsByParameter =
			new Dictionary<TestParameter, ViewModelBase>(GetTopLevelRows(_rowsByParameter));

		WireEvents(_topLevelRowsByParameter.Values.OfType<TestParameterValueCollectionViewModel>());

		Rows = new List<ViewModelBase>(_topLevelRowsByParameter.Values);
	}

	private IEnumerable<KeyValuePair<TestParameter, ViewModelBase>> GetTopLevelRows(
		[NotNull] Dictionary<TestParameter, IList<ViewModelBase>> rowsByParameter)
	{
		Assert.ArgumentNotNull(rowsByParameter, nameof(rowsByParameter));

		foreach (KeyValuePair<TestParameter, IList<ViewModelBase>> pair in rowsByParameter)
		{
			TestParameter parameter = pair.Key;
			IList<ViewModelBase> rows = pair.Value;

			if (parameter.ArrayDimension > 0)
			{
				yield return new KeyValuePair<TestParameter, ViewModelBase>(
					parameter, new TestParameterValueCollectionViewModel(parameter, rows, this));
			}
			else if (rows.Count == 1)
			{
				yield return new KeyValuePair<TestParameter, ViewModelBase>(parameter, rows[0]);
			}
			// todo daro log
		}
	}

	private Dictionary<TestParameter, IList<ViewModelBase>> CreateRows(
		[CanBeNull] InstanceConfiguration instanceConfiguration)
	{
		var rowsByParameter = new Dictionary<TestParameter, IList<ViewModelBase>>();

		if (instanceConfiguration == null)
		{
			return rowsByParameter;
		}

		var factory = InstanceFactoryUtils.CreateFactory(instanceConfiguration);

		// todo daro log
		if (factory == null)
		{
			return rowsByParameter;
		}

		var parametersByName = new Dictionary<string, TestParameter>();

		foreach (TestParameter param in factory.Parameters)
		{
			rowsByParameter.Add(param, new List<ViewModelBase>());
			parametersByName.Add(param.Name, param);
		}

		foreach (TestParameterValue paramValue in instanceConfiguration.ParameterValues)
		{
			string name = paramValue.TestParameterName;

			if (! parametersByName.TryGetValue(name, out TestParameter param))
			{
				continue;
			}

			if (paramValue is DatasetTestParameterValue datasetValue)
			{
				rowsByParameter[param]
					.Add(DatasetTestParameterValueViewModel.CreateInstance(
						     param, datasetValue, this));
			}
			else if (paramValue is ScalarTestParameterValue scalarValue)
			{
				rowsByParameter[param]
					.Add(new ScalarTestParameterValueViewModel(
						     param, scalarValue.GetValue(), this));
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(paramValue),
				                                      $@"Unkown {nameof(TestParameterValue)} type");
			}
		}

		return rowsByParameter;
	}

	public ViewModelBase InsertRow(TestParameter parameter)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));
		Assert.ArgumentCondition(_rowsByParameter.ContainsKey(parameter), nameof(parameter));

		var collectionRow =
			Assert.NotNull(
				(TestParameterValueCollectionViewModel) _topLevelRowsByParameter[parameter]);

		ViewModelBase insertRow;

		if (TestParameterTypeUtils.IsDatasetType(parameter.Type))
		{
			insertRow = DatasetTestParameterValueViewModel.CreateInstance(parameter, null, this);
		}
		else
		{
			insertRow = new ScalarTestParameterValueViewModel(parameter, null, this);
		}

		insertRow.New = true;

		WireEvents(insertRow);

		_rowsByParameter[parameter].Add(insertRow);

		collectionRow.Insert(insertRow);

		return insertRow;
	}

	public void DeleteRow(ViewModelBase row)
	{
		TestParameter parameter = row.Parameter;

		var collectionRow =
			Assert.NotNull(
				(TestParameterValueCollectionViewModel) _topLevelRowsByParameter[parameter]);

		Assert.True(_rowsByParameter[parameter].Remove(row), $"cannot remove {row}");

		collectionRow.Remove(row);
	}

	public bool TryMoveDown([NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(row, nameof(row));

		TestParameter parameter = row.Parameter;

		var collectionRow =
			Assert.NotNull(
				(TestParameterValueCollectionViewModel) _topLevelRowsByParameter[parameter]);

		List<ViewModelBase> values = Assert.NotNull(collectionRow.Values);

		int index = values.IndexOf(row);

		if (index == -1 || index == values.Count - 1)
		{
			// selected row is not in this collection view model
			return false;
		}

		Assert.True(_rowsByParameter[parameter].Remove(row), $"cannot remove {row}");
		_rowsByParameter[parameter].Insert(index + 1, row);

		return collectionRow.TryMoveDown(row);
	}

	public bool TryMoveUp([NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(row, nameof(row));

		TestParameter parameter = row.Parameter;

		var collectionRow =
			Assert.NotNull(
				(TestParameterValueCollectionViewModel) _topLevelRowsByParameter[parameter]);

		List<ViewModelBase> values = Assert.NotNull(collectionRow.Values);

		int index = values.IndexOf(row);

		if (index is -1 or 0)
		{
			return false;
		}

		Assert.True(_rowsByParameter[parameter].Remove(row), $"cannot remove {row}");
		_rowsByParameter[parameter].Insert(index - 1, row);

		return collectionRow.TryMoveUp(row);
	}

	private void UpdateEntity([NotNull] InstanceConfiguration instanceConfiguration)
	{
		Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));

		instanceConfiguration.ClearParameterValues();

		foreach (KeyValuePair<TestParameter, IList<ViewModelBase>> pair in _rowsByParameter)
		{
			TestParameter testParameter = pair.Key;
			IList<ViewModelBase> rows = pair.Value;

			foreach (ViewModelBase row in rows)
			{
				if (row is DatasetTestParameterValueViewModel datasetParamVM)
				{
					var newValue = new DatasetTestParameterValue(
						               testParameter,
						               datasetParamVM.DatasetSource.Match(d => d, t => null),
						               datasetParamVM.FilterExpression,
						               datasetParamVM.UsedAsReferenceData)
					               {
						               ValueSource = datasetParamVM.DatasetSource.Match(
							               d => null, t => t)
					               };

					UpdateRowFilterConfigurations(newValue, datasetParamVM);

					instanceConfiguration.AddParameterValue(
						newValue);
				}
				else if (row is ScalarTestParameterValueViewModel scalar)
				{
					instanceConfiguration.AddParameterValue(
						new ScalarTestParameterValue(testParameter, row.Value));
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(row), @"Unkown view model type");
				}
			}
		}
	}

	private void UpdateRowFilterConfigurations(DatasetTestParameterValue datasetParameter,
	                                           DatasetTestParameterValueViewModel viewModel)
	{
		datasetParameter.RowFiltersExpression = viewModel.RowFilterExpression;
		
		datasetParameter.ClearRowFilters();

		datasetParameter.ClearRowFilters();
		foreach (RowFilterConfiguration rowFilterConfiguration in viewModel.RowFilterConfigurations)
		{
			datasetParameter.AddRowFilter(rowFilterConfiguration);
		}
	}

	#region events

	private void WireEvents([NotNull] IEnumerable<ViewModelBase> rows)
	{
		Assert.ArgumentNotNull(rows, nameof(rows));

		foreach (ViewModelBase row in rows)
		{
			WireEvents(row);
		}
	}

	private void WireEvents(ViewModelBase row)
	{
		row.PropertyChanged += OnRowPropertyChanged;
	}

	private void UnwireEvents([NotNull] IEnumerable<ViewModelBase> rows)
	{
		Assert.ArgumentNotNull(rows, nameof(rows));

		foreach (ViewModelBase row in rows)
		{
			UnwireEvents(row);
		}
	}

	private void UnwireEvents(ViewModelBase row)
	{
		row.PropertyChanged -= OnRowPropertyChanged;
	}

	private void OnRowPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		UpdateEntity(Assert.NotNull(_item.GetEntity()));
	}

	#endregion

	#region unused

	private static string GetDatasetParameterName(IEnumerable<TestParameter> parameters)
	{
		string datasetParameterName = null;

		foreach (TestParameter parameter in parameters)
		{
			if (TestParameterTypeUtils.IsDatasetType(parameter.Type))
			{
				if (datasetParameterName != null)
				{
					return datasetParameterName;
				}

				datasetParameterName = parameter.Name;

				if (parameter.ArrayDimension > 0 &&
				    parameter.IsConstructorParameter)
				{
					return datasetParameterName;
				}
			}
			else
			{
				// scalar parameter - no arrays allowed if a required constructor parameter
				if (parameter.ArrayDimension > 0 &&
				    parameter.IsConstructorParameter)
				{
					return datasetParameterName;
				}
			}
		}

		return datasetParameterName;
	}

	[NotNull]
	private static IList<TestParameter> GetEditableScalarParameters(
		[NotNull] IEnumerable<TestParameter> testParameters,
		[NotNull] string datasetParameterName)
	{
		var result = new List<TestParameter>();

		foreach (TestParameter testParameter in testParameters)
		{
			if (testParameter.ArrayDimension > 0)
			{
				if (! testParameter.IsConstructorParameter)
				{
					// ignore *optional* parameters with array dimension > 0
					continue;
				}

				// fail for constructor parameters
				Assert.Fail("Unexpected array dimension: {0}",
				            testParameter.ArrayDimension);
			}

			if (TestParameterTypeUtils.IsDatasetType(testParameter.Type))
			{
				Assert.AreEqual(testParameter.Name, datasetParameterName,
				                "Unexpected dataset parameter name");
				continue;
			}

			// it's a scalar parameter
			result.Add(testParameter);
		}

		return result;
	}

	#endregion
}
