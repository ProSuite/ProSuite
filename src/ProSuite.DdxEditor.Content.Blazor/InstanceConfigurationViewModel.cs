using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor;

public class InstanceConfigurationViewModel<T> : NotifyPropertyChangedBase,
                                                 IInstanceConfigurationViewModel
	where T : InstanceConfiguration
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly EntityItem<T, T> _item;

	public InstanceConfigurationViewModel([NotNull] EntityItem<T, T> item,
	                                      [NotNull] ITestParameterDatasetProvider datasetProvider,
	                                      [NotNull] IItemNavigation itemNavigation)
	{
		Assert.ArgumentNotNull(item, nameof(item));
		Assert.ArgumentNotNull(datasetProvider, nameof(datasetProvider));
		Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

		_item = item;

		DatasetProvider = datasetProvider;
		ItemNavigation = itemNavigation;
	}

	[CanBeNull]
	public IList<ViewModelBase> Values { get; private set; }

	[NotNull]
	public InstanceConfiguration GetEntity()
	{
		return Assert.NotNull(_item.GetEntity());
	}

	[NotNull]
	public ITestParameterDatasetProvider DatasetProvider { get; }

	[NotNull]
	public IItemNavigation ItemNavigation { get; }

	public void NotifyChanged(bool dirty)
	{
		_item.NotifyChanged();
	}

	// todo rename to instanceConfiguration
	public void BindTo([NotNull] InstanceConfiguration qualityCondition)
	{
		Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

		Values = new List<ViewModelBase>(GetTopLevelRows(CreateRows(qualityCondition)));

		// Update the entity now, e.g.: a new entity with required collection parameter is added,
		// the collection is not yet in InstanceConfiguration.ParameterValues. After UpdateEntity
		// it is, the collection parameter is initialized.
		UpdateEntity(Assert.NotNull(_item.GetEntity()), Assert.NotNull(Values));

		// call stack:
		// IInstanceConfigurationViewModel.BindTo()
		// QualityConditionTableViewBlazor.ViewModel.set
		// QualityConditionTableViewBlazor.OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		OnPropertyChanged(nameof(Values));
	}

	public void Dispose()
	{
		Assert.NotNull(Values);

		foreach (ViewModelBase vm in Values)
		{
			vm.Dispose();
		}
	}

	void IInstanceConfigurationViewModel.OnRowPropertyChanged(
		object sender, PropertyChangedEventArgs e)
	{
		UpdateEntity(Assert.NotNull(_item.GetEntity()), Assert.NotNull(Values));
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
				yield return new TestParameterValueCollectionViewModel(
					parameter, rows, this, parameter.IsConstructorParameter);
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

		InstanceFactory factory = null;
		try
		{
			factory = InstanceFactoryUtils.CreateFactory(instanceConfiguration);
		}
		catch (Exception e)
		{
			_msg.Debug($"Error loading factory for {instanceConfiguration}.", e);
		}

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
		
		var initializedParameters = new List<TestParameter>();

		foreach (TestParameterValue paramValue in instanceConfiguration.ParameterValues)
		{
			string name = paramValue.TestParameterName;

			if (! parametersByName.TryGetValue(name, out TestParameter param))
			{
				continue;
			}

			initializedParameters.Add(param);

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
						     param.IsConstructorParameter));
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(paramValue),
				                                      $@"Unkown {nameof(TestParameterValue)} type");
			}
		}

		foreach (TestParameter param in factory.Parameters)
		{
			if (initializedParameters.Contains(param))
			{
				continue;
			}

			if (param.IsConstructorParameter)
			{
				if (TestParameterTypeUtils.IsDatasetType(param.Type))
				{
					rowsByParameter[param]
						.Add(DatasetTestParameterValueViewModel.CreateInstance(param, null, this));
				}
				else
				{
					rowsByParameter[param]
						.Add(new ScalarTestParameterValueViewModel(
							     param, null, this, param.IsConstructorParameter));
				}
			}
		}

		return rowsByParameter;
	}

	private static void UpdateEntity([NotNull] InstanceConfiguration instanceConfiguration,
	                                 [NotNull] IEnumerable<ViewModelBase> values)
	{
		Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
		Assert.ArgumentNotNull(values, nameof(values));

		instanceConfiguration.ClearParameterValues();

		foreach (ViewModelBase row in values)
		{
			AddTestParameterValue(instanceConfiguration, row);
		}
	}

	private static void AddTestParameterValue([NotNull] InstanceConfiguration instanceConfiguration,
	                                          [NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
		Assert.ArgumentNotNull(row, nameof(row));

		TestParameter testParameter = row.Parameter;
		object value = row.Value;

		if (row is DatasetTestParameterValueViewModel datasetParamVM)
		{
			instanceConfiguration.AddParameterValue(
				CreateDatasetValue(datasetParamVM, testParameter));
		}
		else if (row is ScalarTestParameterValueViewModel)
		{
			instanceConfiguration.AddParameterValue(
				new ScalarTestParameterValue(testParameter, value));
		}
		else if (row is TestParameterValueCollectionViewModel)
		{
			Assert.NotNull(value);
			foreach (ViewModelBase childRow in (IEnumerable<ViewModelBase>) value)
			{
				// recursive
				AddTestParameterValue(instanceConfiguration, childRow);
			}
		}
		else if (row is DummyTestParameterValueViewModel)
		{
			// do nothing
		}
	}

	[NotNull]
	private static DatasetTestParameterValue CreateDatasetValue(
		[NotNull] DatasetTestParameterValueViewModel datasetParamVm,
		[NotNull] TestParameter testParameter)
	{
		Either<Dataset, TransformerConfiguration> datasetSource = datasetParamVm.DatasetSource;

		Dataset dataset;
		TransformerConfiguration transformerConfiguration;

		if (datasetSource != null)
		{
			dataset = datasetSource.Match(ds => ds, _ => null);
			transformerConfiguration = datasetSource.Match(_ => null, t => t);
		}
		else
		{
			dataset = null;
			transformerConfiguration = null;
		}

		return new DatasetTestParameterValue(
			       testParameter,
			       dataset,
			       datasetParamVm.FilterExpression,
			       datasetParamVm.UsedAsReferenceData)
		       {
			       ValueSource = transformerConfiguration
		       };
	}
}
