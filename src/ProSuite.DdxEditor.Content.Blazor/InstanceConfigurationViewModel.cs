using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor;

public class InstanceConfigurationViewModel<T> : NotifyPropertyChangedBase,
                                                 IInstanceConfigurationViewModel
	where T : InstanceConfiguration
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly EntityItem<T, T> _item;

	[NotNull] private Dictionary<TestParameter, IList<ViewModelBase>> _rowsByParameter = new();

	public InstanceConfigurationViewModel([NotNull] EntityItem<T, T> item,
	                                      [NotNull] ITestParameterDatasetProvider datasetProvider)
	{
		Assert.ArgumentNotNull(item, nameof(item));
		Assert.ArgumentNotNull(datasetProvider, nameof(datasetProvider));

		_item = item;

		DatasetProvider = datasetProvider;

		InstanceConfiguration = Assert.NotNull(_item.GetEntity());
	}

	[CanBeNull]
	public IList<ViewModelBase> Values { get; private set; }

	[NotNull]
	public InstanceConfiguration InstanceConfiguration { get; }

	[NotNull]
	public ITestParameterDatasetProvider DatasetProvider { get; }

	public void NotifyChanged(bool dirty)
	{
		_item.NotifyChanged();
	}

	public bool IsPersistent => InstanceConfiguration.IsPersistent;

	public void BindTo([NotNull] InstanceConfiguration qualityCondition)
	{
		Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

		// force dispose in case of discarding changes
		Dispose();

		_rowsByParameter = CreateRows(qualityCondition);

		Values = new List<ViewModelBase>(GetTopLevelRows(_rowsByParameter));
		OnPropertyChanged(nameof(Values));
	}

	void IInstanceConfigurationViewModel.OnRowPropertyChanged(
		object sender, PropertyChangedEventArgs e)
	{
		UpdateEntity(Assert.NotNull(_item.GetEntity()));
	}

	public void Dispose()
	{
		Values?.Clear();
		Values = null;

		foreach (ViewModelBase vm in _rowsByParameter.Values.SelectMany(row => row))
		{
			vm.Dispose();
		}

		_rowsByParameter.Clear();
	}

	private IEnumerable<ViewModelBase> GetTopLevelRows(
		[NotNull] Dictionary<TestParameter, IList<ViewModelBase>> rowsByParameter)
	{
		Assert.ArgumentNotNull(rowsByParameter, nameof(rowsByParameter));

		foreach (KeyValuePair<TestParameter, IList<ViewModelBase>> pair in rowsByParameter)
		{
			TestParameter parameter = pair.Key;
			IList<ViewModelBase> rows = pair.Value;

			if (parameter.ArrayDimension == 1)
			{
				yield return new TestParameterValueCollectionViewModel(parameter, rows, this);
			}
			else if (parameter.ArrayDimension == 0)
			{
				Assert.True(rows.Count == 1,
				            $"Unexpected row count for {parameter.ArrayDimension} dimensional test parameter {parameter}");
				yield return rows[0];
			}
			else
			{
				throw new ArgumentOutOfRangeException(
					$"Unexpected array dimension ${parameter.ArrayDimension} for test parameter {parameter}");
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

		InstanceFactory factory = InstanceFactoryUtils.CreateFactory(instanceConfiguration);

		if (factory == null)
		{
			_msg.Debug($"{nameof(InstanceFactory)} of {instanceConfiguration} is null");
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
						     param, scalarValue.GetValue(), this,
						     param.IsConstructorParameter,
						     param.IsConstructorParameter));
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(paramValue),
				                                      $@"Unkown {nameof(TestParameterValue)} type");
			}
		}

		return rowsByParameter;
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

					instanceConfiguration.AddParameterValue(
						newValue);
				}
				else if (row is ScalarTestParameterValueViewModel)
				{
					if (testParameter.IsConstructorParameter)
					{
						Assert.NotNull(row.Value);
					}

					instanceConfiguration.AddParameterValue(
						new ScalarTestParameterValue(testParameter, row.Value));
				}
				else if (row is DummyTestParameterValueViewModel)
				{
					// do nothing
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(row), @"Unkown view model type");
				}
			}
		}
	}
}
