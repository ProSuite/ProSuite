using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor;

// todo daro implement IDisposable
public class QualityConditionViewModel : Observable, IQualityConditionAwareViewModel
{
	[NotNull] private readonly QualityConditionItem _item;
	private Dictionary<TestParameter, IList<ViewModelBase>> _rowsByParameter;
	private IList<ViewModelBase> _rows;

	// todo daro InstanceConfiguration?
	public QualityConditionViewModel([NotNull] QualityConditionItem item,
	                                 [NotNull] ITestParameterDatasetProvider datasetProvider) : base(null)
	{
		Assert.ArgumentNotNull(item, nameof(item));
		Assert.ArgumentNotNull(datasetProvider, nameof(datasetProvider));

		_item = item;

		DatasetProvider = datasetProvider;

		QualityCondition = Assert.NotNull(_item.GetEntity());
	}

	public IList<ViewModelBase> Rows
	{
		get => _rows;
		private set => SetProperty(ref _rows, value);
	}

	[NotNull]
	public QualityCondition QualityCondition { get; }

	[NotNull]
	public ITestParameterDatasetProvider DatasetProvider { get; }

	public void NotifyChanged(bool dirty)
	{
		_item.NotifyChanged();
	}

	public void BindTo([NotNull] QualityCondition qualityCondition)
	{
		Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

		if (_rowsByParameter != null && _rowsByParameter?.Count != 0)
		{
			UnwireEvents(_rowsByParameter.Values.SelectMany(row => row));
		}

		_rowsByParameter = CreateRows(qualityCondition);

		// todo daro inline
		Rows = new List<ViewModelBase>(GetTopLevelRows(_rowsByParameter).ToList());

		WireEvents(_rowsByParameter.Values.SelectMany(row => row));

		// implement event args?
		//SavedChanges?.Invoke(this, null);
	}

	private IEnumerable<ViewModelBase> GetTopLevelRows(
		[NotNull] Dictionary<TestParameter, IList<ViewModelBase>> rowsByParameter)
	{
		Assert.ArgumentNotNull(rowsByParameter, nameof(rowsByParameter));

		foreach (KeyValuePair<TestParameter, IList<ViewModelBase>> pair in rowsByParameter)
		{
			TestParameter testParam = pair.Key;
			IList<ViewModelBase> rows = pair.Value;

			if (rows.Count > 1)
			{
				yield return new TestParameterValueCollectionViewModel(testParam.Name, rows, this);
			}
			else
			{
				yield return rows[0];
			}
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

		TestFactory factory = TestFactoryUtils.CreateTestFactory(instanceConfiguration);

		// todo daro log
		if (factory == null)
		{
			return rowsByParameter;
		}

		InstanceFactoryUtils.InitializeParameterValues(
			factory, instanceConfiguration.ParameterValues);

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
					.Add(new DatasetTestParameterValueViewModel(
						     name,
						     datasetValue.DatasetValue,
						     datasetValue.FilterExpression,
						     datasetValue.UsedAsReferenceData,
						     this));
			}
			else if (paramValue is ScalarTestParameterValue scalarValue)
			{
				rowsByParameter[param]
					.Add(new ScalarTestParameterValueViewModel(
						     name, scalarValue.GetValue(), scalarValue.DataType, this));
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(paramValue),
				                                      $@"Unkown {nameof(TestParameterValue)} type");
			}
		}

		return rowsByParameter;
	}

	private void UpdateEntity([NotNull] QualityCondition qualityCondition)
	{
		Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

		qualityCondition.ClearParameterValues();

		foreach (KeyValuePair<TestParameter, IList<ViewModelBase>> pair in _rowsByParameter)
		{
			TestParameter testParameter = pair.Key;
			IList<ViewModelBase> rows = pair.Value;

			foreach (ViewModelBase row in rows)
			{
				if (row is DatasetTestParameterValueViewModel dataset)
				{
					qualityCondition.AddParameterValue(new DatasetTestParameterValue(testParameter,
						                                   dataset.Dataset,
						                                   dataset.FilterExpression,
						                                   dataset.UsedAsReferenceData));
				}
				else if (row is ScalarTestParameterValueViewModel scalar)
				{
					qualityCondition.AddParameterValue(
						new ScalarTestParameterValue(testParameter, row.Value));
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(row), @"Unkown view model type");
				}
			}
		}
	}

	//public event EventHandler SavedChanges;

	#region events

	private void WireEvents([NotNull] IEnumerable<ViewModelBase> rows)
	{
		Assert.ArgumentNotNull(rows, nameof(rows));

		foreach (ViewModelBase row in rows)
		{
			row.PropertyChanged += OnRowPropertyChanged;
		}
	}

	private void UnwireEvents([NotNull] IEnumerable<ViewModelBase> rows)
	{
		Assert.ArgumentNotNull(rows, nameof(rows));

		foreach (ViewModelBase row in rows)
		{
			row.PropertyChanged -= OnRowPropertyChanged;
		}
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
