using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.Finder;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.BoundTableRows;

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

	public void Dispose()
	{
		DisposeCore(this);
	}

	[CanBeNull]
	public IList<ViewModelBase> Values { get; private set; }

	#region IInstanceConfigurationViewModel

	public InstanceConfiguration GetEntity()
	{
		return Assert.NotNull(_item.GetEntity());
	}

	[NotNull]
	public ITestParameterDatasetProvider DatasetProvider { get; }

	public IItemNavigation ItemNavigation { get; }

	/// <summary>
	/// An optional expression builder for SQL expressions.
	/// </summary>
	public ISqlExpressionBuilder SqlExpressionBuilder { get; set; }

	void IViewObserver.NotifyChanged(bool dirty)
	{
		_item.NotifyChanged();
	}

	void IInstanceConfigurationViewModel.BindTo(InstanceConfiguration instanceConfiguration)
	{
		Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));

		Values = new List<ViewModelBase>(GetTopLevelRows(CreateRows(instanceConfiguration)));

		// call stack:
		// IInstanceConfigurationViewModel.BindTo()
		// QualityConditionTableViewBlazor.ViewModel.set
		// QualityConditionTableViewBlazor.OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		OnPropertyChanged(nameof(Values));
	}

	void IInstanceConfigurationViewModel.OnRowPropertyChanged(
		object sender, PropertyChangedEventArgs e)
	{
		UpdateEntity(Assert.NotNull(_item.GetEntity()), Assert.NotNull(Values));
	}

	DatasetFinderItem IInstanceConfigurationViewModel.FindDatasetClicked(TestParameter parameter)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));

		TestParameterType parameterType = TestParameterTypeUtils.GetParameterType(parameter.Type);

		using FinderForm<DatasetFinderItem> form = GetDatasetFinderForm(parameterType);

		DialogResult result = form.ShowDialog();

		if (result != DialogResult.OK)
		{
			//return value;
		}

		IList<DatasetFinderItem> selection = form.Selection;

		if (selection is not { Count: 1 })
		{
			return null;
		}

		return selection[0];
	}

	#endregion

	private void DisposeCore(
		[NotNull] IInstanceConfigurationViewModel instanceConfigurationViewModel)
	{
		if (Values == null)
		{
			return;
		}

		foreach (ViewModelBase vm in Values)
		{
			_msg.VerboseDebug(() => $"OnRowPropertyChanged unregister: {this}");

			vm.PropertyChanged -= instanceConfigurationViewModel.OnRowPropertyChanged;

			vm.Dispose();
		}
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
				yield return ViewModelFactory.CreateCollectionViewModel(parameter, rows, this);
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

			rowsByParameter[param]
				.Add(ViewModelFactory.CreateTestParameterViewModel(param, paramValue, this));
		}

		foreach (TestParameter param in factory.Parameters)
		{
			if (initializedParameters.Contains(param))
			{
				continue;
			}

			// TOP-5941: Do not automatically add empty rows to list parameters - they will be stored.
			if (param.IsConstructorParameter && param.ArrayDimension == 0)
			{
				rowsByParameter[param]
					.Add(ViewModelFactory.CreateEmptyTestParameterViewModel(param, this));
			}
		}

		return rowsByParameter;
	}

	private static void UpdateEntity([NotNull] InstanceConfiguration instanceConfiguration,
	                                 [NotNull] IEnumerable<ViewModelBase> values)
	{
		Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
		Assert.ArgumentNotNull(values, nameof(values));

		using (_msg.IncrementIndentation())
		{
			_msg.VerboseDebug(() => $"update entity: {instanceConfiguration}");

			instanceConfiguration.ClearParameterValues();

			foreach (ViewModelBase row in values)
			{
				AddTestParameterValue(instanceConfiguration, row);
			}
		}
	}

	private static void AddTestParameterValue([NotNull] InstanceConfiguration instanceConfiguration,
	                                          [NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
		Assert.ArgumentNotNull(row, nameof(row));

		TestParameter testParameter = row.Parameter;
		object value = row.Value;

		_msg.VerboseDebug(() => $"add {row}");

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

	private FinderForm<DatasetFinderItem> GetDatasetFinderForm(
		TestParameterType datasetParameterType)
	{
		var finder = new Finder<DatasetFinderItem>();

		InstanceConfiguration instanceConfiguration = GetEntity();

		DataQualityCategory category = instanceConfiguration.Category;

		if (instanceConfiguration is TransformerConfiguration transformer)
		{
			// Do not allow circular references!
			DatasetProvider.Exclude(transformer);
		}

		return FinderUtils.GetDatasetFinder(category, DatasetProvider,
		                                    datasetParameterType,
		                                    finder);
	}
}
